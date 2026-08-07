using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using ChronoFall.CharacterPresentation;
using ChronoFall.CharacterPresentation.SdlGpu;
using ChronoFall.EditorUi.SdlGpu;
using Evergine.Bindings.Imgui;
using SDL;
using Starfall.Client.DevelopmentUi;
using Starfall.Client.Networking;
using Starfall.Content.Monsters;
using Starfall.Content.Zones;
using Starfall.Protocol.Combat;
using Starfall.Protocol.Monsters;
using Starfall.Protocol.Movement;
using static SDL.SDL3;

namespace Starfall.Client;

internal static unsafe class NativeClientPreview
{
    private const int WindowWidth = 1920;
    private const int WindowHeight = 1080;

    internal static void Run(
        CharacterPresentationContent content,
        ConnectedWalkingClientSession? connectedSession = null,
        bool developmentUiInitiallyVisible = true)
    {
        ArgumentNullException.ThrowIfNull(content);
        ConfigureNativeSdl();
        using var session = new PreviewSession(
            content.Cooked.Asset.Mesh,
            content.Bow.Mesh,
            content.Arrow.Mesh,
            visible: true,
            enableDevelopmentUi: true,
            developmentUiInitiallyVisible);
        session.Run(content, connectedSession);
    }

    internal static void CaptureSuite(CharacterPresentationContent content, string outputDirectory)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ConfigureNativeSdl();
        using var session = new PreviewSession(
            content.Cooked.Asset.Mesh,
            content.Bow.Mesh,
            content.Arrow.Mesh,
            visible: false,
            enableDevelopmentUi: false,
            developmentUiInitiallyVisible: false);
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
        private readonly IReadOnlyList<Draft0MonsterPresentationSnapshot> localMonsterSnapshots;
        private readonly Draft0ConnectedMonsterPresentation connectedMonsterPresentation = new();
        private readonly ConnectedBasicArrowSelection basicArrowSelection = new();
        private readonly ProvisionalBasicBowAttachment basicBowAttachment;
        private readonly ProvisionalBasicArrowNockAttachment basicArrowNockAttachment;
        private readonly GroundBounds validGround;
        private readonly SDL_GPUTextureFormat colorFormat;
        private SDL_Window* window;
        private SDL_GPUDevice* device;
        private SdlGpuSkinnedCharacterRenderer? renderer;
        private SdlGpuSkinnedMesh? mesh;
        private SdlGpuSkinningPalette? palette;
        private SdlGpuStaticMeshRenderer? staticRenderer;
        private SdlGpuStaticMesh? staticMesh;
        private SdlGpuStaticMesh? monsterMesh;
        private SdlGpuStaticMesh? bowMesh;
        private SdlGpuStaticMesh? arrowMesh;
        private SdlGpuImGuiBackend? developmentUi;
        private DevelopmentDebugShellState? developmentDebugState;
        private DevelopmentDebugShell? developmentDebugShell;
        private SDL_GPUTexture* depth;
        private uint depthWidth;
        private uint depthHeight;
        private bool windowClaimed;
        private bool reportedConnectedMonsters;

        internal PreviewSession(
            SkinnedMeshDefinition sourceMesh,
            StaticMeshDefinition sourceBowMesh,
            StaticMeshDefinition sourceArrowMesh,
            bool visible,
            bool enableDevelopmentUi,
            bool developmentUiInitiallyVisible)
        {
            ArgumentNullException.ThrowIfNull(sourceMesh);
            ArgumentNullException.ThrowIfNull(sourceBowMesh);
            ArgumentNullException.ThrowIfNull(sourceArrowMesh);
            basicBowAttachment = new ProvisionalBasicBowAttachment(sourceMesh.Skin.Skeleton);
            basicArrowNockAttachment = new ProvisionalBasicArrowNockAttachment(sourceMesh.Skin.Skeleton);
            Draft0GrayboxLayout layout = Draft0GrayboxCatalog.FirstPlayable;
            validGround = layout.Specification.Bounds;
            cameras = new Draft0GrayboxCameraController();
            fixture = new Draft0LocalWalkingFixture(layout.Town.RespawnAnchor);
            graybox = Draft0GrayboxPresentation.Create(layout);
            localMonsterSnapshots = Draft0LocalMonsterFixture.Create(
                layout,
                Draft0StarterMonsterCatalog.FirstPlayable);

            if (!SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO))
                throw new InvalidOperationException($"SDL video initialization failed: {SDL_GetError()}");

