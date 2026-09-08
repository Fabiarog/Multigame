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
    // Shared tempo contract: game state waits for the physical card motion,
    // instead of accepting the next turn while cards are still in flight.
    public const float DealCardInterval = .18f;
    public const float DealFlightDuration = .42f;
    public const float DealSettlingTime = .35f;
    public const float StandardCardFlightDuration = .82f;
    public const float SpecialCardFlightDuration = .65f;

    public static float DealPresentationDuration(int movingCards) =>
        movingCards <= 0 ? 0f : DealFlightDuration + (movingCards - 1) * DealCardInterval + DealSettlingTime;

    public static float CardPresentationDuration(bool special) =>
        (special ? SpecialCardFlightDuration : StandardCardFlightDuration) + .14f;

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
    private static readonly string[] RoomThemeIds = new[] { "classic_club", "barao_lounge", "dama_salon", "cyber_casino" };
    private static readonly string[] RoomThemeNames = new[] { "Salão Clássico", "Lounge do Barão", "Salão da Dama", "Cassino Cyber" };
    private Button _roomThemeButton;
    private Control _inGameSettingsModal;
    private CanvasLayer _inGameSettingsCanvas;
    private Button _pauseCameraToggleBtn;
    private Button _pauseThemeToggleBtn;
    private readonly List<OmniLight3D> _sconceLights = new();
    private OmniLight3D _fireplaceLight;
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
        if (SettingsManager.Instance != null)
        {
            if (!string.IsNullOrWhiteSpace(SettingsManager.Instance.RoomTheme))
                _currentRoomTheme = SettingsManager.Instance.RoomTheme;
            if (SettingsManager.Instance.DefaultCameraMode == "table" || SettingsManager.Instance.DefaultCameraMode == "overview")
                CurrentCameraMode = CameraPerspectiveMode.OverheadCinematic;
            else
                CurrentCameraMode = CameraPerspectiveMode.FirstPersonPov;
        }

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
        var cameraTools = new HBoxContainer { Name = "CameraControls", Visible = false };
        AddChild(cameraTools);
        cameraTools.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        cameraTools.OffsetLeft = -650; cameraTools.OffsetRight = -8; cameraTools.OffsetTop = 8;
        var pauseBtn = ClubTheme.Button("⚙ Ajustes [Esc]");
        pauseBtn.CustomMinimumSize = new Vector2(0, 30);
        pauseBtn.Pressed += ToggleInGameSettings;
        cameraTools.AddChild(pauseBtn);
        _roomThemeButton = ClubTheme.Button("Cenário: Salão Clássico [M]");
        _roomThemeButton.CustomMinimumSize = new Vector2(0, 30);
        _roomThemeButton.Pressed += CycleNextRoomTheme;
        cameraTools.AddChild(_roomThemeButton);
        UpdateRoomThemeButtonText();
        _cameraModeButton = ClubTheme.Button(CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov ? "Visão: POV [C]" : "Visão: Mesa [C]");
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

        // Luxury classic bar cart with decanters, cocktail shaker and crystal glasses
        if (ResourceLoader.Exists("res://assets/models/club/club_bar_cart.glb"))
        {
            try
            {
                var cartScene = GD.Load<PackedScene>("res://assets/models/club/club_bar_cart.glb");
                if (cartScene != null)
                {
                    var barCart = cartScene.Instantiate<Node3D>();
                    barCart.Name = "ClubBarCart";
                    barCart.Position = new Vector3(4.8f, -0.72f, -3.8f);
                    barCart.RotationDegrees = new Vector3(0, -32f, 0);
                    barCart.Scale = Vector3.One * 0.96f;
                    _world.AddChild(barCart);
                }
            }
            catch (Exception ex)
            {
                GD.PushWarning($"[TableStage] Bar cart loading: {ex.Message}");
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

        // Discard felt tray beside the deck (proportional to cards)
        _world.AddChild(new MeshInstance3D {
            Mesh = new BoxMesh { Size = new Vector3(0.66f, 0.012f, 0.90f) },
            Position = new Vector3(-1.85f, 0.075f, -0.55f),
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
        if (SettingsManager.Instance != null && SettingsManager.Instance.RoomTheme != themeId)
        {
            SettingsManager.Instance.RoomTheme = themeId;
            SettingsManager.Instance.SaveSettings();
        }
        UpdateRoomThemeButtonText();

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
        _fireplaceLight = null;

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

                // Back Wall Sconces
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

                // Grand Chandelier Light
                var chLight = new OmniLight3D
                {
                    Position = new Vector3(0.0f, 4.8f, 0.0f),
                    LightColor = sconceColor,
                    LightEnergy = themeId == "cyber_casino" ? 1.55f : 1.25f,
                    OmniRange = 9.0f,
                    OmniAttenuation = 1.15f,
                    ShadowEnabled = false
                };
                _world.AddChild(chLight);
                _sconceLights.Add(chLight);

                // Side Wall Sconces
                foreach (float wx in new[] { -12.8f, 12.8f })
                {
                    foreach (float sz in new[] { 3.5f, -1.5f, -5.5f })
                    {
                        var sideLight = new OmniLight3D
                        {
                            Position = new Vector3(wx, 3.9f, sz),
                            LightColor = sconceColor,
                            LightEnergy = 0.75f,
                            OmniRange = 5.0f,
                            OmniAttenuation = 1.35f,
                            ShadowEnabled = false
                        };
                        _world.AddChild(sideLight);
                        _sconceLights.Add(sideLight);
                    }
                }

                // Dynamic Warm Fireplace Light for Classic Club
                if (themeId == "classic_club")
                {
                    _fireplaceLight = new OmniLight3D
                    {
                        Name = "FireplaceLight",
                        Position = new Vector3(0.0f, 0.85f, -5.75f),
                        LightColor = new Color("#ff6a18"),
                        LightEnergy = 2.2f,
                        OmniRange = 7.8f,
                        OmniAttenuation = 1.25f,
                        ShadowEnabled = false
                    };
                    _world.AddChild(_fireplaceLight);
                    _sconceLights.Add(_fireplaceLight);
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

    public void CycleNextRoomTheme()
    {
        int currentIdx = Array.IndexOf(RoomThemeIds, _currentRoomTheme);
        if (currentIdx < 0) currentIdx = 0;
        int nextIdx = (currentIdx + 1) % RoomThemeIds.Length;
        SetRoomTheme(RoomThemeIds[nextIdx]);
    }

    private void UpdateRoomThemeButtonText()
    {
        if (_roomThemeButton == null) return;
        int idx = Array.IndexOf(RoomThemeIds, _currentRoomTheme);
        string name = idx >= 0 ? RoomThemeNames[idx] : "Salão Clássico";
        _roomThemeButton.Text = $"Cenário: {name} [M]";
        _roomThemeButton.TooltipText = "M alterna o cenário (Salão Clássico, Lounge do Barão, Salão da Dama, Cassino Cyber).";
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
        var cards = new List<MeshInstance3D>();
        foreach (Node3D child in _deckPile.GetChildren())
        {
            if (child is MeshInstance3D mi && mi.Mesh is BoxMesh) cards.Add(mi);
        }
        if (cards.Count == 0) return;

        if (cutting)
        {
            // Corte realista do baralho: metade superior ergue, desloca lateralmente,
            // desce para baixo do baralho e fecha o montinho certinho
            int half = cards.Count / 2;
            for (int idx = 0; idx < cards.Count; idx++)
            {
                var card = cards[idx];
                var orig = card.Position;
                bool isTop = idx >= half;
                if (isTop)
                {
                    Vector3 upPos = orig + new Vector3(0.24f, 0.14f, -0.04f);
                    int newIdx = idx - half;
                    Vector3 underPos = new Vector3(orig.X, 0.082f + newIdx * 0.018f, orig.Z);

                    var seq = CreateTween();
                    _motions.Add(seq);
                    seq.TweenProperty(card, "position", upPos, 0.18f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                    seq.TweenProperty(card, "position", orig, 0.22f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                }
                else
                {
                    var seq = CreateTween();
                    _motions.Add(seq);
                    seq.TweenProperty(card, "position", orig + new Vector3(-0.12f, 0.03f, 0.02f), 0.16f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                    seq.TweenProperty(card, "position", orig, 0.22f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
                }
            }
        }
        else
        {
            for (int idx = 0; idx < cards.Count; idx++)
            {
                var card = cards[idx];
                var origin = card.Position;
                var motion = CreateTween();
                _motions.Add(motion);
                var offset = new Vector3(idx % 2 == 0 ? -0.28f : 0.28f, 0.08f, 0);
                motion.TweenProperty(card, "position", origin + offset, 0.18f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                motion.TweenProperty(card, "position", origin, 0.28f)
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
                flight.TweenInterval(emitted++ * DealCardInterval);
                flight.TweenMethod(Callable.From<float>(t => {
                    if (IsInstanceValid(card))
                    {
                        card.Position = start.Lerp(target, t) + Vector3.Up * Mathf.Sin(t * Mathf.Pi) * .35f;
                        card.Rotation = new Vector3(0, t * Mathf.Pi * 0.5f + seat * 0.2f, 0);
                    }
                }), 0f, 1f, DealFlightDuration).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
                int handCount = pass + held + 1;
                flight.TweenCallback(Callable.From(() => {
                    SetCardCount(seat, handCount);
                    AudioManager.Instance?.PlaySound("deal");
                    if (seat != dealer) PlayTableAction(seat);
                    if (IsInstanceValid(card)) card.QueueFree();
                }));
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
        // If this actor has an active skeletal animator, let the rich bezier skeleton gesture play without root jerk
        if (seat < _animators.Count && _animators[seat] != null && _animators[seat].IsPlaying()) return;
        if (_tableActions.TryGetValue(seat, out var previous)) previous.Kill();
        var actor = _actors[seat];
        float rotY = actor.Rotation.Y;
        Vector3 forwardOffset = new Vector3(Mathf.Sin(rotY) * 0.045f, 0, Mathf.Cos(rotY) * 0.045f);
        var action = CreateTween(); _motions.Add(action); _tableActions[seat] = action;
        action.TweenProperty(actor, "position", _positions[seat] + forwardOffset, .16f);
        action.TweenProperty(actor, "position", _positions[seat], .25f);
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
                new Vector3(0.0f, -.72f, 2.75f),
                new Vector3(0.0f, -.72f, -2.50f)
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
                // Configure smooth cross-fading and looping idle for all character models
                foreach (string animA in animator.GetAnimationList())
                {
                    var a = animator.GetAnimation(animA);
                    if (a != null && (animA.Equals("idle", StringComparison.OrdinalIgnoreCase) || animA.EndsWith("/idle", StringComparison.OrdinalIgnoreCase)))
                    {
                        a.LoopMode = Animation.LoopModeEnum.Linear;
                    }
                    foreach (string animB in animator.GetAnimationList())
                    {
                        if (animA != animB)
                            animator.SetBlendTime(animA, animB, 0.25f);
                    }
                }
                PlayIdle(seat);
            }

            // Hand fan held right at the character's hands in front of their chest
            var hand = new Node3D { Position = pos + toCenter * 0.44f + new Vector3(0, 0.88f, 0), Rotation = new Vector3(0, rotY, 0) };
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
            else if (keyEvent.Keycode == Key.M)
            {
                CycleNextRoomTheme();
                GetViewport().SetInputAsHandled();
            }
            else if (keyEvent.Keycode == Key.Escape)
            {
                ToggleInGameSettings();
                GetViewport().SetInputAsHandled();
            }
        }
    }

    public void ToggleInGameSettings()
    {
        if (_inGameSettingsModal == null)
        {
            BuildInGameSettingsModal();
        }
        _inGameSettingsModal.Visible = !_inGameSettingsModal.Visible;
        if (_inGameSettingsModal.Visible)
        {
            UpdateInGameSettingsUI();
        }
    }

    private void UpdateInGameSettingsUI()
    {
        if (_pauseCameraToggleBtn != null)
        {
            string camMode = CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov ? "Primeira Pessoa (POV)" : "Visão Aérea (Mesa)";
            _pauseCameraToggleBtn.Text = $"Câmera: {camMode}";
        }
        if (_pauseThemeToggleBtn != null)
        {
            int idx = Array.IndexOf(RoomThemeIds, _currentRoomTheme);
            string name = idx >= 0 ? RoomThemeNames[idx] : "Salão Clássico";
            _pauseThemeToggleBtn.Text = $"Cenário: {name}";
        }
    }

    private void BuildInGameSettingsModal()
    {
        _inGameSettingsCanvas = new CanvasLayer
        {
            Name = "InGameSettingsCanvas",
            Layer = 120
        };
        AddChild(_inGameSettingsCanvas);

        _inGameSettingsModal = new ColorRect
        {
            Name = "InGameSettingsModal",
            Color = new Color(0.02f, 0.04f, 0.035f, 0.88f),
            Visible = false,
            MouseFilter = MouseFilterEnum.Stop
        };
        _inGameSettingsModal.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _inGameSettingsCanvas.AddChild(_inGameSettingsModal);

        var center = new CenterContainer();
        center.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _inGameSettingsModal.AddChild(center);

        var panel = new PanelContainer { CustomMinimumSize = new Vector2(440, 460) };
        panel.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Panel, ClubTheme.Gold, 24, 12));
        center.AddChild(panel);

        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 14);
        panel.AddChild(box);

        var title = ClubTheme.Label("Ajustes da Partida", 26, ClubTheme.Gold);
        title.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        title.HorizontalAlignment = HorizontalAlignment.Center;
        box.AddChild(title);

        box.AddChild(new ColorRect { Color = ClubTheme.Border, CustomMinimumSize = new Vector2(0, 1) });

        // Camera mode toggle
        box.AddChild(ClubTheme.Label("VISÃO DA MESA", 11, ClubTheme.Gold));
        _pauseCameraToggleBtn = ClubTheme.Button("Câmera: Primeira Pessoa (POV)");
        _pauseCameraToggleBtn.CustomMinimumSize = new Vector2(0, 38);
        _pauseCameraToggleBtn.Pressed += () => {
            ToggleCameraMode();
            UpdateInGameSettingsUI();
        };
        box.AddChild(_pauseCameraToggleBtn);

        // Room Theme toggle
        box.AddChild(ClubTheme.Label("CENÁRIO 3D", 11, ClubTheme.Gold));
        _pauseThemeToggleBtn = ClubTheme.Button("Cenário: Salão Clássico");
        _pauseThemeToggleBtn.CustomMinimumSize = new Vector2(0, 38);
        _pauseThemeToggleBtn.Pressed += () => {
            CycleNextRoomTheme();
            UpdateInGameSettingsUI();
        };
        box.AddChild(_pauseThemeToggleBtn);

        // Sliders for volume
        box.AddChild(ClubTheme.Label("ÁUDIO", 11, ClubTheme.Gold));
        var volBox = new VBoxContainer();
        volBox.AddThemeConstantOverride("separation", 8);
        box.AddChild(volBox);

        var masterRow = new HBoxContainer();
        masterRow.AddChild(ClubTheme.Label("Geral", 13));
        var masterSlider = new HSlider { MinValue = 0, MaxValue = 100, Value = (SettingsManager.Instance?.MasterVolume ?? 1f) * 100, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        masterSlider.ValueChanged += v => {
            if (SettingsManager.Instance != null) { SettingsManager.Instance.MasterVolume = (float)v / 100f; SettingsManager.Instance.ApplySettings(); }
        };
        masterRow.AddChild(masterSlider);
        volBox.AddChild(masterRow);

        var musicRow = new HBoxContainer();
        musicRow.AddChild(ClubTheme.Label("Música", 13));
        var musicSlider = new HSlider { MinValue = 0, MaxValue = 100, Value = (SettingsManager.Instance?.MusicVolume ?? 0.8f) * 100, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        musicSlider.ValueChanged += v => {
            if (SettingsManager.Instance != null) { SettingsManager.Instance.MusicVolume = (float)v / 100f; SettingsManager.Instance.ApplySettings(); }
        };
        musicRow.AddChild(musicSlider);
        volBox.AddChild(musicRow);

        // Accessibility toggle
        var reduceToggle = new CheckButton {
            Text = "Reduzir animações, tremores e esperas",
            ButtonPressed = SettingsManager.Instance?.ReduceMotion == true
        };
        reduceToggle.Toggled += pressed => {
            if (SettingsManager.Instance != null) { SettingsManager.Instance.ReduceMotion = pressed; SettingsManager.Instance.SaveSettings(); }
        };
        box.AddChild(reduceToggle);

        box.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });

        // Action buttons
        var btnRow = new HBoxContainer();
        btnRow.AddThemeConstantOverride("separation", 12);
        box.AddChild(btnRow);

        var resumeBtn = ClubTheme.Button("Continuar", true);
        resumeBtn.CustomMinimumSize = new Vector2(0, 44);
        resumeBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        resumeBtn.Pressed += () => _inGameSettingsModal.Visible = false;
        btnRow.AddChild(resumeBtn);

        var quitBtn = ClubTheme.Button("Sair para o Menu");
        quitBtn.CustomMinimumSize = new Vector2(0, 44);
        quitBtn.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        quitBtn.Pressed += () => {
            if (SettingsManager.Instance != null) SettingsManager.Instance.SaveSettings();
            GetTree().ChangeSceneToFile("res://hub/scenes/HubMain.tscn");
        };
        btnRow.AddChild(quitBtn);
    }

    public void ShowReactionBubble(int seat, string emotion, float duration = 1.8f)
    {
        if (seat < 0 || seat >= _positions.Count || _camera == null) return;
        var bubble = new PanelContainer();
        bubble.AddThemeStyleboxOverride("panel", ClubTheme.Box(ClubTheme.Ink, ClubTheme.Gold, 6, 8));
        var content = new HBoxContainer();
        content.AddThemeConstantOverride("separation", 6);
        bubble.AddChild(content);

        int charId = CharacterAt(seat);
        var portrait = new TextureRect
        {
            Texture = CharacterCatalog.Portrait(charId, react: true),
            CustomMinimumSize = new Vector2(28, 28),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        content.AddChild(portrait);

        string displayEmotion = emotion switch
        {
            "truco"   => "💥 TRUCO!",
            "blefe"   => "😏 Blefe...",
            "tensao"  => "😰 Tensão!",
            "vitoria" => "🏆 Boa!",
            _         => emotion
        };
        content.AddChild(ClubTheme.Label(displayEmotion, 13, ClubTheme.Gold));
        AddChild(bubble);

        Vector3 headPos = _positions[seat] + Vector3.Up * 2.1f;
        Vector2 screenPos = _camera.UnprojectPosition(headPos);
        bubble.Position = screenPos - new Vector2(50, 40);
        bubble.Modulate = new Color(1, 1, 1, 0);

        var tween = CreateTween();
        tween.TweenProperty(bubble, "modulate:a", 1.0f, 0.15f);
        tween.TweenProperty(bubble, "position:y", bubble.Position.Y - 12, 0.20f).SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        tween.TweenInterval(duration);
        tween.TweenProperty(bubble, "modulate:a", 0.0f, 0.25f);
        tween.TweenCallback(Callable.From(() => {
            if (IsInstanceValid(bubble)) bubble.QueueFree();
        }));
    }

    public override void _Input(InputEvent @event)
    {
        if (_intro && @event is InputEventKey key && key.Pressed && !key.Echo)
        {
            if (key.Keycode == Key.Escape || key.Keycode == Key.Space)
            {
                _skip = true;
                GetViewport().SetInputAsHandled();
                return;
            }
        }
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
            Vector3 eyePos = seatPos + Vector3.Up * (2.35f + bobY) - toCenter * 0.70f + right * (0.35f + lean);
            _camera.Projection = Camera3D.ProjectionType.Perspective;
            _camera.KeepAspect = Camera3D.KeepAspectEnum.Height;
            _camera.Fov = 48.0f;
            _camera.Near = .05f;
            _camera.Position = eyePos;
            _camera.LookAt(new Vector3(0, .72f, -0.35f), Vector3.Up);
            _camera.RotateObjectLocal(Vector3.Up, yaw);
            _camera.RotateObjectLocal(Vector3.Right, Mathf.DegToRad(_lookAngles.Y));
            if (SettingsManager.Instance?.ReduceMotion != true) _camera.RotateObjectLocal(Vector3.Back, -lean * .08f);
            if (_cameraModeButton != null) _cameraModeButton.Text = "Visão: POV [C]";

            // In first-person POV, the local player's mesh and chair are hidden to avoid clipping into the eye camera
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!IsInstanceValid(_actors[i])) continue;
                bool isLocal = (i == seat);
                _actors[i].Visible = !isLocal;
            }
            for (int i = 0; i < _chairs.Count; i++)
            {
                if (IsInstanceValid(_chairs[i]))
                    _chairs[i].Visible = (i != seat);
            }
            for (int i = 0; i < _hands.Count; i++)
            {
                if (IsInstanceValid(_hands[i]))
                    _hands[i].Visible = (i != seat);
            }
        }
        else
        {
            // Overhead Cinematic View - all seated characters and furniture are visible
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
            }
            for (int i = 0; i < _chairs.Count; i++)
            {
                if (IsInstanceValid(_chairs[i]))
                    _chairs[i].Visible = true;
            }
            for (int i = 0; i < _hands.Count; i++)
            {
                if (IsInstanceValid(_hands[i]))
                    _hands[i].Visible = (i != LocalSeatIndex);
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
            Mesh = new QuadMesh { Size = new Vector2(0.64f, 0.88f) },
            RotationDegrees = new Vector3(-90, 0, 0),
            Position = new Vector3(0, -0.012f, 0),
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0, 0, 0, 0.40f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                CullMode = BaseMaterial3D.CullModeEnum.Disabled
            }
        };
        card.AddChild(shadow);

        // Outer card body with realistic thickness matching deck proportions (0.58m x 0.82m)
        var bodyMesh = new BoxMesh
        {
            Size = new Vector3(0.58f, .018f, 0.82f),
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
                Mesh = new BoxMesh { Size = new Vector3(0.62f, .014f, 0.86f) },
                Position = new Vector3(0, -.002f, 0),
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
            Mesh = new PlaneMesh { Size = new Vector2(0.56f, 0.80f) },
            Position = new Vector3(0, .010f, 0),
            MaterialOverride = face
        });

        // Floating nameplate label indicating who played this card and if it is manilha
        if (!string.IsNullOrEmpty(ownerName))
        {
            var label = new Label3D
            {
                Text = isSpecial ? $"★ {ownerName} ★\nMANILHA" : ownerName,
                FontSize = 22,
                OutlineSize = 5,
                OutlineRenderPriority = 1,
                Modulate = isSpecial ? ClubTheme.Gold : ClubTheme.Paper,
                OutlineModulate = ClubTheme.Ink,
                Position = new Vector3(0, 0.06f, -0.52f),
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
                PixelSize = 0.0028f
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
            float x = (cardInTrick - 0.5f) * 0.75f;
            target = new Vector3(x, .088f + n * .008f, -0.20f);
        }
        else if (SeatCount <= 4)
        {
            float x = (cardInTrick - 1.5f) * 0.72f;
            target = new Vector3(x, .088f + n * .008f, -0.20f);
        }
        else
        {
            float rowZ = cardInTrick < 3 ? -0.32f : 0.08f;
            float rowX = ((cardInTrick % 3) - 1.0f) * 0.72f;
            target = new Vector3(rowX, .088f + n * .008f, rowZ);
        }

        Vector3 start = GetHandSocketPosition(seat);
        if (seat == LocalSeatIndex && CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov && _camera != null)
        {
            // In first-person POV, spawn the played card cleanly in front of and below the eye camera view
            start = _camera.GlobalPosition + (-_camera.GlobalTransform.Basis.Y) * 0.28f + (-_camera.GlobalTransform.Basis.Z) * 0.48f;
        }
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
        float peakArc = isSpecial ? 0.70f : 0.48f;
        float duration = isSpecial ? SpecialCardFlightDuration : StandardCardFlightDuration;
        var shadowMat = shadow.MaterialOverride as StandardMaterial3D;
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (IsInstanceValid(card))
            {
                float arc = Mathf.Sin(t * Mathf.Pi) * peakArc;
                card.Position = start.Lerp(target, t) + Vector3.Up * arc;
                if (IsInstanceValid(shadow) && shadowMat != null)
                {
                    shadow.Position = new Vector3(0, -arc - 0.004f, 0);
                    float alpha = Mathf.Lerp(0.42f, 0.12f, arc / peakArc);
                    shadowMat.AlbedoColor = new Color(0, 0, 0, alpha);
                    float sc = 1.0f + (arc / peakArc) * 0.22f;
                    shadow.Scale = new Vector3(sc, sc, 1.0f);
                }
            }
        }), 0f, 1f, duration).SetTrans(isSpecial ? Tween.TransitionType.Back : Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(card, "rotation", targetRotation, duration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        tween.Chain().TweenCallback(Callable.From(() =>
        {
            AudioManager.Instance?.PlaySound(isSpecial ? "score" : "play");
            if (isSpecial) ShowReactionBubble(seat, "truco", 1.4f);
            if (IsInstanceValid(shadow) && shadowMat != null)
            {
                shadow.Position = new Vector3(0, -0.005f, 0);
                shadowMat.AlbedoColor = new Color(0, 0, 0, 0.42f);
                shadow.Scale = Vector3.One;
            }
            if (seat == LocalSeatIndex && CurrentCameraMode == CameraPerspectiveMode.FirstPersonPov && SettingsManager.Instance?.ReduceMotion != true)
            {
                var recoil = CreateTween();
                _motions.Add(recoil);
                float recoilAmount = isSpecial ? -0.035f : -0.016f;
                recoil.TweenMethod(Callable.From<float>(v => _tactileRecoilY = v), 0f, recoilAmount, 0.04f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
                recoil.TweenMethod(Callable.From<float>(v => _tactileRecoilY = v), recoilAmount, 0f, 0.18f)
                    .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
            }
        }));
        // Subtle micro settling bounce when hitting the table felt
        tween.Chain().TweenProperty(card, "position:y", target.Y + 0.012f, 0.04f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(card, "position:y", target.Y, 0.08f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
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

        // 1. Destruir IMEDIATAMENTE todos os rótulos 3D de nome e manilha para não flutuarem sobre o baralho
        foreach (var card in roundCards)
        {
            if (!IsInstanceValid(card)) continue;
            foreach (var child in card.GetChildren())
            {
                if (child is Label3D lbl)
                {
                    lbl.Visible = false;
                    lbl.QueueFree();
                }
            }
        }

        Vector3 deckPos = new Vector3(-1.85f, 0.082f, 0.2f);
        if (SettingsManager.Instance?.ReduceMotion == true)
        {
            for (int i = 0; i < roundCards.Count; i++)
            {
                var card = roundCards[i];
                if (!IsInstanceValid(card)) continue;
                card.Position = deckPos + new Vector3(0, i * 0.018f, 0);
                card.Rotation = new Vector3(Mathf.Pi, 0, 0);
                _discards.Add(card);
            }
            _collecting.Clear();
            return;
        }

        // 2. Juntar as cartas jogadas no centro da mesa, viradas para baixo em montinho
        var gatherTween = CreateTween();
        _motions.Add(gatherTween);
        gatherTween.SetParallel(true);
        Vector3 tableCenter = new Vector3(0, 0.12f, 0.0f);

        for (int i = 0; i < roundCards.Count; i++)
        {
            var card = roundCards[i];
            if (!IsInstanceValid(card)) continue;
            Vector3 centerOffset = tableCenter + new Vector3(0, i * 0.014f, 0);
            gatherTween.TweenProperty(card, "position", centerOffset, 0.20f)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
            gatherTween.TweenProperty(card, "rotation", new Vector3(Mathf.Pi, (i % 5 - 2) * 0.03f, 0), 0.20f)
                .SetTrans(Tween.TransitionType.Quad);
        }

        await ToSignal(gatherTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        // 3. "meio que ergue o baralho, essa parte do baralho ali ergue"
        // Coletar todos os cartões que atualmente compõem o baralho
        var deckCards = new List<MeshInstance3D>();
        if (_deckPile != null)
        {
            foreach (Node3D child in _deckPile.GetChildren())
            {
                if (child is MeshInstance3D mi && mi.Mesh is BoxMesh) deckCards.Add(mi);
            }
        }

        float liftHeight = 0.16f + roundCards.Count * 0.018f;
        var liftTween = CreateTween();
        _motions.Add(liftTween);
        liftTween.SetParallel(true);
        foreach (var dc in deckCards)
        {
            liftTween.TweenProperty(dc, "position:y", dc.Position.Y + liftHeight, 0.18f)
                .SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
        }

        await ToSignal(liftTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        // 4. "e essas cartas que tão em cima elas vão embaixo"
        // As cartas recolhidas do centro deslizam diretamente para BAIXO do baralho erguido
        var underTween = CreateTween();
        _motions.Add(underTween);
        underTween.SetParallel(true);

        for (int i = 0; i < roundCards.Count; i++)
        {
            var card = roundCards[i];
            if (!IsInstanceValid(card)) continue;
            float jitterX = (i % 3 - 1) * 0.0035f;
            float jitterZ = (i % 4 - 1.5f) * 0.004f;
            float jitterRot = (i % 5 - 2) * 0.012f;

            Vector3 target = new Vector3(-1.85f + jitterX, 0.082f + i * 0.018f, 0.2f + jitterZ);
            Vector3 targetRot = new Vector3(Mathf.Pi, jitterRot, 0);

            float delay = i * 0.02f;
            underTween.TweenProperty(card, "position", target, 0.22f)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            underTween.TweenProperty(card, "rotation", targetRot, 0.22f)
                .SetDelay(delay);
        }

        await ToSignal(underTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        // 5. "aí fecha, faz um montinho certinho pra não ficar essa parte aí esquisita"
        // As cartas do baralho que estavam no ar descem suavemente para fechar por cima das novas cartas!
        var closeTween = CreateTween();
        _motions.Add(closeTween);
        closeTween.SetParallel(true);

        for (int i = 0; i < deckCards.Count; i++)
        {
            var dc = deckCards[i];
            float jitterX = (i % 3 - 1) * 0.0035f;
            float jitterZ = (i % 4 - 1.5f) * 0.004f;
            float finalY = 0.082f + (roundCards.Count + i) * 0.018f;
            closeTween.TweenProperty(dc, "position", new Vector3(-1.85f + jitterX, finalY, 0.2f + jitterZ), 0.22f)
                .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
        }

        await ToSignal(closeTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        AudioManager.Instance?.PlaySound("cut");

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

    public Vector3 GetHandSocketPosition(int seat)
    {
        if (seat >= 0 && seat < _actors.Count && IsInstanceValid(_actors[seat]))
        {
            var skeleton = _actors[seat].FindChild("Skeleton3D", true, false) as Skeleton3D;
            if (skeleton != null)
            {
                int boneIdx = skeleton.FindBone("CardSocket.R");
                if (boneIdx < 0) boneIdx = skeleton.FindBone("Hand.R");
                if (boneIdx >= 0)
                {
                    Transform3D globalPose = skeleton.GlobalTransform * skeleton.GetBoneGlobalPose(boneIdx);
                    return globalPose.Origin;
                }
            }
        }
        int seatIdx = Mathf.PosMod(seat, _hands.Count);
        return _hands[seatIdx].Position + Vector3.Up * .08f;
    }

    private static readonly Dictionary<string, string> GestureFallback = new(StringComparer.OrdinalIgnoreCase)
    {
        { "idle_table_01", "idle" },
        { "idle_table_02", "idle" },
        { "idle_impatient", "idle" },
        { "think", "idle" },
        { "inspect_hand", "idle" },
        { "hold_cards", "idle" },
        { "play_card_fast", "play_card" },
        { "play_card_dramatic", "play_card" },
        { "deal", "play_card" },
        { "shuffle", "play_card" },
        { "cut_deck", "play_card" },
        { "check", "play_card" },
        { "bet", "play_card" },
        { "all_in", "truco" },
        { "fold", "idle" },
        { "showdown", "flourish" },
        { "accept_truco", "truco" },
        { "decline_truco", "idle" },
        { "bid_confident", "victory" },
        { "bid_uncertain", "idle" },
        { "bid_zero", "idle" },
        { "trick_win", "victory" },
        { "trick_lose", "idle" },
        { "life_lost", "idle" },
        { "small_win", "victory" },
        { "big_win", "victory" },
        { "lose", "idle" },
        { "bad_beat", "idle" },
        { "suspicious", "idle" },
        { "surprised", "idle" },
        { "laugh", "idle" },
        { "taunt", "flourish" }
    };

    public void PlayGesture(int seat, string clip)
    {
        if (seat < 0 || seat >= _animators.Count || SettingsManager.Instance?.ReduceMotion == true) return;
        var player = _animators[seat];
        if (player == null) return;

        string targetClip = clip;
        bool hasClip = false;
        foreach (string animation in player.GetAnimationList())
        {
            if (animation.Equals(clip, StringComparison.OrdinalIgnoreCase) || animation.EndsWith("/" + clip, StringComparison.OrdinalIgnoreCase))
            {
                hasClip = true;
                targetClip = animation;
                break;
            }
        }

        if (!hasClip && GestureFallback.TryGetValue(clip, out var fallback))
        {
            foreach (string animation in player.GetAnimationList())
            {
                if (animation.Equals(fallback, StringComparison.OrdinalIgnoreCase) || animation.EndsWith("/" + fallback, StringComparison.OrdinalIgnoreCase))
                {
                    targetClip = animation;
                    hasClip = true;
                    break;
                }
            }
        }

        if (hasClip)
        {
            var anim = player.GetAnimation(targetClip);
            if (anim != null)
            {
                if (targetClip.Equals("idle", StringComparison.OrdinalIgnoreCase) || targetClip.EndsWith("/idle", StringComparison.OrdinalIgnoreCase))
                    anim.LoopMode = Animation.LoopModeEnum.Linear;
                else
                    anim.LoopMode = Animation.LoopModeEnum.None;
            }
            player.Play(targetClip, 0.22);
            foreach (string idle in player.GetAnimationList())
            {
                if (idle.Equals("idle", StringComparison.OrdinalIgnoreCase) || idle.EndsWith("/idle", StringComparison.OrdinalIgnoreCase))
                {
                    var idleAnim = player.GetAnimation(idle);
                    if (idleAnim != null) idleAnim.LoopMode = Animation.LoopModeEnum.Linear;
                    player.Queue(idle);
                    break;
                }
            }
        }
    }

    /// <summary>3D cinematic intro cutscene with dynamic camera angles, letterbox 2.35:1 bars, luxury gold chyrons and instant skip.</summary>
    public async Task PlayEntrance(bool boss = false)
    {
        if (SettingsManager.Instance?.ReduceMotion == true || _intro) return;
        _intro = true;
        _skip = false;

        // Cinematic UI overlay
        _cinema = new Control { Name = "TableEntrance", MouseFilter = MouseFilterEnum.Stop };
        _cinema.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_cinema);

        // Letterbox Anamorphic 2.35:1 Bars
        var topBar = new ColorRect { Color = new Color(0.02f, 0.02f, 0.03f, 0.96f), MouseFilter = MouseFilterEnum.Ignore };
        topBar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        topBar.CustomMinimumSize = new Vector2(0, 64);
        _cinema.AddChild(topBar);

        var bottomBar = new ColorRect { Color = new Color(0.02f, 0.02f, 0.03f, 0.96f), MouseFilter = MouseFilterEnum.Ignore };
        bottomBar.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        bottomBar.CustomMinimumSize = new Vector2(0, 64);
        _cinema.AddChild(bottomBar);

        // Skip Button top right
        var skipMargin = new MarginContainer();
        skipMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        skipMargin.AddThemeConstantOverride("margin_top", 14);
        skipMargin.AddThemeConstantOverride("margin_right", 18);
        _cinema.AddChild(skipMargin);
        var skip = ClubTheme.Button("Pular entrada");
        skip.Name = "SkipButton";
        skip.CustomMinimumSize = new Vector2(130, 36);
        skip.Pressed += () => _skip = true;
        skipMargin.AddChild(skip);

        // Centered Golden Chyron Container
        var chyronMargin = new MarginContainer();
        chyronMargin.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        chyronMargin.AddThemeConstantOverride("margin_bottom", 74);
        chyronMargin.AddThemeConstantOverride("margin_left", 180);
        chyronMargin.AddThemeConstantOverride("margin_right", 180);
        _cinema.AddChild(chyronMargin);

        var chyronPanel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
        chyronPanel.AddThemeStyleboxOverride("panel", ClubTheme.Box(new Color(0.03f, 0.07f, 0.055f, 0.92f), ClubTheme.Gold, 10));
        chyronMargin.AddChild(chyronPanel);

        var chyronBox = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        chyronBox.AddThemeConstantOverride("separation", 2);
        chyronPanel.AddChild(chyronBox);

        var eyebrow = ClubTheme.Label(boss ? "★ CONFRONTO DE ALTA CÚPULA ★" : "CLUBE PRIVADO  ·  SESSÃO DE ALTA APOSTA", 11, ClubTheme.Gold);
        eyebrow.HorizontalAlignment = HorizontalAlignment.Center;
        chyronBox.AddChild(eyebrow);

        _caption = ClubTheme.Label(boss ? "O DESAFIO DO CHEFÃO" : "A GRANDE MESA DO CLUBE", 22, ClubTheme.Paper);
        _caption.HorizontalAlignment = HorizontalAlignment.Center;
        _caption.AddThemeFontOverride("font", ClubTheme.DisplayFont);
        chyronBox.AddChild(_caption);

        var subtitle = ClubTheme.Label(boss ? "A casa apresenta seu mestre supremo" : "Os mestres tomam seus lugares à mesa", 13, ClubTheme.Muted);
        subtitle.HorizontalAlignment = HorizontalAlignment.Center;
        chyronBox.AddChild(subtitle);

        int rivalSeat = RivalIndex >= 0 && RivalIndex < _actors.Count ? RivalIndex : (_actors.Count > 1 ? 1 : 0);
        int rivalCast = _cast.Count > rivalSeat ? _cast[rivalSeat] : rivalSeat;
        int localSeat = Mathf.Clamp(LocalSeatIndex, 0, _positions.Count - 1);
        Vector3 seatPos = _positions[localSeat];
        Vector3 toCenter = -new Vector3(seatPos.X, 0, seatPos.Z).Normalized();
        var right = toCenter.Cross(Vector3.Up).Normalized();
        Vector3 eyePos = seatPos + Vector3.Up * 2.35f - toCenter * 0.70f + right * 0.35f;

        _camera.Projection = Camera3D.ProjectionType.Perspective;
        _camera.KeepAspect = Camera3D.KeepAspectEnum.Height;

        if (boss)
        {
            // ===== MODO PÔQUER / CHEFÃO =====
            // Trilha sonora de fundo e efeito de chegada dramática
            AudioManager.Instance?.PlayMusic("midnight-baron");
            AudioManager.Instance?.PlaySound("boss-arrival");

            for (int i = 0; i < _actors.Count; i++)
            {
                _actors[i].Position = _positions[i];
                _actors[i].Visible = true;
                _hands[i].Visible = false;
                PlayIdle(i);
            }

            Vector3 bPos = _positions[rivalSeat];
            Vector3 bFwd = -new Vector3(bPos.X, 0, bPos.Z).Normalized();

            // SHOT 1 (Pôquer): Plano médio imersivo da sala com o Chefão à mesa (1.6s)
            _camera.Fov = 42.0f;
            Vector3 bMidStart = new Vector3(0, 2.75f, 3.2f);
            Vector3 bMidEnd = new Vector3(0, 2.50f, 2.7f);
            _camera.Position = bMidStart;
            _camera.LookAt(bPos + Vector3.Up * 1.55f, Vector3.Up);

            var t1 = CreateTween();
            _motions.Add(t1);
            t1.TweenProperty(_camera, "position", bMidEnd, 1.6f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            if (!await WaitPresentation(1.55)) return;

            // SHOT 2 (Pôquer): CLOSE-UP DETALHADO NA CARA DO BOSS COM ANIMAÇÃO (2.2s)
            if (!_skip && IsInsideTree())
            {
                eyebrow.Text = "★ O CHEFÃO DA BANCA ★";
                _caption.Text = CharacterCatalog.Names[rivalCast].ToUpper();
                subtitle.Text = CharacterCatalog.Descriptions[rivalCast];

                // Câmera posicionada bem de perto na altura do rosto do boss (Y = 1.62m, nunca abaixo da mesa)
                Vector3 faceCam = bPos + bFwd * 1.12f + Vector3.Up * 2.30f;
                _camera.Position = faceCam;
                _camera.LookAt(bPos + Vector3.Up * 2.36f, Vector3.Up);
                _camera.Fov = 28.0f; // Lente retrato telephoto fechada com foco na face e bokeh

                // O boss executa uma animação de apresentação/desafio
                PlayGesture(rivalSeat, "boss_intro");

                var t2 = CreateTween();
                _motions.Add(t2);
                t2.TweenProperty(_camera, "position", faceCam + bFwd * -0.10f + Vector3.Up * 0.04f, 2.2f).SetTrans(Tween.TransitionType.Linear);
                if (!await WaitPresentation(2.15)) return;
            }

            // SHOT 3 (Pôquer): Transição arqueada e suave por cima da mesa para o POV do jogador (1.4s)
            if (!_skip && IsInsideTree())
            {
                eyebrow.Text = "SUA VEZ NA MESA";
                _caption.Text = "PREPARE SUAS APOSTAS";
                subtitle.Text = "Vença a meta de fichas para avançar.";

                Vector3 arcStart = new Vector3(seatPos.X * 0.5f, 2.45f, seatPos.Z * 0.5f + 0.6f);
                _camera.Position = arcStart;
                _camera.LookAt(new Vector3(0, 0.72f, -0.35f), Vector3.Up);
                _camera.Fov = 48.0f;

                var t3 = CreateTween();
                _motions.Add(t3);
                t3.TweenProperty(_camera, "position", eyePos, 1.35f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
                if (!await WaitPresentation(1.30)) return;
            }
        }
        else
        {
            // ===== MODO TRUCO / FODINHA =====
            AudioManager.Instance?.PlaySound("arrival");

            // Personagens sentando na cadeira: disparamos o gesto 'entrance' para todos
            for (int i = 0; i < _actors.Count; i++)
            {
                _actors[i].Position = _positions[i];
                _actors[i].Visible = true;
                _hands[i].Visible = false;
                PlayGesture(i, "entrance");
            }

            // SHOT 1 (Truco/Fodinha): Ângulo de fora no canto superior da sala (2.2s)
            // Revela a atmosfera completa do clube (lareira, relógio, bar cart) com os personagens tomando assento
            _camera.Fov = 52.0f;
            Vector3 cornerStart = new Vector3(-4.9f, 3.45f, 4.6f);
            Vector3 cornerEnd = new Vector3(-4.1f, 3.10f, 3.9f);
            _camera.Position = cornerStart;
            _camera.LookAt(new Vector3(0, 0.75f, 0.0f), Vector3.Up);

            eyebrow.Text = "CLUBE PRIVADO  ·  SESSÃO DE ALTA APOSTA";
            _caption.Text = "A GRANDE MESA DO CLUBE";
            subtitle.Text = "Os mestres tomam seus lugares à mesa.";

            var t1 = CreateTween();
            _motions.Add(t1);
            t1.TweenProperty(_camera, "position", cornerEnd, 2.2f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
            if (!await WaitPresentation(2.10)) return;

            // SHOT 2 (Truco/Fodinha): Varredura aérea elegante da mesa (1.8s)
            if (!_skip && IsInsideTree())
            {
                eyebrow.Text = "MESA COMPLETA";
                _caption.Text = rivalCast < CharacterCatalog.Names.Length ? $"DISPUTA CONTRA {CharacterCatalog.Names[rivalCast].ToUpper()}" : "RODADA DE APOSTAS";
                subtitle.Text = "Feltro clássico, baralho selado e regras tradicionais.";

                // Posição orbital elevada ao redor da mesa (Y = 2.40m -> 2.25m, bem acima do feltro)
                Vector3 orbitStart = new Vector3(2.8f, 2.40f, 3.1f);
                Vector3 orbitEnd = new Vector3(1.4f, 2.25f, 2.8f);
                _camera.Position = orbitStart;
                _camera.LookAt(new Vector3(0, 0.72f, 0.0f), Vector3.Up);
                _camera.Fov = 46.0f;

                var t2 = CreateTween();
                _motions.Add(t2);
                t2.TweenProperty(_camera, "position", orbitEnd, 1.8f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
                if (!await WaitPresentation(1.75)) return;
            }

            // SHOT 3 (Truco/Fodinha): Descida suave para o assento POV do jogador (1.4s)
            if (!_skip && IsInsideTree())
            {
                eyebrow.Text = "SUA VEZ NA MESA";
                _caption.Text = "QUE OS JOGOS COMECEM";
                subtitle.Text = "Boa sorte no feltro.";

                Vector3 descentStart = seatPos + Vector3.Up * 2.45f + toCenter * 0.40f;
                _camera.Position = descentStart;
                _camera.LookAt(new Vector3(0, 0.72f, -0.35f), Vector3.Up);
                _camera.Fov = 48.0f;

                var t3 = CreateTween();
                _motions.Add(t3);
                t3.TweenProperty(_camera, "position", eyePos, 1.35f).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.InOut);
                if (!await WaitPresentation(1.30)) return;
            }
        }

        // ===== CLEANUP & SEAMLESS GAMEPLAY HANDOFF =====
        foreach (var tween in _motions) tween?.Kill();
        _motions.Clear();
        for (int i = 0; i < _actors.Count; i++)
        {
            _actors[i].Position = _positions[i];
            _hands[i].Visible = true;
            PlayIdle(i);
        }
        if (IsInstanceValid(_cinema)) _cinema.QueueFree();
        _cinema = null;
        _intro = false;
        UpdateCameraPosition();
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
            if (_fireplaceLight != null && IsInstanceValid(_fireplaceLight))
            {
                float fireFlicker = Mathf.Sin(_time * 7.5f) * 0.18f + Mathf.Sin(_time * 16.3f) * 0.10f;
                _fireplaceLight.LightEnergy = 2.2f + fireFlicker;
            }
        }
        if (_intro) return;
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
