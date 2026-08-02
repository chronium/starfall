using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using ChronoFall.CharacterPresentation;
using ChronoFall.CharacterPresentation.SdlGpu;
using SDL;
using static SDL.SDL3;

namespace Starfall.Client;

internal static unsafe class NativeCharacterPreview
{
    private const int WindowWidth = 960;
    private const int WindowHeight = 720;

    internal static void Run(CharacterPresentationContent content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ConfigureNativeSdl();
        using var session = new PreviewSession(content.Cooked.Asset.Mesh);
        session.Run(content.IdleAnimation, content.Cooked.Asset.Mesh.Skin);
    }

    private static void ConfigureNativeSdl() =>
        NativeLibrary.SetDllImportResolver(typeof(SDL3).Assembly, ResolveNativeLibrary);

    private static IntPtr ResolveNativeLibrary(
        string libraryName,
        Assembly assembly,
        DllImportSearchPath? searchPath)
    {
        if (!string.Equals(libraryName, "SDL3", StringComparison.Ordinal))
            return IntPtr.Zero;
        if (!OperatingSystem.IsMacOS() || RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
            return IntPtr.Zero;

        string path = Path.Combine(
            AppContext.BaseDirectory,
            "runtimes",
            "osx-arm64",
            "native",
            "libSDL3.dylib");
        if (!File.Exists(path))
            throw new DllNotFoundException($"SDL3 was not bundled for macOS ARM64. Expected path: {path}");
        return NativeLibrary.Load(path);
    }

    private sealed class PreviewSession : IDisposable
    {
        private const SDL_GPUTextureFormat DepthFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT;
        private static readonly SDL_FColor ClearColor = new() { r = 0.035f, g = 0.045f, b = 0.070f, a = 1.0f };

        private readonly MeshBounds bounds;
        private SDL_Window* window;
        private SDL_GPUDevice* device;
        private SdlGpuSkinnedCharacterRenderer? renderer;
        private SdlGpuSkinnedMesh? mesh;
        private SdlGpuSkinningPalette? palette;
        private SDL_GPUTexture* depth;
        private uint depthWidth;
        private uint depthHeight;
        private bool windowClaimed;

        internal PreviewSession(SkinnedMeshDefinition sourceMesh)
        {
            ArgumentNullException.ThrowIfNull(sourceMesh);
            bounds = MeshBounds.Create(sourceMesh.Vertices.Select(static vertex => vertex.Position));

            if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
                throw new InvalidOperationException($"SDL video initialization failed: {SDL_GetError()}");

            try
            {
                window = SDL_CreateWindow(
                    "Starfall - Shared Character Presentation",
                    WindowWidth,
                    WindowHeight,
                    (SDL_WindowFlags)0);
                if (window is null)
                    throw new InvalidOperationException($"SDL window creation failed: {SDL_GetError()}");

                const SDL_GPUShaderFormat requested =
                    SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL |
                    SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV;
                device = SDL_CreateGPUDevice(requested, debug_mode: true, name: (byte*)null);
                if (device is null)
                    throw new InvalidOperationException($"SDL GPU device creation failed: {SDL_GetError()}");
                if (!SDL_ClaimWindowForGPUDevice(device, window))
                    throw new InvalidOperationException($"SDL GPU window claim failed: {SDL_GetError()}");
                windowClaimed = true;

                SDL_GPUShaderFormat shaderFormat = SelectShaderFormat(SDL_GetGPUShaderFormats(device));
                renderer = new SdlGpuSkinnedCharacterRenderer(
                    device,
                    SDL_GetGPUSwapchainTextureFormat(device, window),
                    DepthFormat,
                    LoadShaders(shaderFormat));
                SDL_GPUCommandBuffer* uploadCommand = AcquireCommand();
                mesh = renderer.UploadMesh(uploadCommand, sourceMesh);
                palette = renderer.CreatePalette(sourceMesh.Skin.Skeleton.JointCount);
                if (!SDL_SubmitGPUCommandBuffer(uploadCommand))
                    throw new InvalidOperationException($"SDL GPU mesh submission failed: {SDL_GetError()}");
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Run(AnimationClip idleAnimation, SkinDefinition skin)
        {
            ArgumentNullException.ThrowIfNull(idleAnimation);
            ArgumentNullException.ThrowIfNull(skin);
            if (!ReferenceEquals(idleAnimation.Skeleton, skin.Skeleton))
                throw new ArgumentException("The idle animation and skin must use the same skeleton.");

            Console.WriteLine("STARFALL_CLIENT_CONTROLS Escape=close");
            ulong frequency = SDL_GetPerformanceFrequency();
            if (frequency == 0)
                throw new InvalidOperationException("SDL returned a zero performance-counter frequency.");
            ulong started = SDL_GetPerformanceCounter();

            bool running = true;
            while (running)
            {
                SDL_Event sdlEvent;
                while (SDL_PollEvent(&sdlEvent))
                {
                    if (sdlEvent.Type is SDL_EventType.SDL_EVENT_QUIT or
                        SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED ||
                        sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        sdlEvent.key.key == SDL_Keycode.SDLK_ESCAPE)
                    {
                        running = false;
                    }
                }

                if (!running)
                    break;

                float sampleTime = (float)((SDL_GetPerformanceCounter() - started) / (double)frequency);
                try
                {
                    RenderFrame(idleAnimation, skin, sampleTime);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Starfall character presentation failed for clip '{idleAnimation.Name}' " +
                        $"at sample {sampleTime:F3} seconds (joints={skin.Skeleton.JointCount}).",
                        exception);
                }
                SDL_Delay(16);
            }
        }

        public void Dispose()
        {
            if (device is not null)
                _ = SDL_WaitForGPUIdle(device);
            if (depth is not null && device is not null)
                SDL_ReleaseGPUTexture(device, depth);
            depth = null;
            palette?.Dispose();
            palette = null;
            mesh?.Dispose();
            mesh = null;
            renderer?.Dispose();
            renderer = null;
            if (device is not null)
            {
                if (windowClaimed && window is not null)
                    SDL_ReleaseWindowFromGPUDevice(device, window);
                windowClaimed = false;
                SDL_DestroyGPUDevice(device);
            }
            device = null;
            if (window is not null)
                SDL_DestroyWindow(window);
            window = null;
            SDL_Quit();
        }

        private void RenderFrame(AnimationClip animation, SkinDefinition skin, float sampleTime)
        {
            SkeletonPose pose = AnimationSampler.Sample(animation, sampleTime, AnimationPlaybackMode.Loop);
            SkeletonGlobalPose global = SkeletonPoseEvaluator.EvaluateGlobal(pose);
            SkinningPalette sourcePalette = SkeletonPoseEvaluator.CreateSkinningPalette(skin, global);

            SDL_GPUCommandBuffer* command = AcquireCommand();
            renderer!.UploadPalette(command, palette!, sourcePalette);

            SDL_GPUTexture* swapchain;
            uint swapchainWidth;
            uint swapchainHeight;
            if (!SDL_WaitAndAcquireGPUSwapchainTexture(
                    command,
                    window,
                    &swapchain,
                    &swapchainWidth,
                    &swapchainHeight))
            {
                throw new InvalidOperationException($"SDL GPU swapchain acquisition failed: {SDL_GetError()}");
            }

            if (swapchain is not null)
            {
                EnsureDepth(swapchainWidth, swapchainHeight);
                Matrix4x4 viewProjection = CreateViewProjection(bounds, swapchainWidth, swapchainHeight);
                var colorTarget = new SDL_GPUColorTargetInfo
                {
                    texture = swapchain,
                    clear_color = ClearColor,
                    load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                };
                var depthTarget = new SDL_GPUDepthStencilTargetInfo
                {
                    texture = depth,
                    clear_depth = 1.0f,
                    load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                    store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
                    stencil_load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
                    stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
                };
                SDL_GPURenderPass* pass = SDL_BeginGPURenderPass(command, &colorTarget, 1, &depthTarget);
                if (pass is null)
                    throw new InvalidOperationException($"SDL GPU render pass failed: {SDL_GetError()}");

                for (int section = 0; section < mesh!.SectionCount; section++)
                {
                    Vector4 color = section % 2 == 0
                        ? new Vector4(0.76f, 0.23f, 0.17f, 1.0f)
                        : new Vector4(0.16f, 0.48f, 0.72f, 1.0f);
                    renderer.DrawSection(
                        command,
                        pass,
                        mesh,
                        palette!,
                        section,
                        new SkinnedCharacterDraw(
                            Matrix4x4.Identity,
                            viewProjection,
                            color,
                            new Vector3(-0.35f, -0.70f, -0.62f)));
                }
                SDL_EndGPURenderPass(pass);
            }

            if (!SDL_SubmitGPUCommandBuffer(command))
                throw new InvalidOperationException($"SDL GPU frame submission failed: {SDL_GetError()}");
        }

        private SDL_GPUCommandBuffer* AcquireCommand()
        {
            SDL_GPUCommandBuffer* command = SDL_AcquireGPUCommandBuffer(device);
            if (command is null)
                throw new InvalidOperationException($"SDL GPU command acquisition failed: {SDL_GetError()}");
            return command;
        }

        private void EnsureDepth(uint width, uint height)
        {
            if (depth is not null && depthWidth == width && depthHeight == height)
                return;
            if (depth is not null)
                SDL_ReleaseGPUTexture(device, depth);

            var info = new SDL_GPUTextureCreateInfo
            {
                type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
                format = DepthFormat,
                usage = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET,
                width = width,
                height = height,
                layer_count_or_depth = 1,
                num_levels = 1,
                sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
            };
            depth = SDL_CreateGPUTexture(device, &info);
            if (depth is null)
                throw new InvalidOperationException($"SDL GPU depth texture creation failed: {SDL_GetError()}");
            depthWidth = width;
            depthHeight = height;
        }

        private static SdlGpuSkinnedShaderSet LoadShaders(SDL_GPUShaderFormat format)
        {
            string suffix = format switch
            {
                SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL => ".msl",
                SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV => ".spv",
                _ => throw new NotSupportedException($"Unsupported Starfall shader format: {format}."),
            };
            string entryPoint = format == SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL ? "main0" : "main";
            string vertexPath = Path.Combine(AppContext.BaseDirectory, "shaders", "skinned-character.vert" + suffix);
            string fragmentPath = Path.Combine(AppContext.BaseDirectory, "shaders", "skinned-character.frag" + suffix);
            if (!File.Exists(vertexPath))
                throw new FileNotFoundException($"Starfall vertex shader was not found: {vertexPath}", vertexPath);
            if (!File.Exists(fragmentPath))
                throw new FileNotFoundException($"Starfall fragment shader was not found: {fragmentPath}", fragmentPath);
            return new SdlGpuSkinnedShaderSet(
                format,
                File.ReadAllBytes(vertexPath),
                File.ReadAllBytes(fragmentPath),
                entryPoint);
        }

        private static SDL_GPUShaderFormat SelectShaderFormat(SDL_GPUShaderFormat supported)
        {
            if (supported.HasFlag(SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL))
                return SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL;
            if (supported.HasFlag(SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV))
                return SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV;
            throw new NotSupportedException($"SDL GPU supports no requested shader format. Reported: {supported}.");
        }
    }

    private readonly record struct MeshBounds(Vector3 Minimum, Vector3 Maximum)
    {
        internal Vector3 Center => (Minimum + Maximum) * 0.5f;
        internal Vector3 Extents => (Maximum - Minimum) * 0.5f;
        internal float Radius => Extents.Length();

        internal static MeshBounds Create(IEnumerable<Vector3> positions)
        {
            ArgumentNullException.ThrowIfNull(positions);
            using IEnumerator<Vector3> iterator = positions.GetEnumerator();
            if (!iterator.MoveNext())
                throw new ArgumentException("Mesh bounds require at least one position.", nameof(positions));

            Vector3 minimum = iterator.Current;
            Vector3 maximum = iterator.Current;
            while (iterator.MoveNext())
            {
                minimum = Vector3.Min(minimum, iterator.Current);
                maximum = Vector3.Max(maximum, iterator.Current);
            }
            return new MeshBounds(minimum, maximum);
        }
    }

    private static Matrix4x4 CreateViewProjection(MeshBounds bounds, uint width, uint height)
    {
        if (width == 0 || height == 0)
            throw new ArgumentOutOfRangeException(nameof(width), "Swapchain dimensions must be positive.");
        if (!float.IsFinite(bounds.Radius) || bounds.Radius <= 1e-5f)
            throw new ArgumentException("Mesh bounds must have a positive finite radius.", nameof(bounds));

        const float fieldOfView = MathF.PI / 5.0f;
        Vector3 direction = Vector3.Normalize(new Vector3(1.35f, 0.45f, 2.1f));
        float limitingHalfExtent = MathF.Max(bounds.Extents.Y, bounds.Radius * 0.78f);
        float distance = limitingHalfExtent / MathF.Tan(fieldOfView * 0.5f) * 1.18f;
        Vector3 position = bounds.Center + direction * distance;
        Matrix4x4 view = Matrix4x4.CreateLookAt(position, bounds.Center, Vector3.UnitY);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(
            fieldOfView,
            width / (float)height,
            MathF.Max(0.01f, distance - bounds.Radius * 2.0f),
            distance + bounds.Radius * 3.0f);
        return view * projection;
    }
}