            try
            {
                window = SDL_CreateWindow(
                    CreateWindowTitle(null),
                    WindowWidth,
                    WindowHeight,
                    visible
                        ? SDL_WindowFlags.SDL_WINDOW_RESIZABLE
                        : SDL_WindowFlags.SDL_WINDOW_HIDDEN);
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
                if (enableDevelopmentUi)
                {
                    developmentUi = SdlGpuImGuiBackend.Create(
                        window,
                        device,
                        colorFormat,
                        options: new SdlGpuImGuiBackendOptions(
                            ConfigureFonts: static atlas =>
                            {
                                if (atlas is null || ImguiNative.ImFontAtlas_AddFontDefaultBitmap(atlas, null) is null)
                                    throw new InvalidOperationException("Starfall could not add ImGui's default development font.");
                            }));
                    developmentDebugState = new DevelopmentDebugShellState(developmentUiInitiallyVisible);
                    developmentDebugShell = new DevelopmentDebugShell(developmentDebugState);
                    developmentUi.SetMouseInputEnabled(developmentDebugState.IsVisible);
                }
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
                    monsterMesh = staticRenderer.UploadMesh(
                        uploadCommand,
                        Draft0MonsterPlaceholderMesh.Create());
                    bowMesh = staticRenderer.UploadMesh(uploadCommand, sourceBowMesh);
                    arrowMesh = staticRenderer.UploadMesh(uploadCommand, sourceArrowMesh);
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
            CharacterPresentationContent content,
            ConnectedWalkingClientSession? connectedSession)
        {
            ArgumentNullException.ThrowIfNull(content);
            AnimationClip idleAnimation = content.IdleAnimation;
            AnimationClip walkAnimation = content.WalkAnimation;
            SkinDefinition skin = content.Cooked.Asset.Mesh.Skin;
            if (!ReferenceEquals(idleAnimation.Skeleton, skin.Skeleton) ||
                !ReferenceEquals(walkAnimation.Skeleton, skin.Skeleton))
            {
                throw new ArgumentException("The locomotion animations and skin must use the same skeleton.");
            }

            var playback = new TechnicalPlayerLocomotionPlayback(idleAnimation, walkAnimation);
            ConnectedBasicArrowBodyPresentationController? basicArrowBody = connectedSession is null
                ? null
                : new ConnectedBasicArrowBodyPresentationController(
                    content.BowNotchAnimation,
                    content.BowAimAnimation,
                    content.BowShootAnimation);
            ConnectedBasicArrowProjectilePresentationController? basicArrowProjectile = connectedSession is null
                ? null
                : new ConnectedBasicArrowProjectilePresentationController(content.Arrow.Mesh);
            ConnectedBasicArrowBodyPhase reportedBasicArrowPhase = ConnectedBasicArrowBodyPhase.Locomotion;

            Console.WriteLine(
                $"STARFALL_CLIENT_CONTROLS mode={(connectedSession is null ? "local" : "connected")} " +
                "RightClick=move-intent LeftClick=basic-arrow-connected-only " +
                "KPPlus/KPMinus=local-speed " +
                "F1-F7=view Tab=next-view Up/Down=F1-distance T=command-console F12=debug-ui Escape=close");
            if (connectedSession is null)
                Console.WriteLine($"STARFALL_LOCAL_MONSTERS count={localMonsterSnapshots.Count} source=CONTENT-0007");
            SetWindowTitle(CreateWindowTitle(connectedSession));
            EmitCameraDiagnostic(connectedSession);
            ulong frequency = SDL_GetPerformanceFrequency();
            if (frequency == 0)
                throw new InvalidOperationException("SDL returned a zero performance-counter frequency.");
            ulong previousCounter = SDL_GetPerformanceCounter();
            double monsterPresentationSeconds = 0.0;
            double developmentUiSeconds = 0.0;

            bool running = true;
            while (running)
            {
                SDL_Event sdlEvent;
                while (SDL_PollEvent(&sdlEvent))
                {
                    _ = developmentUi?.ProcessEvent(&sdlEvent);
                    ImGuiCaptureState capture = developmentUi?.Capture ?? default;
                    DevelopmentDebugInputOwnership inputOwnership = DevelopmentDebugInputOwnership.Resolve(
                        developmentDebugState?.IsVisible ?? false,
                        developmentDebugState?.Console.IsInputOpen ?? false,
                        capture.WantsMouse,
                        capture.WantsKeyboard,
                        capture.WantsTextInput);
                    if (sdlEvent.Type is SDL_EventType.SDL_EVENT_QUIT or
                        SDL_EventType.SDL_EVENT_WINDOW_CLOSE_REQUESTED)
                    {
                        running = false;
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        sdlEvent.key.key == SDL_Keycode.SDLK_F12)
                    {
                        if (developmentDebugState?.ToggleVisibility(sdlEvent.key.repeat) == true)
                            developmentUi!.SetMouseInputEnabled(developmentDebugState.IsVisible);
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        sdlEvent.key.key == SDL_Keycode.SDLK_ESCAPE &&
                        developmentDebugState?.Console.IsInputOpen == true)
                    {
                        developmentDebugState.Console.CloseInput();
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        developmentDebugState?.Console.IsInputOpen == true &&
                        sdlEvent.key.key is SDL_Keycode.SDLK_UP or SDL_Keycode.SDLK_DOWN)
                    {
                        developmentDebugState.Console.NavigateHistory(
                            sdlEvent.key.key == SDL_Keycode.SDLK_UP ? -1 : 1);
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        sdlEvent.key.key == SDL_Keycode.SDLK_T &&
                        developmentDebugState?.TryOpenConsole(
                            sdlEvent.key.repeat,
                            capture.WantsKeyboard || capture.WantsTextInput) == true)
                    {
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        sdlEvent.key.key == SDL_Keycode.SDLK_ESCAPE &&
                        !inputOwnership.SuppressKeyboardGameplay)
                    {
                        running = false;
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN &&
                        !inputOwnership.SuppressMouseGameplay)
                    {
                        Draft0PointerAction action = sdlEvent.button.Button switch
                        {
                            SDLButton.SDL_BUTTON_LEFT => Draft0PointerControls.Resolve(
                                Draft0PointerButton.Primary,
                                connectedSession is not null),
                            SDLButton.SDL_BUTTON_RIGHT => Draft0PointerControls.Resolve(
                                Draft0PointerButton.Secondary,
                                connectedSession is not null),
                            _ => Draft0PointerAction.None,
                        };
                        if (action == Draft0PointerAction.Move)
                            SubmitMovementIntent(sdlEvent.button, connectedSession);
                        else if (action == Draft0PointerAction.BasicArrow)
                            SubmitBasicArrowIntent(sdlEvent.button, connectedSession!);
                    }
                    else if (sdlEvent.Type == SDL_EventType.SDL_EVENT_KEY_DOWN &&
                        !inputOwnership.SuppressKeyboardGameplay &&
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
                {
                    SetWindowTitle(CreateWindowTitle(connectedSession));
                    if (connectedSession.MonsterSnapshot is { } monsterSnapshot &&
                        connectedMonsterPresentation.Accept(monsterSnapshot, monsterPresentationSeconds))
                    {
                        if (basicArrowSelection.Reconcile(monsterSnapshot))
                        {
                            Console.WriteLine("STARFALL_CLIENT_BASIC_ARROW_SELECTION_CLEARED reason=target-unavailable");
                            SetWindowTitle(CreateWindowTitle(connectedSession));
                        }
                        if (!reportedConnectedMonsters)
                        {
                            reportedConnectedMonsters = true;
                            Console.WriteLine(
                                $"STARFALL_CONNECTED_MONSTERS sequence={monsterSnapshot.Sequence} " +
                                $"tick={monsterSnapshot.SimulationTick} live={monsterSnapshot.LiveMonsters.Length} " +
                                $"defeated={monsterSnapshot.DefeatedMonsters.Length}");
                        }
                    }
                }

                ulong counter = SDL_GetPerformanceCounter();
                double elapsedSeconds = (counter - previousCounter) / (double)frequency;
                previousCounter = counter;
                double presentationElapsed = Math.Min(elapsedSeconds, FixedTickAccumulator.MaximumElapsedSeconds);
                monsterPresentationSeconds += presentationElapsed;
                developmentUiSeconds += presentationElapsed;
                if (connectedSession is not null)
                    DrainDevelopmentCommandResults(connectedSession, developmentUiSeconds);
                if (connectedSession is null)
                    fixedTicks.Advance(elapsedSeconds, fixture.AdvanceTick);
                TechnicalPlayerSnapshot currentSnapshot = connectedSession?.Snapshot ?? fixture.Snapshot;
                TechnicalPlayerPresentationState presentation =
                    TechnicalPlayerPresentationAdapter.Adapt(currentSnapshot);
                DevelopmentDebugSnapshot developmentSnapshot = CreateDevelopmentDebugSnapshot(
                    currentSnapshot,
                    connectedSession);
                playback.SetLocomotion(presentation.Locomotion);
                playback.Advance(
                    presentationElapsed,
                    presentation.Snapshot.VelocityMetresPerSecond.Length());
                SkeletonPose locomotionPose = playback.CreatePose();
                if (connectedSession is not null && basicArrowBody is not null && basicArrowProjectile is not null)
                {
                    if (basicArrowProjectile.ActiveTarget is { } observedTarget &&
                        connectedMonsterPresentation.TryGetLiveWorldCentre(
                            observedTarget,
                            monsterPresentationSeconds,
                            out Vector3 observedTargetPoint))
                    {
                        basicArrowProjectile.ObserveLiveTarget(observedTarget, observedTargetPoint);
                    }
                    while (connectedSession.TryDequeueBasicArrowOutcome(
                               out ConnectedBasicArrowOutcome outcome))
                    {
                        Vector3? targetPoint = connectedMonsterPresentation.TryGetLiveWorldCentre(
                            outcome.TargetEntityId,
                            monsterPresentationSeconds,
                            out Vector3 liveTargetPoint)
                                ? liveTargetPoint
                                : null;
                        basicArrowBody.HandleOutcome(outcome, locomotionPose);
                        basicArrowProjectile.HandleOutcome(outcome, targetPoint);
                    }
                    basicArrowBody.Advance(presentationElapsed, locomotionPose);
                    basicArrowProjectile.Advance(presentationElapsed);
                    while (basicArrowProjectile.TryDequeueImpact(out BasicArrowPresentationImpact impact))
                    {
                        bool flashed = connectedMonsterPresentation.TriggerHitFlash(
                            impact.TargetEntityId,
                            monsterPresentationSeconds);
                        Console.WriteLine(
                            $"STARFALL_CLIENT_BASIC_ARROW_IMPACT sequence={impact.Sequence} " +
                            $"target={impact.TargetEntityId} flashed={flashed} " +
                            $"point=({impact.WorldPoint.X:F3},{impact.WorldPoint.Y:F3},{impact.WorldPoint.Z:F3})");
                    }
                    if (basicArrowBody.Phase != reportedBasicArrowPhase)
                    {
                        reportedBasicArrowPhase = basicArrowBody.Phase;
                        Console.WriteLine(
                            $"STARFALL_CLIENT_BASIC_ARROW_BODY phase={basicArrowBody.Phase} " +
                            $"sequence={basicArrowBody.ActiveSequence?.ToString() ?? "none"} " +
                            $"sample={basicArrowBody.CurrentSampleTime:F3}");
                    }
                }
                SkeletonPose presentationPose = basicArrowBody?.CreatePose(locomotionPose) ?? locomotionPose;
                try
                {
                    RenderFrame(
                        presentationPose,
                        skin,
                        presentation,
                        monsterPresentationSeconds,
                        connectedSession is not null,
                        Math.Max(presentationElapsed, 1.0 / 1000.0),
                        developmentSnapshot,
                        developmentUiSeconds,
                        basicArrowBody,
                        basicArrowProjectile);
                }
                catch (Exception exception)
                {
                    throw new InvalidOperationException(
                        $"Starfall client presentation failed at tick {presentation.Snapshot.Tick} " +
                        $"(joints={skin.Skeleton.JointCount}, view={cameras.CurrentPreset.Name}, " +
                        $"mode={(connectedSession is null ? "local" : "connected")}).",
                        exception);
                }
                DispatchDevelopmentConsoleSubmissions(connectedSession, developmentUiSeconds);
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
                        historicalPresentation,
                        Draft0GrayboxCaptureSuite.AnimationSampleSeconds));
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
            developmentUi?.Dispose();
            developmentUi = null;
            developmentDebugShell = null;
            developmentDebugState = null;
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
            monsterMesh?.Dispose();
            monsterMesh = null;
            bowMesh?.Dispose();
            bowMesh = null;
            arrowMesh?.Dispose();
            arrowMesh = null;
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
            TechnicalPlayerPresentationState presentation,
            double monsterPresentationSeconds,
            bool connectedMonsters,
            double frameDeltaSeconds,
            DevelopmentDebugSnapshot? developmentSnapshot,
            double developmentUiSeconds,
            ConnectedBasicArrowBodyPresentationController? basicArrowBody,
            ConnectedBasicArrowProjectilePresentationController? basicArrowProjectile)
        {
            SkeletonGlobalPose globalPose = SkeletonPoseEvaluator.EvaluateGlobal(pose);
            SkinningPalette sourcePalette = SkeletonPoseEvaluator.CreateSkinningPalette(skin, globalPose);
            ProvisionalBasicBowFrame bowFrame = basicBowAttachment.Evaluate(globalPose, presentation.World);
            Matrix4x4? arrowWorld = null;
            if (basicArrowBody is not null && basicArrowProjectile is not null)
            {
                Vector3 nockWorldPoint = basicArrowNockAttachment.EvaluateWorldPoint(globalPose, presentation.World);
                while (basicArrowBody.TryDequeueReleaseMarker(
                           out BasicArrowPresentationReleaseMarker release))
                {
                    bool started = basicArrowProjectile.HandleRelease(release, nockWorldPoint);
                    Console.WriteLine(
                        $"STARFALL_CLIENT_BASIC_ARROW_RELEASE sequence={release.Sequence} " +
                        $"actor={release.ActorEntityId} target={release.TargetEntityId} " +
                        $"clip=Bow_Shoot sample={release.ShootSampleTime:F3} " +
                        $"frame={release.ShootSampleFrame} projectileStarted={started}");
                }
                if (basicArrowProjectile.TryCreateFrame(nockWorldPoint, out BasicArrowProjectileFrame arrowFrame))
                    arrowWorld = arrowFrame.World;
            }

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
                    BeginDevelopmentUiFrame(swapchainWidth, swapchainHeight, frameDeltaSeconds);
                    RecordFrame(
                        command,
                        swapchain,
                        depth,
                        swapchainWidth,
                        swapchainHeight,
                        sourcePalette,
                        presentation,
                        cameras.CreateCamera(presentation.Snapshot.Position),
                        monsterPresentationSeconds,
                        connectedMonsters,
                        developmentUi,
                        developmentSnapshot,
                        developmentUiSeconds,
                        bowFrame.BowWorldTransform,
                        arrowWorld);
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
            TechnicalPlayerPresentationState presentation,
            double monsterPresentationSeconds)
        {
            SkeletonPose pose = AnimationSampler.Sample(animation, sampleTime, AnimationPlaybackMode.Loop);
            SkeletonGlobalPose globalPose = SkeletonPoseEvaluator.EvaluateGlobal(pose);
            SkinningPalette sourcePalette = SkeletonPoseEvaluator.CreateSkinningPalette(skin, globalPose);
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
                    cameras.CreateCamera(presentation.Snapshot.Position),
                    monsterPresentationSeconds,
                    connectedMonsters: false,
                    developmentUi: null,
                    developmentSnapshot: null,
                    bowWorld: null,
                    arrowWorld: null);
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
            PerspectiveIsometricCamera camera,
            double monsterPresentationSeconds,
            bool connectedMonsters,
            SdlGpuImGuiBackend? developmentUi,
            DevelopmentDebugSnapshot? developmentSnapshot,
            double developmentUiSeconds = 0.0,
            Matrix4x4? bowWorld = null,
            Matrix4x4? arrowWorld = null)
        {
            bool developmentUiBuilding = developmentUi is not null;
            bool developmentUiPrepared = false;
            try
            {
                renderer!.UploadPalette(command, palette!, sourcePalette);
                Matrix4x4 viewProjection = camera.CreateViewProjection(width, height);
                if (developmentUi is not null)
                {
                    if (!developmentSnapshot.HasValue || developmentDebugShell is null)
                        throw new InvalidOperationException("Interactive development UI requires a Starfall debug snapshot and shell.");
                    developmentDebugShell.Draw(developmentSnapshot.Value, developmentUiSeconds);
                    developmentUi.PrepareDrawData(command);
                    developmentUiBuilding = false;
                    developmentUiPrepared = true;
                }

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

                    if (connectedMonsters)
                    {
                        foreach (Draft0MonsterPresentationState monster in
                            connectedMonsterPresentation.CreateLiveStates(monsterPresentationSeconds))
                        {
                            staticRenderer!.Draw(
                                command,
                                pass,
                                monsterMesh!,
                                new StaticMeshDraw(
                                    monster.World,
                                    viewProjection,
                                    monster.BaseColor,
                                    new Vector3(-0.35f, -0.70f, -0.62f)));
                        }

                        foreach (Draft0MonsterDefeatPresentationState monster in
                            connectedMonsterPresentation.CreateDefeatStates(monsterPresentationSeconds))
                        {
                            staticRenderer!.Draw(
                                command,
                                pass,
                                monsterMesh!,
                                new StaticMeshDraw(
                                    monster.World,
                                    viewProjection,
                                    monster.BaseColor,
                                    new Vector3(-0.35f, -0.70f, -0.62f)));
                        }
                    }
                    else
                    {
                        foreach (Draft0MonsterPresentationSnapshot snapshot in localMonsterSnapshots)
                        {
                            Draft0MonsterPresentationState monster =
                                Draft0MonsterPresentationAdapter.Adapt(snapshot, monsterPresentationSeconds);
                            staticRenderer!.Draw(
                                command,
                                pass,
                                monsterMesh!,
                                new StaticMeshDraw(
                                    monster.World,
                                    viewProjection,
                                    monster.BaseColor,
                                    new Vector3(-0.35f, -0.70f, -0.62f)));
                        }
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

                    if (bowWorld is Matrix4x4 resolvedBowWorld)
                    {
                        staticRenderer!.Draw(
                            command,
                            pass,
                            bowMesh!,
                            new StaticMeshDraw(
                                resolvedBowWorld,
                                viewProjection,
                                new Vector3(0.90f, 0.65f, 0.12f),
                                new Vector3(-0.35f, -0.70f, -0.62f)));
                    }

                    if (arrowWorld is Matrix4x4 resolvedArrowWorld)
                    {
                        staticRenderer!.Draw(
                            command,
                            pass,
                            arrowMesh!,
                            new StaticMeshDraw(
                                resolvedArrowWorld,
                                viewProjection,
                                ConnectedBasicArrowProjectilePresentationController.ArrowColor,
                                new Vector3(-0.35f, -0.70f, -0.62f)));
                    }

                }
                finally
                {
                    SDL_EndGPURenderPass(pass);
                }

                if (developmentUi is not null)
                {
                    var developmentUiTarget = new SDL_GPUColorTargetInfo
                    {
                        texture = color,
                        load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                        store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
                    };
                    SDL_GPURenderPass* developmentUiPass = SDL_BeginGPURenderPass(
                        command,
                        &developmentUiTarget,
                        1,
                        null);
                    if (developmentUiPass is null)
                        throw new InvalidOperationException($"SDL GPU development UI render pass failed: {SDL_GetError()}");
                    try
                    {
                        developmentUi.RecordDrawData(command, developmentUiPass);
                        developmentUiPrepared = false;
                    }
                    finally
                    {
                        SDL_EndGPURenderPass(developmentUiPass);
                    }
                }
            }
            catch (Exception exception)
            {
                Exception? cleanupFailure = TryResolveDevelopmentUiFrame(
                    developmentUi,
                    developmentUiBuilding,
                    developmentUiPrepared);
                if (cleanupFailure is not null)
                {
                    throw new AggregateException(
                        "Starfall scene rendering failed and the development UI frame could not be resolved.",
                        exception,
                        cleanupFailure);
                }
                throw;
            }
        }

