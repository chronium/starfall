using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using ChronoFall.CharacterPresentation;
using ChronoFall.CharacterPresentation.SdlGpu;
using SDL;
using Starfall.Client.Networking;
using Starfall.Content.Zones;
using static SDL.SDL3;

namespace Starfall.Client;

internal static unsafe class NativeClientPreview
{
    private const int WindowWidth = 1920;
    private const int WindowHeight = 1080;

    internal static void Run(
        CharacterPresentationContent content,
        ConnectedWalkingClientSession? connectedSession = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ConfigureNativeSdl();
        using var session = new PreviewSession(content.Cooked.Asset.Mesh, visible: true);
        session.Run(content.IdleAnimation, content.WalkAnimation, content.Cooked.Asset.Mesh.Skin, connectedSession);
    }

    internal static void CaptureSuite(CharacterPresentationContent content, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ConfigureNativeSdl();
        using var session = new PreviewSession(content.Cooked.Asset.Mesh, visible: false);
        session.CaptureSuite(content.IdleAnimation, content.Cooked.Asset.Mesh.Skin, outputDirectory);
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

        private readonly Draft0GrayboxCameraController cameras;
        private readonly Draft0LocalWalkingFixture fixture;
        private readonly FixedTickAccumulator fixedTicks = new();
        private readonly Draft0GrayboxPresentation graybox;
        private readonly GroundBounds validGround;
        private readonly SDL_GPUTextureFormat colorFormat;
        private SDL_Window* window;
        private SDL_GPUDevice* device;
        private SdlGpuSkinnedCharacterRenderer? renderer;
        private SdlGpuSkinnedMesh? mesh;
        private SdlGpuSkinningPalette? palette;
        private SdlGpuStaticMeshRenderer? staticRenderer;
        private SdlGpuStaticMesh? staticMesh;
        private SDL_GPUTexture* depth;
        private uint depthWidth;
        private uint depthHeight;
        private bool windowClaimed;

        internal PreviewSession(SkinnedMeshDefinition sourceMesh, bool visible)
        {
            ArgumentNullException.ThrowIfNull(sourceMesh);
            Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;
            validGround = layout.Specification.Bounds;
            cameras = new Draft0GrayboxCameraController();
            fixture = new Draft0LocalWalkingFixture(layout.Town.RespawnAnchor);
            graybox = Draft0GrayboxPresentation.Create(layout);

            if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
                throw new InvalidOperationException($"SDL video initialization failed: {SDL_GetError()}");

            try
            {
                window = SDL_CreateWindow(
                    CreateWindowTitle(null),
                    WindowWidth,
                    WindowHeight,
                    visible ? (SDL_WindowFlags)0 : SDL_WindowFlags.SDL_WINDOW_HIDDEN);
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
                colorFormat = SDL_GetGPUSwapchainTextureFormat(device, window);
                renderer = new SdlGpuSkinnedCharacterRenderer(
                    device,
                    colorFormat,
                    DepthFormat,
                    LoadSkinnedShaders(shaderFormat));
                staticRenderer = new SdlGpuStaticMeshRenderer(
                    device,
                    colorFormat,
                    DepthFormat,
                    LoadStaticShaders(shaderFormat));
                SDL_GPUCommandBuffer* uploadCommand = AcquireCommand();
                try
                {
                    mesh = renderer.UploadMesh(uploadCommand, sourceMesh);
                    palette = renderer.CreatePalette(sourceMesh.Skin.Skeleton.JointCount);
                    staticMesh = staticRenderer.UploadMesh(uploadCommand, graybox.Mesh);
                    Exception? submissionFailure = TrySubmitCommand(ref uploadCommand, "mesh upload");
                    if (submissionFailure is not null)
                        throw submissionFailure;
                }
                catch (Exception exception)
                {
                    Exception? cancellationFailure = TryCancelCommand(ref uploadCommand, "mesh upload");
                    if (cancellationFailure is not null)
                    {
                        throw new AggregateException(
                            "Starfall mesh upload failed and its GPU command buffer could not be cancelled.",
                            exception,
                            cancellationFailure);
                    }
                    throw;
                }
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        internal void Run(
            AnimationClip idleAnimation,
            AnimationClip walkAnimation,
            SkinDefinition skin,
            ConnectedWalkingClientSession? connectedSession)
        {
            ArgumentNullException.ThrowIfNull(idleAnimation);
            ArgumentNullException.ThrowIfNull(walkAnimation);
            ArgumentNullException.ThrowIfNull(skin);
            if (!ReferenceEquals(idleAnimation.Skeleton, skin.Skeleton) ||
                !ReferenceEquals(walkAnimation.Skeleton, skin.Skeleton))
            {
                throw new ArgumentException("The locomotion animations and skin must use the same skeleton.");
            }

            var playback = new TechnicalPlayerLocomotionPlayback(idleAnimation, walkAnimation);

            Console.WriteLine(
                $"STARFALL_CLIENT_CONTROLS mode={(connectedSession is null ? "local" : "connected")} " +
                "LeftClick=move-intent KPPlus/KPMinus=local-speed " +
                "F1-F7=view Tab=next-view Up/Down=F1-distance Escape=close");
            SetWindowTitle(CreateWindowTitle(connectedSession));
            EmitCameraDiagnostic(connectedSession);
            ulong frequency = SDL_GetPerformanceFrequency();
            if (frequency == 0)
                throw new InvalidOperationException("SDL returned a zero performance-counter frequency.");
            ulong previousCounter = SDL_GetPerformanceCounter();

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
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN &&
                        sdlEvent.button.Button == SDLButton.SDL_BUTTON_LEFT)
                    {
                        SubmitMovementIntent(sdlEvent.button, connectedSession);
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        HandlePreviewKey(sdlEvent.key.key, sdlEvent.key.repeat, connectedSession is not null))
                    {
                        SetWindowTitle(CreateWindowTitle(connectedSession));
                        EmitCameraDiagnostic(connectedSession);
                    }
                }

                if (!running)
                    break;

                connectedSession?.Poll();
                if (connectedSession is not null)
                    SetWindowTitle(CreateWindowTitle(connectedSession));

                ulong counter = SDL_GetPerformanceCounter();
                double elapsedSeconds = (counter - previousCounter) / (double)frequency;
                previousCounter = counter;
                double presentationElapsed = Math.Min(elapsedSeconds, FixedTickAccumulator.MaximumElapsedSeconds);
                if (connectedSession is null)
                    fixedTicks.Advance(elapsedSeconds, fixture.AdvanceTick);
                TechnicalPlayerSnapshot currentSnapshot = connectedSession?.Snapshot ?? fixture.Snapshot;
                TechnicalPlayerPresentationState presentation =
                    TechnicalPlayerPresentationAdapter.Adapt(currentSnapshot);
                playback.SetLocomotion(presentation.Locomotion);
                playback.Advance(
                    presentationElapsed,
                    presentation.Snapshot.VelocityMetresPerSecond.Length());
                try
                {
                    RenderFrame(playback.CreatePose(), skin, presentation);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Starfall local walking presentation failed at tick {presentation.Snapshot.Tick} " +
                        $"(joints={skin.Skeleton.JointCount}, view={cameras.CurrentPreset.Name}, " +
                        $"mode={(connectedSession is null ? "local" : "connected")}).",
                        exception);
                }
                SDL_Delay(16);
            }
        }

        internal void CaptureSuite(
            AnimationClip idleAnimation,
            SkinDefinition skin,
            string outputDirectory)
        {
            ArgumentNullException.ThrowIfNull(idleAnimation);
            ArgumentNullException.ThrowIfNull(skin);
            ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
            if (!ReferenceEquals(idleAnimation.Skeleton, skin.Skeleton))
                throw new ArgumentException("The idle animation and skin must use the same skeleton.");

            SDL_GPUTexture* captureColor = CreateTexture(
                colorFormat,
                SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET,
                Draft0GrayboxCaptureSuite.Width,
                Draft0GrayboxCaptureSuite.Height);
            SDL_GPUTexture* captureDepth = null;
            try
            {
                captureDepth = CreateTexture(
                    DepthFormat,
                    SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET,
                    Draft0GrayboxCaptureSuite.Width,
                    Draft0GrayboxCaptureSuite.Height);

                var historicalSnapshot = new TechnicalPlayerSnapshot(
                    Draft0LocalWalkingFixture.Identity,
                    tick: 0,
                    new GroundPoint(100.0f, 100.0f),
                    Vector2.Zero,
                    Vector2.UnitY);
                TechnicalPlayerPresentationState historicalPresentation =
                    TechnicalPlayerPresentationAdapter.Adapt(historicalSnapshot);
                var images = new List<RgbaImage>(Draft0GrayboxCaptureSuite.Captures.Count);
                foreach (Draft0GrayboxCapture capture in Draft0GrayboxCaptureSuite.Captures)
                {
                    cameras.SelectPreset(capture.PresetIndex);
                    images.Add(CaptureFrame(
                        idleAnimation,
                        skin,
                        Draft0GrayboxCaptureSuite.AnimationSampleSeconds,
                        captureColor,
                        captureDepth,
                        historicalPresentation));
                }

                IReadOnlyList<ulong> fingerprints = Draft0GrayboxCaptureSuite.Validate(images);
                string fullOutputDirectory = Path.GetFullPath(outputDirectory);
                Directory.CreateDirectory(fullOutputDirectory);
                for (var index = 0; index < Draft0GrayboxCaptureSuite.Captures.Count; index++)
                {
                    Draft0GrayboxCapture capture = Draft0GrayboxCaptureSuite.Captures[index];
                    string path = Path.Combine(fullOutputDirectory, capture.FileName);
                    PngImageWriter.Write(path, images[index]);
                    Console.WriteLine(string.Create(
                        CultureInfo.InvariantCulture,
                        $"STARFALL_GRAYBOX_CAPTURE view={capture.PresetName} " +
                        $"sample={Draft0GrayboxCaptureSuite.AnimationSampleSeconds:F3} " +
                        $"size={images[index].Width}x{images[index].Height} " +
                        $"fingerprint={fingerprints[index]:x16} path={path}"));
                }

                Console.WriteLine(
                    $"STARFALL_GRAYBOX_CAPTURE_SUITE_READY count={images.Count} directory={fullOutputDirectory}");
            }
            finally
            {
                if (captureDepth is not null)
                    SDL_ReleaseGPUTexture(device, captureDepth);
                SDL_ReleaseGPUTexture(device, captureColor);
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
            staticMesh?.Dispose();
            staticMesh = null;
            staticRenderer?.Dispose();
            staticRenderer = null;
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

        private void RenderFrame(
            SkeletonPose pose,
            SkinDefinition skin,
            TechnicalPlayerPresentationState presentation)
        {
            SkinningPalette sourcePalette = EvaluatePalette(pose, skin);

            SDL_GPUCommandBuffer* command = AcquireCommand();
            bool requiresSubmission = false;
            Exception? failure = null;
            try
            {
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
                // Resolve every successful acquisition attempt by submission. SDL forbids cancellation
                // once the command buffer has acquired a non-null swapchain texture.
                requiresSubmission = true;

                if (swapchain is not null)
                {
                    EnsureDepth(swapchainWidth, swapchainHeight);
                    RecordFrame(
                        command,
                        swapchain,
                        depth,
                        swapchainWidth,
                        swapchainHeight,
                        sourcePalette,
                        presentation,
                        cameras.CreateCamera(presentation.Snapshot.Position));
                }
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                Exception? resolutionFailure = requiresSubmission
                    ? TrySubmitCommand(ref command, "frame")
                    : TryCancelCommand(ref command, "frame");
                if (resolutionFailure is not null)
                {
                    failure = CombineFailures(
                        failure,
                        resolutionFailure,
                        "Starfall frame processing and GPU command-buffer resolution both failed.");
                }
            }

            if (failure is not null)
                ExceptionDispatchInfo.Capture(failure).Throw();
        }

        private RgbaImage CaptureFrame(
            AnimationClip animation,
            SkinDefinition skin,
            float sampleTime,
            SDL_GPUTexture* color,
            SDL_GPUTexture* captureDepth,
            TechnicalPlayerPresentationState presentation)
        {
            SkeletonPose pose = AnimationSampler.Sample(animation, sampleTime, AnimationPlaybackMode.Loop);
            SkinningPalette sourcePalette = EvaluatePalette(pose, skin);
            SDL_GPUCommandBuffer* command = AcquireCommand();
            try
            {
                RecordFrame(
                    command,
                    color,
                    captureDepth,
                    Draft0GrayboxCaptureSuite.Width,
                    Draft0GrayboxCaptureSuite.Height,
                    sourcePalette,
                    presentation,
                    cameras.CreateCamera(presentation.Snapshot.Position));
            }
            catch (Exception exception)
            {
                Exception? cancellationFailure = TryCancelCommand(ref command, "capture frame");
                if (cancellationFailure is not null)
                {
                    throw new AggregateException(
                        "Starfall capture rendering failed and its GPU command buffer could not be cancelled.",
                        exception,
                        cancellationFailure);
                }
                throw;
            }

            using SdlGpuReadbackRequest request = SdlGpuTextureReadback.Submit(
                device,
                command,
                color,
                Draft0GrayboxCaptureSuite.Width,
                Draft0GrayboxCaptureSuite.Height,
                colorFormat);
            return request.Wait();
        }

        private void RecordFrame(
            SDL_GPUCommandBuffer* command,
            SDL_GPUTexture* color,
            SDL_GPUTexture* frameDepth,
            uint width,
            uint height,
            SkinningPalette sourcePalette,
            TechnicalPlayerPresentationState presentation,
            PerspectiveIsometricCamera camera)
        {
            renderer!.UploadPalette(command, palette!, sourcePalette);
            Matrix4x4 viewProjection = camera.CreateViewProjection(width, height);
            var colorTarget = new SDL_GPUColorTargetInfo
            {
                texture = color,
                clear_color = ClearColor,
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            };
            var depthTarget = new SDL_GPUDepthStencilTargetInfo
            {
                texture = frameDepth,
                clear_depth = 1.0f,
                load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
                store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
                stencil_load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_DONT_CARE,
                stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_DONT_CARE,
            };
            SDL_GPURenderPass* pass = SDL_BeginGPURenderPass(command, &colorTarget, 1, &depthTarget);
            if (pass is null)
                throw new InvalidOperationException($"SDL GPU render pass failed: {SDL_GetError()}");
            try
            {
                for (var section = 0; section < graybox.Mesh.Sections.Count; section++)
                {
                    staticRenderer!.DrawSection(
                        command,
                        pass,
                        staticMesh!,
                        section,
                        new StaticMeshDraw(
                            Matrix4x4.Identity,
                            viewProjection,
                            graybox.SectionColors[section],
                            new Vector3(-0.35f, -0.70f, -0.62f)));
                }

                for (var section = 0; section < mesh!.SectionCount; section++)
                {
                    Vector4 sectionColor = section % 2 == 0
                        ? new Vector4(0.76f, 0.23f, 0.17f, 1.0f)
                        : new Vector4(0.16f, 0.48f, 0.72f, 1.0f);
                    renderer.DrawSection(
                        command,
                        pass,
                        mesh,
                        palette!,
                        section,
                        new SkinnedCharacterDraw(
                            presentation.World,
                            viewProjection,
                            sectionColor,
                            new Vector3(-0.35f, -0.70f, -0.62f)));
                }
            }
            finally
            {
                SDL_EndGPURenderPass(pass);
            }
        }

        private static SkinningPalette EvaluatePalette(SkeletonPose pose, SkinDefinition skin)
        {
            SkeletonGlobalPose global = SkeletonPoseEvaluator.EvaluateGlobal(pose);
            return SkeletonPoseEvaluator.CreateSkinningPalette(skin, global);
        }

        private SDL_GPUCommandBuffer* AcquireCommand()
        {
            SDL_GPUCommandBuffer* command = SDL_AcquireGPUCommandBuffer(device);
            if (command is null)
                throw new InvalidOperationException($"SDL GPU command acquisition failed: {SDL_GetError()}");
            return command;
        }

        private void SubmitMovementIntent(
            SDL_MouseButtonEvent mouseButton,
            ConnectedWalkingClientSession? connectedSession)
        {
            int logicalWidth;
            int logicalHeight;
            if (!SDL_GetWindowSize(window, &logicalWidth, &logicalHeight))
                throw new InvalidOperationException($"SDL logical window size query failed: {SDL_GetError()}");

            int drawableWidth;
            int drawableHeight;
            if (!SDL_GetWindowSizeInPixels(window, &drawableWidth, &drawableHeight))
                throw new InvalidOperationException($"SDL drawable window size query failed: {SDL_GetError()}");
            if (drawableWidth <= 0 || drawableHeight <= 0)
                return;

            TechnicalPlayerSnapshot currentSnapshot = connectedSession?.Snapshot ?? fixture.Snapshot;
            PerspectiveIsometricCamera camera = cameras.CreateCamera(currentSnapshot.Position);
            if (!GroundMovementInput.TryCreateIntent(
                    camera,
                    validGround,
                    mouseButton.x,
                    mouseButton.y,
                    logicalWidth,
                    logicalHeight,
                    (uint)drawableWidth,
                    (uint)drawableHeight,
                    out GroundMovementIntent intent))
            {
                return;
            }

            if (connectedSession is null)
                fixture.Submit(intent);
            else
                connectedSession.SendMovementIntent(intent.Destination);
            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"STARFALL_CLIENT_MOVE_INTENT x={intent.Destination.XMetres:F3} z={intent.Destination.ZMetres:F3}"));
        }

        private bool HandlePreviewKey(SDL_Keycode key, bool repeated, bool connected)
        {
            if (cameras.HandleKey(key, repeated))
                return true;
            if (connected)
                return false;
            if (!Draft0LocalWalkingControls.TryAdjustSpeed(fixture, key, repeated))
                return false;

            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"STARFALL_CLIENT_FIXTURE_SPEED tenths={fixture.SpeedTenths} " +
                $"metresPerSecond={fixture.SpeedMetresPerSecond:F1}"));
            return true;
        }

        private static Exception? TryCancelCommand(
            ref SDL_GPUCommandBuffer* command,
            string operation)
        {
            SDL_GPUCommandBuffer* ownedCommand = command;
            command = null;
            if (ownedCommand is null || SDL_CancelGPUCommandBuffer(ownedCommand))
                return null;
            return new InvalidOperationException(
                $"SDL GPU {operation} command-buffer cancellation failed: {SDL_GetError()}");
        }

        private static Exception? TrySubmitCommand(
            ref SDL_GPUCommandBuffer* command,
            string operation)
        {
            SDL_GPUCommandBuffer* ownedCommand = command;
            command = null;
            if (ownedCommand is null || SDL_SubmitGPUCommandBuffer(ownedCommand))
                return null;
            return new InvalidOperationException(
                $"SDL GPU {operation} submission failed: {SDL_GetError()}");
        }

        private static Exception CombineFailures(
            Exception? primary,
            Exception secondary,
            string message) =>
            primary is null ? secondary : new AggregateException(message, primary, secondary);

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

        private SDL_GPUTexture* CreateTexture(
            SDL_GPUTextureFormat format,
            SDL_GPUTextureUsageFlags usage,
            int width,
            int height)
        {
            var info = new SDL_GPUTextureCreateInfo
            {
                type = SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D,
                format = format,
                usage = usage,
                width = checked((uint)width),
                height = checked((uint)height),
                layer_count_or_depth = 1,
                num_levels = 1,
                sample_count = SDL_GPUSampleCount.SDL_GPU_SAMPLECOUNT_1,
            };
            SDL_GPUTexture* texture = SDL_CreateGPUTexture(device, &info);
            if (texture is null)
                throw new InvalidOperationException($"SDL GPU capture texture creation failed: {SDL_GetError()}");
            return texture;
        }

        private void EmitCameraDiagnostic(ConnectedWalkingClientSession? connectedSession)
        {
            Draft0GrayboxCameraPreset preset = cameras.CurrentPreset;
            TechnicalPlayerSnapshot currentSnapshot = connectedSession?.Snapshot ?? fixture.Snapshot;
            PerspectiveIsometricCamera camera = cameras.CreateCamera(currentSnapshot.Position);
            Console.WriteLine(string.Create(
                CultureInfo.InvariantCulture,
                $"STARFALL_CLIENT_CAMERA view={preset.Name} key=F{cameras.SelectedIndex + 1} " +
                $"focus=({camera.Focus.XMetres:F1},{camera.Focus.ZMetres:F1}) " +
                $"distance={cameras.CurrentDistanceMetres:F1} " +
                $"mode={(connectedSession is null ? "local" : "connected")} tick={currentSnapshot.Tick}"));
        }

        private string CreateWindowTitle(ConnectedWalkingClientSession? connectedSession) =>
            connectedSession is null
                ? Draft0LocalPreviewTitle.Create(
                    cameras.CurrentPreset.Name,
                    fixture.SpeedTenths,
                    cameras.CurrentDistanceMetres)
                : string.Create(
                    CultureInfo.InvariantCulture,
                    $"Starfall - Connected Walking [{cameras.CurrentPreset.Name}] " +
                    $"[entity {connectedSession.Snapshot?.Identity ?? "pending"}] " +
                    $"[tick {connectedSession.Snapshot?.Tick ?? 0}] " +
                    $"[camera {cameras.CurrentDistanceMetres:F1} m]");

        private void SetWindowTitle(string title)
        {
            byte[] titleBytes = Encoding.UTF8.GetBytes(title + '\0');
            fixed (byte* titlePointer = titleBytes)
            {
                if (!SDL_SetWindowTitle(window, titlePointer))
                    throw new InvalidOperationException($"SDL could not update the Starfall window title: {SDL_GetError()}");
            }
        }

        private static SdlGpuSkinnedShaderSet LoadSkinnedShaders(SDL_GPUShaderFormat format)
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

        private static SdlGpuStaticShaderSet LoadStaticShaders(SDL_GPUShaderFormat format)
        {
            string suffix = format switch
            {
                SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL => ".msl",
                SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV => ".spv",
                _ => throw new NotSupportedException($"Unsupported Starfall shader format: {format}."),
            };
            string entryPoint = format == SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL ? "main0" : "main";
            string vertexPath = Path.Combine(AppContext.BaseDirectory, "shaders", "static-mesh.vert" + suffix);
            string fragmentPath = Path.Combine(AppContext.BaseDirectory, "shaders", "static-mesh.frag" + suffix);
            if (!File.Exists(vertexPath))
                throw new FileNotFoundException($"Starfall static vertex shader was not found: {vertexPath}", vertexPath);
            if (!File.Exists(fragmentPath))
                throw new FileNotFoundException($"Starfall static fragment shader was not found: {fragmentPath}", fragmentPath);
            return new SdlGpuStaticShaderSet(
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
}
