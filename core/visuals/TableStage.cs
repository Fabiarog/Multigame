using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameHub.Core.Systems;
using GameHub.Core.Graphics;

namespace GameHub.Core.Visuals;

/// <summary>Orthographic 2.5D table: Blender actors, physical card piles and lightweight stage light.</summary>
public partial class TableStage : Control
{
    private SubViewport _viewport;
    private Node3D _world;
    private Camera3D _camera;
    private Node3D _deckPile;
    private Node3D _penaVisual;
    private Tween _penaMotion;
    private Godot.Environment _environment;
    private DirectionalLight3D _key;
    private OmniLight3D _pendant;
    private OmniLight3D _fillLight;
    private OmniLight3D _rimLight;
    private CpuParticles3D _dustParticles;
    private CpuParticles3D _bokehParticles;
    private ShaderMaterial _vignetteMaterial;
    private float _basePendantEnergy = 1.6f;
    private AnimationPlayer _clockAnimator;
    private string _clockClip;
    private bool _clockMoving;
    private readonly List<Node3D> _actors = new(), _hands = new(), _played = new(), _discards = new(), _collecting = new(), _chairs = new();
    private readonly List<Vector3> _positions = new();
    private readonly List<int> _cast = new();
    private readonly List<AnimationPlayer> _animators = new();
    private readonly List<Tween> _motions = new();
    private Node3D _roomInstance;
    private string _currentRoomTheme = "classic_club";
    private readonly List<OmniLight3D> _sconceLights = new();
    private Control _cinema;
    private Label _caption;
    private bool _intro, _skip;
    private float _time;
    public bool IsPresenting => _intro;
    public Vector2I RenderTargetSize => _viewport?.Size ?? Vector2I.Zero;
    public int PlayedCardCount => _played.Count + _discards.Count + _collecting.Count;
    public enum CameraPerspectiveMode { FirstPersonPov, OverheadCinematic }
    public CameraPerspectiveMode CurrentCameraMode { get; set; } = CameraPerspectiveMode.FirstPersonPov;
    public int LocalSeatIndex { get; set; } = 0;
    private float _cameraBreathTime = 0f;
    private float _tactileRecoilY = 0f;
    private Vector2 _lookTarget, _lookAngles, _lastLookPointer;
    private bool _dragLook, _motionWasEnabled;
    private Button _cameraModeButton;
    private readonly Dictionary<int, Tween> _tableActions = new();
    public Vector2 LookAngles => _lookAngles;
    public const float NeckYawLimit = 48f, NeckPitchLimit = 12f;
    public int SeatCount { get; set; } = 2;
    public int RivalIndex { get; set; } = 2;
    public TableStage()
    {
        ClipContents = true;
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
    }