        private void BeginDevelopmentUiFrame(
            uint pixelWidth,
            uint pixelHeight,
            double frameDeltaSeconds)
        {
            if (developmentUi is null)
                return;

            int logicalWidth;
            int logicalHeight;
            if (!SDL_GetWindowSize(window, &logicalWidth, &logicalHeight))
                throw new InvalidOperationException($"SDL logical window size query failed: {SDL_GetError()}");

            developmentUi.BeginFrame(new SdlGpuImGuiFrameMetrics(
                logicalWidth,
                logicalHeight,
                checked((int)pixelWidth),
                checked((int)pixelHeight),
                frameDeltaSeconds));
        }

        private static Exception? TryResolveDevelopmentUiFrame(
            SdlGpuImGuiBackend? developmentUi,
            bool building,
            bool prepared)
        {
            if (developmentUi is null)
                return null;

            try
            {
                if (prepared)
                    developmentUi.DiscardPreparedDrawData();
                else if (building)
                    developmentUi.EndFrameWithoutRendering();
                return null;
            }
            catch (Exception exception)
            {
                return exception;
            }
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

        private void DrainDevelopmentCommandResults(
            ConnectedWalkingClientSession connectedSession,
            double nowSeconds)
        {
            DevelopmentConsoleState console = developmentDebugState?.Console ??
                throw new InvalidOperationException("Interactive development commands require console state.");
            while (connectedSession.TryDequeueDevelopmentCommandResult(
                       out ConnectedDevelopmentCommandResult? result))
            {
                console.RecordResult(result!, nowSeconds);
            }
        }

        private void DispatchDevelopmentConsoleSubmissions(
            ConnectedWalkingClientSession? connectedSession,
            double nowSeconds)
        {
            DevelopmentConsoleState console = developmentDebugState?.Console ??
                throw new InvalidOperationException("Interactive development commands require console state.");
            while (console.TryDequeueSubmission(out DevelopmentConsoleInvocation? invocation))
            {
                if (connectedSession is null)
                {
                    console.RecordLocalFailure(
                        invocation!,
                        "development commands require a connected world session",
                        nowSeconds);
                    continue;
                }

                try
                {
                    var sequence = connectedSession.SendDevelopmentCommand(
                        invocation!.CommandId,
                        invocation.Arguments);
                    console.RecordCommandSent(sequence, invocation, nowSeconds);
                }
                catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
                {
                    console.RecordLocalFailure(invocation!, exception.Message, nowSeconds);
                }
            }
        }

        private void SubmitBasicArrowIntent(
            SDL_MouseButtonEvent mouseButton,
            ConnectedWalkingClientSession connectedSession)
        {
            int logicalWidth;
            int logicalHeight;
            if (!SDL_GetWindowSize(window, &logicalWidth, &logicalHeight))
                throw new InvalidOperationException($"SDL logical window size query failed: {SDL_GetError()}");

            int drawableWidth;
            int drawableHeight;
            if (!SDL_GetWindowSizeInPixels(window, &drawableWidth, &drawableHeight))
                throw new InvalidOperationException($"SDL drawable window size query failed: {SDL_GetError()}");

            BoundedMonsterSnapshot? snapshot = connectedSession.MonsterSnapshot;
            if (snapshot is null ||
                logicalWidth <= 0 ||
                logicalHeight <= 0 ||
                drawableWidth <= 0 ||
                drawableHeight <= 0 ||
                !float.IsFinite(mouseButton.x) ||
                !float.IsFinite(mouseButton.y) ||
                mouseButton.x < 0.0f ||
                mouseButton.x > logicalWidth ||
                mouseButton.y < 0.0f ||
                mouseButton.y > logicalHeight)
            {
                ClearBasicArrowSelection("no-live-target", connectedSession);
                return;
            }

            TechnicalPlayerSnapshot player = connectedSession.Snapshot!.Value;
            PerspectiveIsometricCamera camera = cameras.CreateCamera(player.Position);
            var normalized = new Vector2(mouseButton.x / logicalWidth, mouseButton.y / logicalHeight);
            if (!camera.TryCreateWorldRay(
                    normalized,
                    (uint)drawableWidth,
                    (uint)drawableHeight,
                    out PerspectiveWorldRay ray) ||
                !basicArrowSelection.SelectOrClear(ray, snapshot))
            {
                ClearBasicArrowSelection("miss", connectedSession);
                return;
            }

            WorldEntityId target = basicArrowSelection.SelectedTarget!.Value;
            CombatCommandSequence sequence = connectedSession.SendBasicArrowIntent(target);
            Console.WriteLine(
                $"STARFALL_CLIENT_BASIC_ARROW_COMMAND sequence={sequence} target={target} " +
                $"snapshot={snapshot.Sequence} tick={snapshot.SimulationTick}");
            SetWindowTitle(CreateWindowTitle(connectedSession));
        }

        private void ClearBasicArrowSelection(
            string reason,
            ConnectedWalkingClientSession connectedSession)
        {
            if (!basicArrowSelection.Clear())
                return;
            Console.WriteLine($"STARFALL_CLIENT_BASIC_ARROW_SELECTION_CLEARED reason={reason}");
            SetWindowTitle(CreateWindowTitle(connectedSession));
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

        private DevelopmentDebugSnapshot CreateDevelopmentDebugSnapshot(
            TechnicalPlayerSnapshot snapshot,
            ConnectedWalkingClientSession? connectedSession) =>
            new(
                Mode: connectedSession is null ? "Local fixture" : "Connected world",
                SessionStatus: connectedSession is null
                    ? "Local authoritative-style fixture"
                    : connectedSession.IsDisconnected
                        ? "Disconnected"
                        : connectedSession.IsReady ? "Ready" : "Admission pending",
                SessionIdentity: connectedSession?.SessionId?.ToString() ?? "not applicable",
                EntityIdentity: snapshot.Identity,
                Tick: snapshot.Tick,
                CameraPreset: cameras.CurrentPreset.Name,
                CameraDistanceMetres: cameras.CurrentDistanceMetres,
                LocalSpeedMetresPerSecond: connectedSession is null ? fixture.SpeedMetresPerSecond : null);

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
                    $"Starfall - Connected Basic Arrow [{cameras.CurrentPreset.Name}] " +
                    $"[entity {connectedSession.Snapshot?.Identity ?? "pending"}] " +
                    $"[target {basicArrowSelection.SelectedTarget?.Value.ToString(CultureInfo.InvariantCulture) ?? "none"}] " +
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
