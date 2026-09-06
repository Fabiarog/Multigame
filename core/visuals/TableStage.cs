using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameHub.Core.Systems;
using GameHub.Core.Graphics;

namespace GameHub.Core.Visuals;

/// <summary>Orthographic 2.5D table: Blender actors, physical card piles and lightweight stage light.</summary>
public partial class TableStage : SubViewportContainer
{
    private SubViewport _viewport;
    private Node3D _world;
    private Camera3D _camera;
    private Godot.Environment _environment;
    private DirectionalLight3D _key;
    private OmniLight3D _pendant;
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
    public int PlayedCardCount => _played.Count + _discards.Count + _collecting.Count;
    public int SeatCount { get; set; } = 2;
    public int RivalIndex { get; set; } = 2;
    public TableStage()
    {
        Stretch = true;
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

        Cylinder("WalnutRim", 4.7f, .26f, new Color("#2e1710"), new Vector3(0, -.15f, 0), .62f);
        Cylinder("BrassInlay", 4.57f, .05f, new Color("#dfaf42"), Vector3.Zero, .62f);
        Cylinder("GreenFelt", 4.45f, .065f, new Color("#16543d"), new Vector3(0, .04f, 0), .62f);

        // Discard felt tray beside the deck
        _world.AddChild(new MeshInstance3D {
            Mesh = new BoxMesh { Size = new Vector3(1.35f, 0.012f, 1.85f) },
            Position = new Vector3(-1.85f, 0.075f, -0.65f),
            MaterialOverride = StageMaterial(new Color("#123828"))
        });

        // Felt rail and chips supply depth even with shadows and post processing off.
        Color[] chipColors = { new Color("#c62828"), new Color("#d4af37"), new Color("#1565c0"), new Color("#2e7d32"), new Color("#d4af37") };
        for (int i = 0; i < 5; i++)
            Cylinder("Chip", .16f, .038f, chipColors[i], new Vector3(2.25f, .11f + i * .04f, .8f), 1);

        _key = new DirectionalLight3D {
            RotationDegrees = new Vector3(-50, -30, 0),
            LightColor = new Color("#fff2d6"),
            LightEnergy = 1.35f,
            ShadowEnabled = true
        };
        _key.DirectionalShadowMaxDistance = 24;
        _world.AddChild(_key);

        // Warm pendant chandelier directly above table center
        _pendant = new OmniLight3D {
            Position = new Vector3(0, 3.2f, 0.1f),
            LightColor = new Color("#ffeed0"),
            LightEnergy = 1.6f,
            OmniRange = 9.0f,
            OmniAttenuation = 1.15f,
            ShadowEnabled = true
        };
        _world.AddChild(_pendant);

        // Ambient rim light for character silhouettes
        _world.AddChild(new OmniLight3D {
            Position = new Vector3(0, 2.6f, -3.5f),
            LightColor = new Color("#6ea8a4"),
            LightEnergy = 0.75f,
            OmniRange = 10,
            ShadowEnabled = false
        });

        _camera = new Camera3D { Position = new Vector3(0, 6.2f, 11), Projection = Camera3D.ProjectionType.Orthogonal,
            KeepAspect = Camera3D.KeepAspectEnum.Width, Size = 12, Current = true };
        _world.AddChild(_camera); _camera.LookAt(new Vector3(0, .7f, 0), Vector3.Up);
        SetCast(RivalIndex, SeatCount);
        for (int i = 0; i < 7; i++)
            _world.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(.58f, .018f, .82f) },
                Position = new Vector3(-1.85f + i * .005f, .1f + i * .025f, .2f), MaterialOverride = StageMaterial(i == 6 ? ClubTheme.Panel : ClubTheme.Paper) });
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
        _viewport.ScreenSpaceAA = detailed ? Viewport.ScreenSpaceAAEnum.Fxaa : Viewport.ScreenSpaceAAEnum.Disabled;
        _key.ShadowEnabled = detailed;
        _pendant.ShadowEnabled = detailed && settings.RtaoQuality >= RayTracingSettings.RtQualityLevel.High;
        _key.DirectionalShadowMaxDistance = 24;
        _environment.SsaoEnabled = detailed && settings.RtaoEnabled;
        _environment.SsaoRadius = .85f; _environment.SsaoIntensity = 1.4f;
        _environment.SsrEnabled = detailed && settings.RtReflectionsEnabled;
        _viewport.TransparentBg = false;
        _environment.SsrMaxSteps = settings?.RtReflectionsQuality == RayTracingSettings.RtQualityLevel.Ultra ? 56 : 36;
        _environment.SdfgiEnabled = false;
        _environment.AmbientLightEnergy = enhanced && settings?.RayTracingEnabled == true && settings.RtgiEnabled ? .55f : .42f;
        _environment.GlowEnabled = enhanced;
        _environment.GlowIntensity = .38f;
    }

    private static StandardMaterial3D StageMaterial(Color color) => new() { AlbedoColor = color, Roughness = .75f };
    private void Cylinder(string name, float radius, float height, Color color, Vector3 position, float depth)
    {
        var material = StageMaterial(color);
        if (name == "GreenFelt")
        {
            material.AlbedoColor = Colors.White;
            material.AlbedoTexture = GD.Load<Texture2D>("res://assets/models/cards/felt.svg");
            material.Roughness = 0.85f;
        }
        if (name == "BrassInlay")
        {
            material.Metallic = .75f;
            material.Roughness = .25f;
        }
        if (name == "WalnutRim")
        {
            material.Roughness = .45f;
        }
        if (name == "Chip")
        {
            material.Roughness = .35f;
            material.Metallic = .15f;
        }
        _world.AddChild(new MeshInstance3D { Name = name, Position = position, Scale = new Vector3(1, 1, depth),
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 48 }, MaterialOverride = material });
    }

    public void SetCast(int rival, int seats)
    {
        RivalIndex = rival; SeatCount = Mathf.Clamp(seats, 2, 6);
        if (_world == null) return;
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

            // Hand fan held right at the table edge in front of the character, oriented coaxially with chair and character
            var hand = new Node3D { Position = pos + toCenter * 0.72f + new Vector3(0, 0.95f, 0), Rotation = new Vector3(0, rotY, 0) };
            _world.AddChild(hand); _hands.Add(hand); SetCardCount(seat, 3);
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

        // Outer card body with crisp border definition
        var body = new MeshInstance3D
        {
            Mesh = new BoxMesh { Size = new Vector3(1.25f, .024f, 1.76f) },
            MaterialOverride = StageMaterial(ClubTheme.Paper)
        };
        card.AddChild(body);

        // Gold rim if card is a special manilha
        if (isSpecial)
        {
            var goldRim = new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = new Vector3(1.32f, .018f, 1.83f) },
                Position = new Vector3(0, -.004f, 0),
                MaterialOverride = StageMaterial(ClubTheme.Gold)
            };
            card.AddChild(goldRim);
        }

        // Card face with high-definition anisotropic texture filtering and calibrated brightness
        var face = new StandardMaterial3D
        {
            AlbedoTexture = GD.Load<Texture2D>($"res://assets/models/cards/{rank}-{suit}.png"),
            Roughness = 0.85f,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.LinearWithMipmapsAnisotropic,
            TextureRepeat = false,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.PerPixel,
            EmissionEnabled = false
        };
        card.AddChild(new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(1.22f, 1.73f) },
            Position = new Vector3(0, .013f, 0),
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

        Vector3 start = _positions[Mathf.PosMod(seat, _positions.Count)] + new Vector3(0, 1.2f, .45f);
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

        var tween = CreateTween();
        _motions.Add(tween);
        tween.TweenInterval(delay);
        tween.TweenMethod(Callable.From<float>(t =>
        {
            if (IsInstanceValid(card))
                card.Position = start.Lerp(target, t) + Vector3.Up * Mathf.Sin(t * Mathf.Pi) * .8f;
        }), 0f, 1f, .45f);
        tween.Parallel().TweenProperty(card, "rotation", targetRotation, .45f);
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
            Vector3 discardBase = new Vector3(-1.85f, 0.11f, -0.65f);
            for (int i = 0; i < roundCards.Count; i++)
            {
                var card = roundCards[i];
                if (!IsInstanceValid(card)) continue;
                card.Position = discardBase + new Vector3(0, _discards.Count * 0.014f, 0);
                card.Rotation = new Vector3(Mathf.Pi, (i % 3 - 1) * 0.08f, 0);
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
        }

        await ToSignal(gatherTween, Tween.SignalName.Finished);
        if (!IsInsideTree()) return;

        // 2. Tombar ao contrário e deslizar para o montinho de descarte
        var discardTween = CreateTween();
        _motions.Add(discardTween);
        discardTween.SetParallel(true);
        Vector3 discardPos = new Vector3(-1.85f, 0.10f, -0.65f);

        for (int i = 0; i < roundCards.Count; i++)
        {
            var card = roundCards[i];
            if (!IsInstanceValid(card)) continue;
            int pileIdx = _discards.Count + i;
            Vector3 target = discardPos + new Vector3(
                (pileIdx % 3 - 1.0f) * 0.025f,
                pileIdx * 0.014f,
                (pileIdx % 4 - 1.5f) * 0.02f
            );
            Vector3 targetRot = new Vector3(Mathf.Pi, (pileIdx % 5 - 2) * 0.07f, 0);

            float delay = i * 0.025f;
            discardTween.TweenProperty(card, "position", target, 0.32f)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.InOut);
            discardTween.TweenProperty(card, "rotation", targetRot, 0.32f)
                .SetDelay(delay)
                .SetTrans(Tween.TransitionType.Quad);
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
    public void PlayGesture(int seat,string clip)
    {
        if(seat<0||seat>=_animators.Count||SettingsManager.Instance?.ReduceMotion==true)return;
        var player=_animators[seat];if(player==null)return;
        foreach(string animation in player.GetAnimationList())
            if(animation.Equals(clip,StringComparison.OrdinalIgnoreCase)||animation.EndsWith("/"+clip,StringComparison.OrdinalIgnoreCase)){player.Play(animation,.12);return;}
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
        for(int i=0;i<_actors.Count;i++){_actors[i].Position=_positions[i];_hands[i].Visible=true;}
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
        if(_camera==null)return;
        bool motion = SettingsManager.Instance?.ReduceMotion != true;
        if (_clockAnimator != null && _clockClip != null && motion != _clockMoving)
        {
            if (motion) _clockAnimator.Play(_clockClip);
            else _clockAnimator.Pause();
            _clockMoving = motion;
        }
        _camera.Size=Mathf.Max(11.5f,Size.X/Mathf.Max(1,Size.Y)*6.2f);
        if(SettingsManager.Instance?.ReduceMotion==true||_intro)return;
        _time+=(float)delta;
        _camera.Position=new Vector3(Mathf.Sin(_time*.17f)*.07f,6.2f,11);
        _camera.LookAt(new Vector3(0,.7f,0),Vector3.Up);
    }

    public override void _ExitTree()
    {
        _skip=true;
        foreach(var tween in _motions)tween?.Kill();
        if(GraphicsQualityManager.Instance!=null)GraphicsQualityManager.Instance.GraphicsSettingsApplied-=ApplyLighting;
        if(VideoSettingsManager.Instance!=null)VideoSettingsManager.Instance.VideoSettingsApplied-=ApplyLighting;
        _motions.Clear(); _actors.Clear(); _chairs.Clear(); _animators.Clear(); _hands.Clear(); _played.Clear(); _discards.Clear(); _collecting.Clear();
        foreach (var light in _sconceLights) if (IsInstanceValid(light)) light.QueueFree();
        _sconceLights.Clear();
        if (_roomInstance != null && IsInstanceValid(_roomInstance)) _roomInstance.QueueFree();
    }
}