    public override void _Ready()
    {
        _viewport = new SubViewport { Name = "TableViewport", TransparentBg = false, OwnWorld3D = true,
            GuiDisableInput = true, HandleInputLocally = false, Size = new Vector2I(1280, 720) };
        _viewport.Msaa3D = Viewport.Msaa.Msaa4X;
        _viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
        AddChild(_viewport);
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        var picture = new TextureRect {
            Name = "TablePicture", Texture = _viewport.GetTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Linear,
            MouseFilter = MouseFilterEnum.Ignore
        };
        AddChild(picture);
        picture.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var cameraTools = new HBoxContainer { Name = "CameraControls" };
        AddChild(cameraTools);
        cameraTools.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        cameraTools.OffsetLeft = -270; cameraTools.OffsetRight = -8; cameraTools.OffsetTop = 8;
        _cameraModeButton = ClubTheme.Button("Visão: POV [C]");
        _cameraModeButton.CustomMinimumSize = new Vector2(0, 30);
        _cameraModeButton.TooltipText = "C alterna a visão. No POV, segure o botão direito sobre a mesa e arraste para olhar.";
        _cameraModeButton.Pressed += ToggleCameraMode; cameraTools.AddChild(_cameraModeButton);
        var centerView = ClubTheme.Button("Centralizar");
        centerView.CustomMinimumSize = new Vector2(0, 30);
        centerView.Pressed += () => SetLookAngles(0, 0); cameraTools.AddChild(centerView);

        // Cinematic post-processing: vignette, film grain, and warm color grading
        try
        {
            var vigShader = GD.Load<Shader>("res://assets/shaders/TableVignette.gdshader");
            if (vigShader != null)
            {
                _vignetteMaterial = new ShaderMaterial { Shader = vigShader };
                _vignetteMaterial.SetShaderParameter("vignette_radius", 0.72f);
                _vignetteMaterial.SetShaderParameter("vignette_softness", 0.42f);
                _vignetteMaterial.SetShaderParameter("vignette_intensity", 0.35f);
                _vignetteMaterial.SetShaderParameter("grain_intensity", 0.015f);
                _vignetteMaterial.SetShaderParameter("warmth", 0.035f);
                _vignetteMaterial.SetShaderParameter("contrast_boost", 1.04f);
                picture.Material = _vignetteMaterial;
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[TableStage] Failed to initialize vignette shader: {ex.Message}");
        }

        _world = new Node3D(); _viewport.AddChild(_world);

        // Raster lighting with optional screen-space effects; no hardware ray tracing.
        _environment = new Godot.Environment {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color("#080c0a"),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("#92aba2"),
            AmbientLightEnergy = .42f,
            ReflectedLightSource = Godot.Environment.ReflectionSource.Disabled,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            TonemapExposure = 1.10f,

            // SSAO (Screen-Space Ambient Occlusion) for deep contact shadows under furniture and characters
            SsaoEnabled = false,
            SsaoRadius = 0.85f,
            SsaoIntensity = 1.4f,
            SsaoPower = 1.5f,

            // SSR (Screen-Space Reflections) reflecting table, lights and chairs onto polished floors
            SsrEnabled = false,
            SsrMaxSteps = 48,
            SsrFadeIn = 0.15f,
            SsrFadeOut = 1.5f,
            SsrDepthTolerance = 0.3f,

            // Atmospheric bloom for wall sconces and chandelier
            GlowEnabled = false,
            GlowIntensity = 0.38f,
            GlowBloom = 0.16f,
            GlowBlendMode = Godot.Environment.GlowBlendModeEnum.Softlight
        };
        _world.AddChild(new WorldEnvironment { Name = "TableLighting", Environment = _environment });

        // Load default 3D room background
        SetRoomTheme(_currentRoomTheme);
        var clockScene = GD.Load<PackedScene>("res://assets/models/club/club_clock.glb");
        var clock = clockScene.Instantiate<Node3D>();
        clock.Name = "ClubClock";
        clock.Scale = Vector3.One * .78f;
        clock.Position = new Vector3(-3.8f, .52f, -4.4f);
        _world.AddChild(clock);
        foreach (var node in clock.FindChildren("*", "AnimationPlayer", true, false))
        {
            _clockAnimator = (AnimationPlayer)node;
            foreach (string clip in _clockAnimator.GetAnimationList())
                if (clip == "idle" || clip.EndsWith("/idle"))
                {
                    _clockClip = clip;
                    _clockAnimator.GetAnimation(clip).LoopMode = Animation.LoopModeEnum.Linear;
                }
        }

        // Multi-layered table geometry:
        // 1. Walnut outer rim with beveled upper lip
        Cylinder("WalnutRim", 4.75f, .28f, new Color("#2a150e"), new Vector3(0, -.16f, 0), .62f);
        Cylinder("WalnutBevel", 4.76f, .06f, new Color("#1f0f0a"), new Vector3(0, -.03f, 0), .62f);
        // 2. Chesterfield-style padded leather armrest ring
        Cylinder("LeatherInlay", 4.62f, .06f, new Color("#1c120c"), new Vector3(0, -.01f, 0), .62f);
        // 3. Polished brass inlay accent
        Cylinder("BrassInlay", 4.49f, .052f, new Color("#dfaf42"), new Vector3(0, .005f, 0), .62f);
        // 4. Procedural woven felt surface with normal map
        Cylinder("GreenFelt", 4.43f, .065f, new Color("#16543d"), new Vector3(0, .04f, 0), .62f);

        // 5. Ornamental brass studs (24 rivets along the outer rim perimeter)
        var studMat = new StandardMaterial3D { AlbedoColor = new Color("#f2cb62"), Metallic = 0.88f, Roughness = 0.22f };
        var studMesh = new SphereMesh { Radius = 0.026f, Height = 0.048f, RadialSegments = 12, Rings = 6 };
        for (int s = 0; s < 24; s++)
        {
            float angle = s * Mathf.Tau / 24f;
            float sx = Mathf.Cos(angle) * 4.69f;
            float sz = Mathf.Sin(angle) * (4.69f * 0.62f);
            var stud = new MeshInstance3D
            {
                Name = $"Stud_{s}",
                Mesh = studMesh,
                Position = new Vector3(sx, -0.015f, sz),
                MaterialOverride = studMat
            };
            _world.AddChild(stud);
        }

        // Discard felt tray beside the deck
        _world.AddChild(new MeshInstance3D {
            Mesh = new BoxMesh { Size = new Vector3(1.35f, 0.012f, 1.85f) },
            Position = new Vector3(-1.85f, 0.075f, -0.65f),
            MaterialOverride = StageMaterial(new Color("#123828"))
        });

        // Upgraded casino chips with dual stacks and white accent edge rings
        Color[] chipColors1 = { new Color("#c62828"), new Color("#d4af37"), new Color("#1565c0"), new Color("#2e7d32"), new Color("#d4af37") };
        for (int i = 0; i < 5; i++)
        {
            Vector3 chipPos = new Vector3(2.25f, .10f + i * .036f, .8f);
            Cylinder($"Chip_{i}", .16f, .034f, chipColors1[i], chipPos, 1);
            _world.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.125f, BottomRadius = 0.125f, Height = 0.035f, RadialSegments = 32 },
                Position = chipPos,
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#f0ede6"), Roughness = 0.4f, Metallic = 0.05f }
            });
        }

        Color[] chipColors2 = { new Color("#1b1b1b"), new Color("#1565c0"), new Color("#c62828") };
        for (int i = 0; i < 3; i++)
        {
            Vector3 chipPos = new Vector3(2.52f, .10f + i * .036f, .68f);
            Cylinder($"ChipB_{i}", .16f, .034f, chipColors2[i], chipPos, 1);
            _world.AddChild(new MeshInstance3D
            {
                Mesh = new CylinderMesh { TopRadius = 0.125f, BottomRadius = 0.125f, Height = 0.035f, RadialSegments = 32 },
                Position = chipPos,
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color("#f0ede6"), Roughness = 0.4f, Metallic = 0.05f }
            });
        }

        _key = new DirectionalLight3D {
            RotationDegrees = new Vector3(-50, -30, 0),
            LightColor = new Color("#fff2d6"),
            LightEnergy = 1.35f,
            ShadowEnabled = true
        };
        _key.DirectionalShadowMaxDistance = 24;
        _world.AddChild(_key);

        // Warm pendant chandelier directly above table center
        _basePendantEnergy = 1.6f;
        _pendant = new OmniLight3D {
            Position = new Vector3(0, 3.2f, 0.1f),
            LightColor = new Color("#ffeed0"),
            LightEnergy = _basePendantEnergy,
            OmniRange = 9.0f,
            OmniAttenuation = 1.15f,
            ShadowEnabled = true
        };
        _world.AddChild(_pendant);

        // Bounce fill light below the table to soften undercarriage shadows
        _fillLight = new OmniLight3D
        {
            Name = "BounceFillLight",
            Position = new Vector3(0, -1.8f, 0.2f),
            LightColor = new Color("#4a3525"),
            LightEnergy = 0.28f,
            OmniRange = 6.0f,
            OmniAttenuation = 1.4f,
            ShadowEnabled = false
        };
        _world.AddChild(_fillLight);

        // Ambient rim light for character silhouettes
        _rimLight = new OmniLight3D {
            Position = new Vector3(0, 2.6f, -3.5f),
            LightColor = new Color("#6ea8a4"),
            LightEnergy = 0.75f,
            OmniRange = 10,
            ShadowEnabled = false
        };
        _world.AddChild(_rimLight);

        // Ambient atmospheric particles: floating dust motes
        _dustParticles = new CpuParticles3D
        {
            Name = "DustParticles",
            Amount = 45,
            Lifetime = 8.0,
            Preprocess = 4.0,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Box,
            EmissionBoxExtents = new Vector3(3.5f, 1.5f, 2.5f),
            Position = new Vector3(0, 1.8f, 0),
            Gravity = new Vector3(0, -0.015f, 0),
            InitialVelocityMin = 0.02f,
            InitialVelocityMax = 0.05f,
            Mesh = new SphereMesh { Radius = 0.012f, Height = 0.024f, RadialSegments = 8, Rings = 4 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.95f, 0.8f, 0.35f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        _world.AddChild(_dustParticles);

        // Ambient bokeh lights hovering gently near the warm pendant
        _bokehParticles = new CpuParticles3D
        {
            Name = "PendantBokeh",
            Amount = 10,
            Lifetime = 6.0,
            Preprocess = 3.0,
            EmissionShape = CpuParticles3D.EmissionShapeEnum.Sphere,
            EmissionSphereRadius = 1.1f,
            Position = new Vector3(0, 3.1f, 0.1f),
            Gravity = new Vector3(0, 0.008f, 0),
            InitialVelocityMin = 0.01f,
            InitialVelocityMax = 0.035f,
            Mesh = new SphereMesh { Radius = 0.025f, Height = 0.05f, RadialSegments = 8, Rings = 4 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(1f, 0.88f, 0.6f, 0.45f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                BlendMode = BaseMaterial3D.BlendModeEnum.Add,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        _world.AddChild(_bokehParticles);

        _camera = new Camera3D { Name = "MainCamera", KeepAspect = Camera3D.KeepAspectEnum.Width, Current = true };
        _world.AddChild(_camera);
        SetCast(RivalIndex, SeatCount);
        _motionWasEnabled = SettingsManager.Instance?.ReduceMotion != true;

        // Enhanced 12-card deck pile with natural stack jitter, contact shadow, and back detailing
        _deckPile = new Node3D { Name = "DeckPile" }; _world.AddChild(_deckPile);
        var deckShadow = new MeshInstance3D
        {
            Name = "DeckShadow",
            Mesh = new QuadMesh { Size = new Vector2(0.68f, 0.92f) },
            RotationDegrees = new Vector3(-90, 0, 0),
            Position = new Vector3(-1.85f, 0.076f, 0.2f),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0, 0, 0, 0.45f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded
            }
        };
        _deckPile.AddChild(deckShadow);

        for (int i = 0; i < 12; i++)
        {
            float jitterX = (i % 3 - 1) * 0.0035f;
            float jitterZ = (i % 4 - 1.5f) * 0.004f;
            float jitterRot = (i % 5 - 2) * 0.012f;
            var deckCard = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(.58f, .018f, .82f) },
                Position = new Vector3(-1.85f + jitterX, .082f + i * .018f, .2f + jitterZ),
                Rotation = new Vector3(0, jitterRot, 0),
                MaterialOverride = StageMaterial(i == 11 ? ClubTheme.Panel : ClubTheme.Paper)
            };
            if (i == 11)
            {
                var trim = new MeshInstance3D
                {
                    Mesh = new QuadMesh { Size = new Vector2(0.50f, 0.72f) },
                    RotationDegrees = new Vector3(-90, 0, 0),
                    Position = new Vector3(0, 0.010f, 0),
                    MaterialOverride = new StandardMaterial3D { AlbedoColor = ClubTheme.Gold, Roughness = 0.35f, Metallic = 0.6f }
                };
                deckCard.AddChild(trim);
                var innerPanel = new MeshInstance3D
                {
                    Mesh = new QuadMesh { Size = new Vector2(0.46f, 0.68f) },
                    RotationDegrees = new Vector3(-90, 0, 0),
                    Position = new Vector3(0, 0.011f, 0),
                    MaterialOverride = StageMaterial(ClubTheme.Ink)
                };
                deckCard.AddChild(innerPanel);
            }
            _deckPile.AddChild(deckCard);
        }

        ApplyLighting();
        if (GraphicsQualityManager.Instance != null) GraphicsQualityManager.Instance.GraphicsSettingsApplied += ApplyLighting;
        if (VideoSettingsManager.Instance != null) VideoSettingsManager.Instance.VideoSettingsApplied += ApplyLighting;
    }

    /// <summary>
    /// Dynamically switches the 3D room environment model (Classic Club, Barão's Lounge, Dama's Salon, Cyber Casino)
    /// </summary>
    public void SetRoomTheme(string themeId)
    {
        if (string.IsNullOrWhiteSpace(themeId)) themeId = "classic_club";
        _currentRoomTheme = themeId;
        if (_world == null) return;

        if (_roomInstance != null && IsInstanceValid(_roomInstance))
        {
            _roomInstance.QueueFree();
            _roomInstance = null;
        }
        foreach (var light in _sconceLights)
        {
            if (IsInstanceValid(light)) light.QueueFree();
        }
        _sconceLights.Clear();

        string roomPath = $"res://assets/models/club/room_{themeId}.glb";
        if (!ResourceLoader.Exists(roomPath))
            roomPath = "res://assets/models/club/room_classic_club.glb";

        try
        {
            var scene = GD.Load<PackedScene>(roomPath);
            if (scene != null)
            {
                _roomInstance = scene.Instantiate<Node3D>();
                _roomInstance.Name = $"Room_{themeId}";
                _world.AddChild(_roomInstance);

                Color sconceColor = themeId switch
                {
                    "barao_lounge" => new Color("#d4a840"),
                    "dama_salon"   => new Color("#ffaa66"),
                    "cyber_casino" => new Color("#44d4ea"),
                    _              => new Color("#ffdf90")
                };
                float sconceEnergy = themeId == "cyber_casino" ? 1.4f : 1.1f;

                foreach (float sx in new[] { -3.8f, 0.0f, 3.8f })
                {
                    var light = new OmniLight3D
                    {
                        Position = new Vector3(sx, 4.0f, -5.9f),
                        LightColor = sconceColor,
                        LightEnergy = sconceEnergy,
                        OmniRange = 6.5f,
                        OmniAttenuation = 1.25f,
                        ShadowEnabled = false
                    };
                    _world.AddChild(light);
                    _sconceLights.Add(light);
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[TableStage] Error loading room {themeId}: {ex.Message}");
        }

        // Theme-responsive rim lighting
        if (_rimLight != null && IsInstanceValid(_rimLight))
        {
            _rimLight.LightColor = themeId switch
            {
                "cyber_casino" => new Color("#00d4ff"),
                "barao_lounge" => new Color("#f0b830"),
                "dama_salon"   => new Color("#ff6688"),
                _              => new Color("#6ea8a4")
            };
            _rimLight.LightEnergy = themeId == "cyber_casino" ? 0.95f : 0.75f;
        }
    }

    public void ApplyLighting()
    {
        if (_viewport == null) return;
        var settings = GraphicsQualityManager.Instance?.RtSettings;
        bool forward = RenderingServer.GetCurrentRenderingMethod() == "forward_plus";
        bool enhanced = forward && SettingsManager.Instance?.VfxEnabled != false;
        bool detailed = enhanced && settings?.RayTracingEnabled == true;
        _viewport.Scaling3DScale = VideoSettingsManager.Instance?.RenderScale ?? 1;
        _viewport.Msaa3D = detailed ? Viewport.Msaa.Msaa4X : Viewport.Msaa.Msaa2X;
        _viewport.ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled;
        _key.ShadowEnabled = enhanced;
        _pendant.ShadowEnabled = detailed && settings.RtaoQuality >= RayTracingSettings.RtQualityLevel.High;
        _key.DirectionalShadowMaxDistance = 24;
        _key.DirectionalShadowMode = DirectionalLight3D.ShadowMode.Orthogonal;
        _key.ShadowBias = .12f;
        _key.ShadowNormalBias = 1.5f;
        _viewport.PositionalShadowAtlasSize = detailed ? 4096 : 2048;
        if (forward)
        {
            RenderingServer.DirectionalShadowAtlasSetSize(detailed ? 4096 : 2048, false);
            RenderingServer.DirectionalSoftShadowFilterSetQuality(detailed ? RenderingServer.ShadowQuality.SoftMedium : RenderingServer.ShadowQuality.SoftLow);
            RenderingServer.PositionalSoftShadowFilterSetQuality(RenderingServer.ShadowQuality.SoftLow);
        }
        _environment.SsaoEnabled = detailed && settings.RtaoEnabled;
        _environment.SsaoRadius = .85f; _environment.SsaoIntensity = 1.4f;
        _environment.SsrEnabled = detailed && settings.RtReflectionsEnabled;
        _viewport.TransparentBg = false;
        _environment.SsrMaxSteps = settings?.RtReflectionsQuality == RayTracingSettings.RtQualityLevel.Ultra ? 56 : 36;
        _environment.SdfgiEnabled = false;
        _environment.AmbientLightEnergy = enhanced && settings?.RayTracingEnabled == true && settings.RtgiEnabled ? .55f : .42f;
        _environment.GlowEnabled = enhanced;
        _environment.GlowIntensity = .38f;

        bool motion = SettingsManager.Instance?.ReduceMotion != true;
        if (_dustParticles != null && IsInstanceValid(_dustParticles))
            _dustParticles.Emitting = enhanced && motion;
        if (_bokehParticles != null && IsInstanceValid(_bokehParticles))
            _bokehParticles.Emitting = enhanced && motion;
        if (_fillLight != null && IsInstanceValid(_fillLight))
            _fillLight.Visible = enhanced;
        var pic = GetNodeOrNull<TextureRect>("TablePicture");
        if (pic != null && _vignetteMaterial != null)
            pic.Material = enhanced ? _vignetteMaterial : null;
    }

    public void AnimateDeck(bool cutting)
    {
        if (_deckPile == null || SettingsManager.Instance?.ReduceMotion == true) return;
        int i = 0;
        foreach (Node3D card in _deckPile.GetChildren())
        {
            if (card is not MeshInstance3D mi || mi.Mesh is not BoxMesh) continue;
            int index = i++;
            var origin = card.Position;
            var motion = CreateTween();
            _motions.Add(motion);
            if (cutting)
            {
                bool isTop = index >= 6;
                Vector3 cutOffset = isTop
                    ? new Vector3(0.55f, 0.12f, -0.08f)
                    : new Vector3(-0.35f, 0.05f, 0.04f);
                motion.TweenProperty(card, "position", origin + cutOffset, 0.20f)
                    .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                motion.TweenProperty(card, "position", origin, 0.24f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
            else
            {
                var offset = new Vector3(index % 2 == 0 ? -0.32f : 0.32f, 0.10f, 0);
                motion.TweenProperty(card, "position", origin + offset, 0.18f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                motion.TweenProperty(card, "position", origin, 0.32f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            }
        }
    }

    public void AnimateDeal(int dealer, int[] counts, int penaSeat = -1, bool penaKept = false)
    {
        for (int seat = 0; seat < counts.Length; seat++) SetCardCount(seat, penaKept && seat == penaSeat ? 1 : 0);
        if (SettingsManager.Instance?.ReduceMotion == true)
        {
            for (int seat = 0; seat < counts.Length; seat++) SetCardCount(seat, counts[seat]);
            return;
        }
        int emitted = 0;
        for (int pass = 0; pass < 5; pass++)
            for (int offset = 1; offset <= counts.Length; offset++)
            {
                int seat = (dealer + offset) % counts.Length;
                int held = penaKept && seat == penaSeat ? 1 : 0;
                if (pass + held >= counts[seat]) continue;
                var card = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(.30f, .02f, .42f) },
                    Position = new Vector3(-1.85f, .32f, .2f), MaterialOverride = StageMaterial(ClubTheme.Paper) };
                card.AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(.27f, .39f) },
                    Position = new Vector3(0, .011f, 0), MaterialOverride = StageMaterial(ClubTheme.Panel) });
                _world.AddChild(card);
                var start = card.Position; var target = _hands[seat].Position;
                var flight = CreateTween(); _motions.Add(flight);
                flight.TweenInterval(emitted++ * .045f);
                flight.TweenMethod(Callable.From<float>(t => {
                    if (IsInstanceValid(card)) card.Position = start.Lerp(target, t) + Vector3.Up * Mathf.Sin(t * Mathf.Pi) * .3f;
                }), 0f, 1f, .3f);
                int handCount = pass + held + 1;
                flight.TweenCallback(Callable.From(() => { SetCardCount(seat, handCount); if (IsInstanceValid(card)) card.QueueFree(); }));
            }
    }

    public void AnimateDeckPass(int dealer)
    {
        if (_deckPile == null || SettingsManager.Instance?.ReduceMotion == true) return;
        var target = _hands[Mathf.PosMod(dealer, _hands.Count)].Position - new Vector3(-1.85f, .25f, .2f);
        var pass = CreateTween(); _motions.Add(pass);
        pass.TweenProperty(_deckPile, "position", target, .3f).SetTrans(Tween.TransitionType.Cubic);
        pass.TweenInterval(.2f);
        pass.TweenProperty(_deckPile, "position", Vector3.Zero, .3f).SetTrans(Tween.TransitionType.Cubic);
    }

    public void AnimatePenaDelivery(int seat)
    {
        ClearPenaVisual();
        if (seat < 0 || seat >= _hands.Count) return;
        _penaVisual = new MeshInstance3D {
            Name = "PenaInFlight", Position = new Vector3(-1.85f, .32f, .2f),
            Mesh = new BoxMesh { Size = new Vector3(.30f, .02f, .42f) }, MaterialOverride = StageMaterial(ClubTheme.Paper)
        };
        _penaVisual.AddChild(new MeshInstance3D { Mesh = new PlaneMesh { Size = new Vector2(.27f, .39f) },
            Position = new Vector3(0, .011f, 0), MaterialOverride = StageMaterial(ClubTheme.Panel) });
        _world.AddChild(_penaVisual);
        var target = _hands[seat].Position + Vector3.Up * .08f;
        if (SettingsManager.Instance?.ReduceMotion == true) { _penaVisual.Position = target; return; }
        var start = _penaVisual.Position;
        _penaMotion = CreateTween(); _motions.Add(_penaMotion);
        _penaMotion.TweenMethod(Callable.From<float>(t => {
            if (IsInstanceValid(_penaVisual)) _penaVisual.Position = start.Lerp(target, t) + Vector3.Up * Mathf.Sin(t * Mathf.Pi) * .3f;
        }), 0f, 1f, .38f);
    }

    public void AnimatePenaResolution(int seat, bool kept)
    {
        _penaMotion?.Kill();
        if (kept) { ClearPenaVisual(); SetCardCount(seat, 1); return; }
        if (!IsInstanceValid(_penaVisual)) return;
        if (SettingsManager.Instance?.ReduceMotion == true) { ClearPenaVisual(); return; }
        _penaMotion = CreateTween(); _motions.Add(_penaMotion);
        _penaMotion.TweenProperty(_penaVisual, "position", new Vector3(-1.2f, .1f, .2f), .4f).SetTrans(Tween.TransitionType.Cubic);
        _penaMotion.TweenCallback(Callable.From(ClearPenaVisual));
    }

    private void ClearPenaVisual()
    {
        _penaMotion?.Kill(); _penaMotion = null;
        if (IsInstanceValid(_penaVisual)) _penaVisual.QueueFree();
        _penaVisual = null;
    }

    public void PlayTableAction(int seat)
    {
        if (seat < 0 || seat >= _actors.Count || SettingsManager.Instance?.ReduceMotion == true) return;
        if (_tableActions.TryGetValue(seat, out var previous)) previous.Kill();
        var actor = _actors[seat]; var rest = new Vector3(0, actor.Rotation.Y, 0);
        var action = CreateTween(); _motions.Add(action); _tableActions[seat] = action;
        action.TweenProperty(actor, "rotation", rest + new Vector3(.07f, 0, 0), .16f);
        action.TweenProperty(actor, "rotation", rest, .25f);
    }

    private static StandardMaterial3D StageMaterial(Color color) => new() { AlbedoColor = color, Roughness = .75f };
    private void Cylinder(string name, float radius, float height, Color color, Vector3 position, float depth)
    {
        var material = StageMaterial(color);
        if (name == "GreenFelt")
        {
            try
            {
                var feltShader = GD.Load<Shader>("res://assets/shaders/FeltNormal.gdshader");
                if (feltShader != null)
                {
                    var feltMat = new ShaderMaterial { Shader = feltShader };
                    feltMat.SetShaderParameter("felt_color", new Color("#16543d"));
                    feltMat.SetShaderParameter("roughness_base", 0.82f);
                    feltMat.SetShaderParameter("roughness_wear", 0.08f);
                    feltMat.SetShaderParameter("normal_strength", 0.45f);
                    feltMat.SetShaderParameter("fiber_scale", 42.0f);
                    feltMat.SetShaderParameter("wear_scale", 6.0f);
                    _world.AddChild(new MeshInstance3D {
                        Name = name, Position = position, Scale = new Vector3(1, 1, depth),
                        Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 240 },
                        MaterialOverride = feltMat
                    });
                    return;
                }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[TableStage] Felt shader fallback: {ex.Message}");
            }
            material.AlbedoColor = Colors.White;
            material.AlbedoTexture = GD.Load<Texture2D>("res://assets/models/cards/felt.svg");
            material.Roughness = 0.85f;
        }
        if (name == "BrassInlay")
        {
            material.Metallic = .85f;
            material.Roughness = .22f;
        }
        if (name == "WalnutRim")
        {
            material.Roughness = .45f;
        }
        if (name == "WalnutBevel")
        {
            material.Roughness = .38f;
        }
        if (name == "LeatherInlay")
        {
            material.Roughness = .65f;
            material.Metallic = .05f;
        }
        if (name.StartsWith("Chip"))
        {
            material.Roughness = .32f;
            material.Metallic = .12f;
        }
        int segments = name.StartsWith("Chip") ? 64 : 192;
        _world.AddChild(new MeshInstance3D { Name = name, Position = position, Scale = new Vector3(1, 1, depth),
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = segments }, MaterialOverride = material });
    }

    public void SetCast(int rival, int seats)
    {
        RivalIndex = rival; SeatCount = Mathf.Clamp(seats, 2, 6);
        if (_world == null) return;
        ClearPenaVisual();
        foreach (var action in _tableActions.Values) action.Kill();
        _tableActions.Clear();
        foreach (var actor in _actors) actor.QueueFree();
        foreach (var chair in _chairs) chair.QueueFree();
        foreach (var hand in _hands) hand.QueueFree();
        _actors.Clear(); _chairs.Clear(); _hands.Clear(); _positions.Clear(); _cast.Clear(); _animators.Clear();

        // Exact outer perimeter seating around the oval table (rim radius X=4.70, Z=2.914).
        // Seated comfortably OUTSIDE the rim, centered on the table axis, with chairs and hand fans facing inward:
        // 2 Seats: South (Você) -> North (Adversário)
        // 4 Seats: South (Você) -> East (Adversário 1) -> North (Aliado 1) -> West (Adversário 2)
        // 6 Seats: Oval ring (0 South-West -> 1 South-East -> 2 East -> 3 North-East -> 4 North-West -> 5 West)
        Vector3[] positions = SeatCount <= 2
            ? new[] {
                new Vector3(0.0f, -.72f, 3.10f),
                new Vector3(0.0f, -.72f, -3.10f)
              }
            : SeatCount <= 4
                ? new[] {
                    new Vector3(0.0f, -.72f, 3.10f),   // Seat 0: Você (Sul)
                    new Vector3(5.15f, -.72f, 0.0f),   // Seat 1: Adversário 1 (Leste)
                    new Vector3(0.0f, -.72f, -3.10f),  // Seat 2: Aliado 1 (Norte)
                    new Vector3(-5.15f, -.72f, 0.0f)   // Seat 3: Adversário 2 (Oeste)
                  }
                : new[] {
                    new Vector3(-2.45f, -.72f, 2.95f),  // Seat 0: Você (Sul-Oeste)
                    new Vector3(2.45f, -.72f, 2.95f),   // Seat 1: Adversário 1 (Sul-Leste)
                    new Vector3(5.15f, -.72f, 0.0f),    // Seat 2: Aliado 1 (Leste)
                    new Vector3(2.45f, -.72f, -2.95f),  // Seat 3: Adversário 2 (Norte-Leste)
                    new Vector3(-2.45f, -.72f, -2.95f), // Seat 4: Aliado 2 (Norte-Oeste)
                    new Vector3(-5.15f, -.72f, 0.0f)    // Seat 5: Adversário 3 (Oeste)
                  };

        PackedScene chairScene = null;
        try { chairScene = GD.Load<PackedScene>("res://assets/models/club/club_chair.glb"); }
        catch (Exception ex) { GD.PushWarning($"[TableStage] Failed to load chair: {ex.Message}"); }

        for (int seat = 0; seat < SeatCount; seat++)
        {
            int character = seat == 0 ? CharacterCatalog.Find(SettingsManager.Instance?.CharacterId ?? "corvo") :
                seat == 1 ? rival : Mathf.PosMod(rival + seat - 1, CharacterCatalog.PlayableCount);

            Vector3 pos = positions[seat];
            Vector3 toCenter = -new Vector3(pos.X, 0, pos.Z).Normalized();
            float rotY = Mathf.Atan2(toCenter.X, toCenter.Z);

            // 3D Club Armchair placed behind the player (further from the table), facing the table center
            if (chairScene != null)
            {
                var chair = chairScene.Instantiate<Node3D>();
                chair.Name = $"Chair{seat}";
                chair.Position = pos - toCenter * 0.72f;
                chair.Position = new Vector3(chair.Position.X, -.72f, chair.Position.Z);
                chair.Rotation = new Vector3(0, rotY, 0);
                chair.Scale = Vector3.One * 1.28f;
                _world.AddChild(chair);
                _chairs.Add(chair);
            }

            // Actor character seated at the table edge
            var actor = new Node3D { Name = $"Seat{seat}", Position = pos, Scale = Vector3.One * 1.23f };
            actor.Rotation = new Vector3(0, rotY, 0);
            _world.AddChild(actor);
            var model = GD.Load<PackedScene>(CharacterCatalog.ModelPath(character)).Instantiate<Node3D>();
            actor.AddChild(model);
            AnimationPlayer animator = null;
            foreach (var node in model.FindChildren("*", "AnimationPlayer", true, false)) { animator = (AnimationPlayer)node; break; }
            _actors.Add(actor); _animators.Add(animator); _positions.Add(positions[seat]); _cast.Add(character);

            if (animator != null)
            {
                // Configure smooth cross-fading between all animation clips (idle, entrance, truco, victory, etc.)
                foreach (string animA in animator.GetAnimationList())
                {
                    foreach (string animB in animator.GetAnimationList())
                    {
                        if (animA != animB)
                            animator.SetBlendTime(animA, animB, 0.25f);
                    }
                }
                PlayIdle(seat);
            }

            // Hand fan held right at the table edge in front of the character, oriented coaxially with chair and character
            var hand = new Node3D { Position = pos + toCenter * 0.72f + new Vector3(0, 0.95f, 0), Rotation = new Vector3(0, rotY, 0) };
            _world.AddChild(hand); _hands.Add(hand); SetCardCount(seat, 3);
        }
        UpdateCameraPosition();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            if (keyEvent.Keycode == Key.C)
            {
                ToggleCameraMode();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton button && button.ButtonIndex == MouseButton.Right)
        {
            Vector2 globalPos = button.GlobalPosition != Vector2.Zero ? button.GlobalPosition : GetGlobalMousePosition();
            Vector2 localPos = button.Position != Vector2.Zero ? button.Position : GetLocalMousePosition();
            Vector2 scale = Vector2.One;
            if (GetTree()?.Root != null && GetTree().Root.ContentScaleSize.X > 0 && GetTree().Root.ContentScaleSize.Y > 0)
            {
                scale = (Vector2)GetTree().Root.Size / (Vector2)GetTree().Root.ContentScaleSize;
            }
            bool inside = GetGlobalRect().Size.X <= 0 || GetGlobalRect().Size.Y <= 0
                || GetGlobalRect().HasPoint(globalPos)
                || (scale.X > 0 && scale.Y > 0 && GetGlobalRect().HasPoint(globalPos * scale))
                || GetGlobalRect().HasPoint(GetGlobalMousePosition());
            _dragLook = button.Pressed && CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov && inside;
            _lastLookPointer = localPos;
            if (_dragLook) GetViewport().SetInputAsHandled();
        }
        if (@event is InputEventMouseMotion motion && _dragLook)
        {
            Vector2 movement = motion.Relative != Vector2.Zero ? motion.Relative : (motion.Position - _lastLookPointer);
            _lastLookPointer = motion.Position != Vector2.Zero ? motion.Position : GetLocalMousePosition();
            SetLookAngles(_lookTarget.X - movement.X * .18f, _lookTarget.Y - movement.Y * .12f);
            GetViewport().SetInputAsHandled();
        }
    }

    public void SetLookAngles(float yaw, float pitch)
    {
        _lookTarget = new Vector2(Mathf.Clamp(yaw, -NeckYawLimit, NeckYawLimit), Mathf.Clamp(pitch, -NeckPitchLimit, NeckPitchLimit));
    }

    public void ToggleCameraMode()
    {
        _dragLook = false;
        CurrentCameraMode = CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov
            ? CameraPerspectiveMode.OverheadCinematic
            : CameraPerspectiveMode.FirstPersonPov;
        UpdateCameraPosition();
    }

    public void SetLocalSeat(int seatIndex)
    {
        LocalSeatIndex = seatIndex;
        UpdateCameraPosition();
    }

    public void UpdateCameraPosition(float bobY = 0f)
    {
        if (_camera == null || !IsInstanceValid(_camera)) return;

        if (CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov && _positions.Count > 0)
        {
            int seat = Mathf.Clamp(LocalSeatIndex, 0, _positions.Count - 1);
            Vector3 seatPos = _positions[seat];
            Vector3 toCenter = -new Vector3(seatPos.X, 0, seatPos.Z).Normalized();

            // Seat origins are below the felt (-0.72). Keep eyes well above it,
            // with a small neck arc that follows the deliberate look movement.
            var right = toCenter.Cross(Vector3.Up).Normalized();
            float yaw = Mathf.DegToRad(_lookAngles.X);
            float lean = SettingsManager.Instance?.ReduceMotion == true ? 0 : Mathf.Sin(yaw) * .055f;
            Vector3 eyePos = seatPos + Vector3.Up * (2.35f + bobY) + toCenter * .18f + right * lean;
            _camera.Projection = Camera3D.ProjectionType.Perspective;
            _camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
            _camera.Fov = 54.0f;
            _camera.Near = .05f;
            _camera.Position = eyePos;
            _camera.LookAt(new Vector3(0, .65f, 0), Vector3.Up);
            _camera.RotateObjectLocal(Vector3.Up, yaw);
            _camera.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(_lookAngles.Y));
            if (SettingsManager.Instance?.ReduceMotion != true) _camera.RotateObjectLocal(Vector3.Back, -lean * .08f);
            if (_cameraModeButton != null) _cameraModeButton.Text = "Visão: POV [C]";

            // In First-Person POV, keep local player's body and arms visible for tactile immersion,
            // hiding only the head to prevent near-plane camera clipping.
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!IsInstanceValid(_actors[i])) continue;
                _actors[i].Visible = true;
                bool isLocal = (i == seat);
                foreach (Node node in _actors[i].FindChildren("Head*", "Node3D", true, false))
                {
                    if (node is Node3D node3D) node3D.Visible = !isLocal;
                }
            }
            for (int i = 0; i < _chairs.Count; i++)
            {
                if (IsInstanceValid(_chairs[i]))
                    _chairs[i].Visible = true;
            }
            for (int i = 0; i < _hands.Count; i++)
            {
                if (IsInstanceValid(_hands[i]))
                    _hands[i].Visible = (i != seat);
            }
        }
        else
        {
            // Overhead Cinematic View
            _camera.Projection = Camera3D.ProjectionType.Orthogonal;
            _camera.KeepAspect = Camera3D.KeepAspectEnum.Width;
            if (_cameraModeButton != null) _cameraModeButton.Text = "Visão: Mesa [C]";
            _camera.Size = 12.0f;
            _camera.Position = new Vector3(0, 6.2f, 11);
            _camera.LookAt(new Vector3(0, 0.7f, 0), Vector3.Up);

            for (int i = 0; i < _actors.Count; i++)
            {
                if (!IsInstanceValid(_actors[i])) continue;
                _actors[i].Visible = true;
                foreach (Node node in _actors[i].FindChildren("Head*", "Node3D", true, false))
                {
                    if (node is Node3D node3D) node3D.Visible = true;
                }
            }
            for (int i = 0; i < _chairs.Count; i++)
            {
                if (IsInstanceValid(_chairs[i]))
                    _chairs[i].Visible = true;
            }
            for (int i = 0; i < _hands.Count; i++)
            {
                if (IsInstanceValid(_hands[i]))
                    _hands[i].Visible = true;
            }
        }
    }

    public int CharacterAt(int seat)=>_cast.Count==0?0:_cast[Mathf.PosMod(seat,_cast.Count)];
    public int VisibleHandCount(int seat)=>seat>=0&&seat<_hands.Count?_hands[seat].GetChildCount():0;
    public void SetCardCount(int seat,int count)
    {
        if(seat<0||seat>=_hands.Count)return;
        foreach(var child in _hands[seat].GetChildren()){_hands[seat].RemoveChild(child);child.QueueFree();}
        for(int i=0;i<count;i++)
        {
            var card=new MeshInstance3D { Mesh=new BoxMesh {Size=new Vector3(.30f,.42f,.025f)},
                Position=new Vector3((i-(count-1)/2f)*.14f,0,i*.028f),RotationDegrees=new Vector3(-20,0,(i-(count-1)/2f)*-9),MaterialOverride=StageMaterial(ClubTheme.Paper) };
            _hands[seat].AddChild(card);
            card.AddChild(new MeshInstance3D { Mesh=new QuadMesh {Size=new Vector2(.25f,.37f)},Position=new Vector3(0,0,.016f),MaterialOverride=StageMaterial(ClubTheme.Panel) });
        }
    }

    public Vector2 SeatScreenPosition(int seat)
    {
        if(_camera==null||_positions.Count==0)return GetGlobalRect().GetCenter();
        return Project(_positions[Mathf.PosMod(seat,_positions.Count)]+new Vector3(0,1.2f,.45f));
    }
    private Vector2 Project(Vector3 point)=>GlobalPosition+_camera.UnprojectPosition(point)*Size/(Vector2)_viewport.Size;
    public Vector2 DeckScreenPosition()=>_camera==null||Size.X<100||Size.Y<100?GetViewportRect().Size*new Vector2(.52f,.42f):Project(new Vector3(-1.85f,.27f,.2f));

    public void PlayCard(int seat, string display, float delay = 0, string ownerName = "", bool isSpecial = false)
    {
        if (string.IsNullOrWhiteSpace(display) || _world == null) return;
        string suit = display[^1] switch { '♥' => "hearts", '♦' => "diamonds", '♣' => "clubs", _ => "spades" };
        string rank = display[..^1];
        var card = new Node3D { Name = $"Played{_played.Count}_{rank}" };

        // Soft contact shadow blob under the played card
        var shadow = new MeshInstance3D
        {
            Name = "CardShadow",
            Mesh = new QuadMesh { Size = new Vector2(1.30f, 1.82f) },
            RotationDegrees = new Vector3(-90, 0, 0),
            Position = new Vector3(0, -0.016f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0, 0, 0, 0.40f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            }
        };
        card.AddChild(shadow);

        // Outer card body with realistic thickness and geometric subdivisions
        var bodyMesh = new BoxMesh
        {
            Size = new Vector3(1.25f, .032f, 1.76f),
            SubdivideWidth = 2,
            SubdivideDepth = 2
        };
        var body = new MeshInstance3D
        {
            Mesh = bodyMesh,
            MaterialOverride = StageMaterial(ClubTheme.Paper)
        };
        card.AddChild(body);

        // Gold rim if card is a special manilha
        if (isSpecial)
        {
            var goldRim = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(1.32f, .024f, 1.83f) },
                Position = new Vector3(0, -.004f, 0),
                MaterialOverride = StageMaterial(ClubTheme.Gold)
            };
            card.AddChild(goldRim);
        }

        // Card face with high-definition anisotropic texture filtering and glossy finish on manilhas
        var face = new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>($"res://assets/models/cards/{rank}-{suit}.png"),
            Roughness = isSpecial ? 0.30f : 0.82f,
            Metallic = isSpecial ? 0.18f : 0.0f,
            Clearcoat = isSpecial ? 0.65f : 0.0f,
            ClearcoatRoughness = isSpecial ? 0.20f : 0.5f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            TextureRepeat = false,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            EmissionEnabled = false
        };
        card.AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(1.22f, 1.73f) },
            Position = new Vector3(0, .017f, 0),
            MaterialOverride = face
        });

        // Floating nameplate label indicating who played this card and if it is manilha
        if (!string.IsNullOrEmpty(ownerName))
        {
            var label = new Label3D
            {
                Text = isSpecial ? $"★ {ownerName} ★\nMANILHA" : ownerName,
                FontSize = 26,
                OutlineSize = 6,
                OutlineRenderPriority = 1,
                Modulate = isSpecial ? ClubTheme.Gold : ClubTheme.Paper,
                OutlineModulate = ClubTheme.Ink,
                Position = new Vector3(0, 0.08f, -1.05f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                PixelSize = 0.0024f
            };
            card.AddChild(label);
        }

        _world.AddChild(card);
        int n = _played.Count;
        _played.Add(card);

        // Distribute cards widely on the table organized by trick/round so every card remains visible
        int seatsInTrick = SeatCount > 0 ? SeatCount : 2;
        int cardInTrick = n % seatsInTrick;

        Vector3 target;
        if (SeatCount <= 2)
        {
            float x = (cardInTrick - 0.5f) * 1.55f;
            target = new Vector3(x, .095f + n * .015f, -0.25f);
        }
        else if (SeatCount <= 4)
        {
            float x = (cardInTrick - 1.5f) * 1.35f;
            target = new Vector3(x, .095f + n * .015f, -0.25f);
        }
        else
        {
            float rowZ = cardInTrick < 3 ? -0.45f : 0.05f;
            float rowX = ((cardInTrick % 3) - 1.0f) * 1.45f;
            target = new Vector3(rowX, .095f + n * .015f, rowZ);
        }

        Vector3 start = _hands[Mathf.PosMod(seat, _hands.Count)].Position + Vector3.Up * .08f;
        card.Position = start;
        card.Rotation = new Vector3(0, seat * .35f, 0);

        // Parallel to table felt with a subtle natural rotation variance per play
        Vector3 targetRotation = new Vector3(0, (n % 5 - 2) * .07f, 0);

        if (SettingsManager.Instance?.ReduceMotion == true)
        {
            card.Position = target;
            card.Rotation = targetRotation;
            return;
        }

        // Trigger character play_card gesture (or flourish if special manilha)
        PlayGesture(seat, isSpecial ? "flourish" : "play_card");

        var tween = CreateTween();
        _motions.Add(tween);
        tween.TweenInterval(delay);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (IsInstanceValid(card))
            {
                float arc = Mathf.Sin(t * Mathf.Pi) * 0.72f;
                card.Position = start.Lerp(target, t) + Vector3.Up * arc;
            }
        }), 0f, 1f, .42f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(card, "rotation", targetRotation, .42f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            AudioManager.Instance?.PlaySound("play");
            if (seat == LocalSeatIndex && CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov && SettingsManager.Instance?.ReduceMotion != true)
            {
                var recoil = CreateTween();
                _motions.Add(recoil);
                recoil.TweenMethod(Callable.From<float>(v => _tactileRecoilY = v), 0f, -0.016f, 0.05f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                recoil.TweenMethod(Callable.From<float>(v => _tactileRecoilY = v), -0.016f, 0f, 0.16f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }));
    }

    /// <summary>
    /// Gathers all played cards of the current round to the center, flips them face-down ("tombar ao contrário"),
    /// and smoothly slides them into the discard pile beside the deck.
    /// </summary>
    public async Task CollectRoundCardsToDiscard()
    {
        if (_played.Count == 0 || _world == null) return;
        var roundCards = new List<Node3D>(_played);
        _played.Clear();
        _collecting.AddRange(roundCards);

        if (SettingsManager.Instance?.ReduceMotion == true)
        {
            Vector3 discardBase = new Vector3(-1.85f, 0.082f, -0.65f);
            for (int i = 0; i < roundCards.Count; i++)
            {
                var card = roundCards[i];
                if (!IsInstanceValid(card)) continue;
                card.Scale = new Vector3(0.464f, 0.464f, 0.464f);
                card.Position = discardBase + new Vector3(0, _discards.Count * 0.018f, 0);
                card.Rotation = new Vector3(Mathf.Pi, (i % 3 - 1) * 0.06f, 0);
                foreach (var child in card.GetChildren())
                {
                    if (child is Label3D lbl) lbl.Visible = false;
                }
                _discards.Add(card);
            }
            _collecting.Clear();
            return;
        }

        // 1. Juntar as cartas: gather played cards smoothly to the center
        var gatherTween = CreateTween();
        _motions.Add(gatherTween);
        gatherTween.SetParallel(true);
        Vector3 tableCenter = new Vector3(0, 0.30f, 0.15f);

        for (int i = 0; i < roundCards.Count; i++)
        {
            var card = roundCards[i];
            if (!IsInstanceValid(card)) continue;
            Vector3 centerOffset = tableCenter + new Vector3((i - (roundCards.Count - 1) / 2f) * 0.16f, i * 0.015f, 0);
            gatherTween.TweenProperty(card, "position", centerOffset, 0.26f)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);

            // Esmaecer suavemente os rótulos de nome e indicação de MANILHA
            foreach (var child in card.GetChildren())
            {
                if (child is Label3D lbl)
                {
                    gatherTween.TweenProperty(lbl, "modulate:a", 0.0f, 0.20f);
                }
            }
        }

        await ToSignal(gatherTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        // 2. Tombar ao contrário, reduzir para a escala exata do baralho (0.464) e deslizar para a bandeja de descarte
        var discardTween = CreateTween();
        _motions.Add(discardTween);
        discardTween.SetParallel(true);
        Vector3 discardPos = new Vector3(-1.85f, 0.082f, -0.65f);

        for (int i = 0; i < roundCards.Count; i++)
        {
            var card = roundCards[i];
            if (!IsInstanceValid(card)) continue;
            int pileIdx = _discards.Count + i;
            Vector3 target = discardPos + new Vector3(
                (pileIdx % 3 - 1.0f) * 0.015f,
                pileIdx * 0.018f,
                (pileIdx % 4 - 1.5f) * 0.015f
            );
            Vector3 targetRot = new Vector3(Mathf.Pi, (pileIdx % 5 - 2) * 0.06f, 0);

            float delay = i * 0.025f;
            discardTween.TweenProperty(card, "position", target, 0.35f)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            discardTween.TweenProperty(card, "rotation", targetRot, 0.35f)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Quad);
            discardTween.TweenProperty(card, "scale", new Vector3(0.464f, 0.464f, 0.464f), 0.35f)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
        }

        await ToSignal(discardTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        foreach (var card in roundCards)
        {
            if (IsInstanceValid(card)) _discards.Add(card);
        }
        _collecting.Clear();
    }

    public void ClearPlayedCards()
    {
        foreach (var card in _played) if (IsInstanceValid(card)) card.QueueFree();
        foreach (var card in _discards) if (IsInstanceValid(card)) card.QueueFree();
        foreach (var card in _collecting) if (IsInstanceValid(card)) card.QueueFree();
        _played.Clear();
        _discards.Clear();
        _collecting.Clear();
    }

    public void CollectPlayedCards()
    {
        if (SettingsManager.Instance?.ReduceMotion == true) { ClearPlayedCards(); return; }
        var all = new List<Node3D>(_played);
        all.AddRange(_discards);
        all.AddRange(_collecting);
        _played.Clear();
        _discards.Clear();
        _collecting.Clear();

        int index = 0;
        foreach (var card in all)
        {
            if (!IsInstanceValid(card)) continue;
            var tween = CreateTween(); _motions.Add(tween);
            tween.TweenInterval(index++ * .015f);
            tween.TweenProperty(card, "position", new Vector3(-1.85f, .3f, .2f), .30f);
            tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(card)) card.QueueFree(); }));
        }
    }

    public void ShowOutcome(string text)
    {
        var panel=new PanelContainer {MouseFilter=MouseFilterEnum.Ignore};
        panel.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);panel.OffsetTop=-48;
        panel.AddThemeStyleboxOverride("panel",ClubTheme.Box(new Color(.03f,.10f,.08f,.94f),ClubTheme.Gold,8));
        var label=ClubTheme.Label(text,19,ClubTheme.Paper);label.HorizontalAlignment=HorizontalAlignment.Center;panel.AddChild(label);AddChild(panel);
        var tween=CreateTween();_motions.Add(tween);tween.TweenInterval(1.5);tween.TweenProperty(panel,"modulate:a",0f,.2);tween.TweenCallback(Callable.From(panel.QueueFree));
    }

    public void React(int seat)=>PlayGesture(seat,"victory");

    public void PlayIdle(int seat)
    {
        if (seat < 0 || seat >= _animators.Count || SettingsManager.Instance?.ReduceMotion == true) return;
        var player = _animators[seat];
        if (player == null) return;
        foreach (string animation in player.GetAnimationList())
        {
            if (animation.Equals("idle", StringComparison.OrdinalIgnoreCase) || animation.EndsWith("/idle", StringComparison.OrdinalIgnoreCase))
            {
                var anim = player.GetAnimation(animation);
                if (anim != null) anim.LoopMode = Animation.LoopModeEnum.Linear;
                player.Play(animation, 0.25);
                float phase = (seat * 0.65f) % Mathf.Max(0.1f, (float)(anim?.Length ?? 2.6f));
                player.Seek(phase, true);
                return;
            }
        }
    }

    public void PlayGesture(int seat, string clip)
    {
        if (seat < 0 || seat >= _animators.Count || SettingsManager.Instance?.ReduceMotion == true) return;
        var player = _animators[seat];
        if (player == null) return;
        foreach (string animation in player.GetAnimationList())
            if (animation.Equals(clip, StringComparison.OrdinalIgnoreCase) || animation.EndsWith("/" + clip, StringComparison.OrdinalIgnoreCase))
            {
                player.Play(animation, 0.22);
                foreach (string idle in player.GetAnimationList())
                    if (idle.Equals("idle", StringComparison.OrdinalIgnoreCase) || idle.EndsWith("/idle", StringComparison.OrdinalIgnoreCase)) { player.Queue(idle); break; }
                return;
            }
    }

    /// <summary>Entrants move to their places before rules start. Skip and reduced motion do not advance any gameplay turn.</summary>
    public async Task PlayEntrance(bool boss=false)
    {
        if(SettingsManager.Instance?.ReduceMotion==true||_intro)return;
        _intro=true;_skip=false;
        _cinema=new Control {Name="TableEntrance",MouseFilter=MouseFilterEnum.Stop};
        _cinema.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_cinema);
        var banner=new PanelContainer {MouseFilter=MouseFilterEnum.Ignore};banner.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);banner.OffsetTop=-72;
        banner.AddThemeStyleboxOverride("panel",ClubTheme.Box(new Color(.025f,.055f,.05f,.93f),ClubTheme.Gold,12));
        var row=new HBoxContainer();banner.AddChild(row);
        _caption=ClubTheme.Label(boss?"A CASA TEM UM NOVO DESAFIO":"OS JOGADORES ESTÃO CHEGANDO",18,ClubTheme.Gold);_caption.HorizontalAlignment=HorizontalAlignment.Center;_caption.SizeFlagsHorizontal=SizeFlags.ExpandFill;row.AddChild(_caption);_cinema.AddChild(banner);
        var skip=ClubTheme.Button("Pular entrada");skip.CustomMinimumSize=new Vector2(140,36);skip.Pressed+=()=>_skip=true;row.AddChild(skip);
        for(int i=0;i<_actors.Count;i++){_actors[i].Position=_positions[i]+new Vector3(i%2==0?-6:6,0,0);_hands[i].Visible=false;}
        AudioManager.Instance?.PlaySound(boss?"boss-arrival":"arrival");
        for(int seat=0;seat<_actors.Count&&!_skip;seat++)
        {
            _caption.Text=(CharacterCatalog.IsBoss(_cast[seat])?"CHEFE DA MESA  ·  ":"À MESA  ·  ")+CharacterCatalog.Names[_cast[seat]];
            PlayGesture(seat,boss&&CharacterCatalog.IsBoss(_cast[seat])?"boss_intro":"entrance");
            var tween=CreateTween();_motions.Add(tween);tween.TweenProperty(_actors[seat],"position",_positions[seat],.5f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            if(!await WaitPresentation(.62))return;
        }
        if(!_skip && !await WaitPresentation(boss ? .65 : .25))return;
        foreach(var tween in _motions)tween?.Kill();_motions.Clear();
        for(int i=0;i<_actors.Count;i++)
        {
            _actors[i].Position=_positions[i];
            _hands[i].Visible=true;
            PlayIdle(i);
        }
        _cinema.QueueFree();_cinema=null;_intro=false;
    }

    private async Task<bool> WaitPresentation(double seconds)
    {
        double elapsed=0;
        while(elapsed<seconds&&!_skip){await ToSignal(GetTree(),SceneTree.SignalName.ProcessFrame);if(!IsInsideTree())return false;elapsed+=GetProcessDeltaTime();}
        return IsInsideTree();
    }

    public override void _Process(double delta)
    {
        // Canvas stretch keeps layout at 1280x720. Render the embedded world at
        // its physical display size, including window stretch and parent scale.
        // A SubViewportContainer would force this back to logical Control.Size.
        if (_viewport != null && Size.X > 0 && Size.Y > 0)
        {
            var screen = GetViewport().GetFinalTransform() * GetGlobalTransformWithCanvas();
            var pixels = new Vector2I(Mathf.Clamp(Mathf.RoundToInt(Size.X * screen.X.Length()), 2, 7680),
                Mathf.Clamp(Mathf.RoundToInt(Size.Y * screen.Y.Length()), 2, 4320));
            if (_viewport.Size != pixels) _viewport.Size = pixels;
        }
        if (_camera == null) return;
        bool motion = SettingsManager.Instance?.ReduceMotion != true;
        if (motion != _motionWasEnabled)
        {
            for (int seat = 0; seat < _animators.Count; seat++)
            {
                if (motion) PlayIdle(seat);
                else _animators[seat]?.Pause();
            }
            _motionWasEnabled = motion;
        }
        if (_clockAnimator != null && _clockClip != null && motion != _clockMoving)
        {
            if (motion) _clockAnimator.Play(_clockClip);
            else _clockAnimator.Pause();
            _clockMoving = motion;
        }
        if (motion)
        {
            _time += (float)delta;
            if (_pendant != null)
            {
                float flicker = Mathf.Sin(_time * 2.3f) * 0.035f + Mathf.Sin(_time * 7.1f) * 0.015f;
                _pendant.LightEnergy = _basePendantEnergy + flicker;
            }
        }
        if (CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov)
        {
            _lookAngles = _lookAngles.Lerp(_lookTarget, motion ? 1f - Mathf.Exp(-10f * (float)delta) : 1f);
            if (motion)
            {
                _cameraBreathTime += (float)delta * 1.6f;
            }
            UpdateCameraPosition(motion ? (Mathf.Sin(_cameraBreathTime) * .004f + _tactileRecoilY) : 0f);
        }
        else
        {
            _camera.Size = Mathf.Max(11.5f, Size.X / Mathf.Max(1, Size.Y) * 6.2f);
            if (motion)
            {
                _camera.Position = new Vector3(Mathf.Sin(_time * .17f) * .07f, 6.2f, 11);
                _camera.LookAt(new Vector3(0, .7f, 0), Vector3.Up);
            }
            else
            {
                _camera.Position = new Vector3(0, 6.2f, 11);
                _camera.LookAt(new Vector3(0, .7f, 0), Vector3.Up);
            }
        }
    }

    public override void _ExitTree()
    {
        _skip=true;
        foreach(var tween in _motions)tween?.Kill();
        if(GraphicsQualityManager.Instance!=null)GraphicsQualityManager.Instance.GraphicsSettingsApplied-=ApplyLighting;
        if(VideoSettingsManager.Instance!=null)VideoSettingsManager.Instance.VideoSettingsApplied-=ApplyLighting;
        _motions.Clear(); _actors.Clear(); _chairs.Clear(); _animators.Clear(); _hands.Clear(); _played.Clear(); _discards.Clear(); _collecting.Clear();
        _tableActions.Clear();
        foreach (var light in _sconceLights) if (IsInstanceValid(light)) light.QueueFree();
        _sconceLights.Clear();
        if (_dustParticles != null && IsInstanceValid(_dustParticles)) _dustParticles.QueueFree();
        if (_bokehParticles != null && IsInstanceValid(_bokehParticles)) _bokehParticles.QueueFree();
        if (_fillLight != null && IsInstanceValid(_fillLight)) _fillLight.QueueFree();
        if (_rimLight != null && IsInstanceValid(_rimLight)) _rimLight.QueueFree();
        if (_roomInstance != null && IsInstanceValid(_roomInstance)) _roomInstance.QueueFree();
    }
}
