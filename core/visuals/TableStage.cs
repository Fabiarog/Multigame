using Godot;
using System.Collections.Generic;
using GameHub.Core.Systems;

namespace GameHub.Core.Visuals;

/// <summary>A real 3D felt table with billboard pixel characters and physical card fans.</summary>
public partial class TableStage : SubViewportContainer
{
    private SubViewport _viewport;
    private Node3D _world;
    private Camera3D _camera;
    private readonly List<Sprite3D> _actors = new();
    private readonly List<Node3D> _hands = new();
    private readonly List<Vector3> _positions = new();
    private readonly List<int> _cast = new();
    private readonly List<Tween> _reactions = new();
    private float _time;
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
        _viewport = new SubViewport { Name = "TableViewport", TransparentBg = true, OwnWorld3D = true,
            GuiDisableInput = true, HandleInputLocally = false, Size = new Vector2I(900, 400),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        AddChild(_viewport);
        _world = new Node3D();
        _viewport.AddChild(_world);
        var env = new Godot.Environment { BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = ClubTheme.Ink, AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color("#c7bd9a"), AmbientLightEnergy = .65f };
        _world.AddChild(new WorldEnvironment { Environment = env });
        AddCylinder("WalnutRim", 4.7f, .26f, new Color("#51331f"), new Vector3(0, -.15f, 0));
        AddCylinder("BrassInlay", 4.57f, .055f, ClubTheme.Gold.Darkened(.28f), Vector3.Zero);
        AddCylinder("GreenFelt", 4.45f, .065f, new Color("#194b37"), new Vector3(0, .04f, 0));
        _world.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-48, -24, 0), LightColor = new Color("#ffdaa1"), LightEnergy = 1.15f });
        _world.AddChild(new OmniLight3D { Position = new Vector3(-3, 4, 2), LightColor = new Color("#ffd393"), LightEnergy = 1.1f, OmniRange = 12 });
        _camera = new Camera3D { Position = new Vector3(0, 5.2f, 10.5f), Projection = Camera3D.ProjectionType.Orthogonal,
            KeepAspect = Camera3D.KeepAspectEnum.Width, Size = 12, Current = true };
        _world.AddChild(_camera);
        _camera.LookAt(new Vector3(0, .65f, -.4f), Vector3.Up);
        SetCast(RivalIndex, SeatCount);
        // A physical stack remains on the table between deals.
        for (int i = 0; i < 7; i++)
        {
            var mesh = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(.55f, .018f, .78f) },
                Position = new Vector3(-1.85f + i * .005f, .1f + i * .025f, .2f),
                MaterialOverride = CreateMaterial(i == 6 ? ClubTheme.Panel : ClubTheme.Paper) };
            _world.AddChild(mesh);
        }
    }

    private void AddCylinder(string name, float radius, float height, Color color, Vector3 position)
    {
        _world.AddChild(new MeshInstance3D { Name = name, Position = position, Scale = new Vector3(1, 1, .6f),
            Mesh = new CylinderMesh { TopRadius = radius, BottomRadius = radius, Height = height, RadialSegments = 64 }, MaterialOverride = CreateMaterial(color) });
    }

    private static StandardMaterial3D CreateMaterial(Color color) => new() { AlbedoColor = color, Roughness = .8f };

    public void SetCast(int rival, int seats)
    {
        RivalIndex = rival;
        SeatCount = Mathf.Clamp(seats, 2, 6);
        if (_world == null) return;
        foreach (var actor in _actors) actor.QueueFree();
        foreach (var hand in _hands) hand.QueueFree();
        foreach (var tween in _reactions) tween?.Kill();
        _actors.Clear(); _positions.Clear(); _hands.Clear(); _cast.Clear(); _reactions.Clear();
        var positions = new[] { new Vector3(-3.6f, 1.2f, 1.05f), new Vector3(3.25f, 1.2f, -2.2f),
            new Vector3(3.8f, 1.2f, .8f), new Vector3(-3.3f, 1.2f, -1.7f), new Vector3(-1.15f, 1.2f, -2.8f), new Vector3(3.5f, 1.2f, -1.5f) };
        if (SeatCount == 6)
            positions = new[] { new Vector3(-3.9f, 1.15f, 1.1f), new Vector3(-3.65f, 1.2f, -1.8f),
                new Vector3(-1.25f, 1.2f, -2.8f), new Vector3(1.25f, 1.2f, -2.8f),
                new Vector3(3.65f, 1.2f, -1.8f), new Vector3(3.9f, 1.15f, 1.1f) };
        for (int seat = 0; seat < SeatCount; seat++)
        {
            int character = seat == 0 ? CharacterCatalog.Find(SettingsManager.Instance?.CharacterId ?? "nina") : Mathf.PosMod(rival + seat - 1, 4);
            var actor = new Sprite3D { Texture = CharacterCatalog.Portrait(character), PixelSize = .0053f,
                Billboard = BaseMaterial3D.BillboardModeEnum.Enabled, Position = positions[seat],
                AlphaCut = SpriteBase3D.AlphaCutMode.Discard, TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
                Shaded = false, DoubleSided = true };
            _world.AddChild(actor);
            _actors.Add(actor); _positions.Add(positions[seat]); _cast.Add(character); _reactions.Add(null);
            var hand = new Node3D { Position = positions[seat] + new Vector3(0, -.6f, .45f) };
            _world.AddChild(hand); _hands.Add(hand);
            SetCardCount(seat, 3);
        }
    }

    public void SetCardCount(int seat, int count)
    {
        if (seat < 0 || seat >= _hands.Count) return;
        foreach (var child in _hands[seat].GetChildren()) { _hands[seat].RemoveChild(child); child.QueueFree(); }
        for (int i = 0; i < count; i++)
        {
            var card = new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(.35f, .48f, .025f) },
                Position = new Vector3((i - (count - 1) / 2f) * .19f, 0, i * .028f),
                RotationDegrees = new Vector3(-20, 0, (i - (count - 1) / 2f) * -12), MaterialOverride = CreateMaterial(ClubTheme.Paper) };
            _hands[seat].AddChild(card);
            var back = new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(.3f, .42f) },
                Position = new Vector3(0, 0, .015f), MaterialOverride = CreateMaterial(ClubTheme.Panel) };
            card.AddChild(back);
            back.AddChild(new MeshInstance3D { Mesh = new QuadMesh { Size = new Vector2(.075f, .075f) },
                Position = new Vector3(0, 0, .004f), RotationDegrees = new Vector3(0, 0, 45), MaterialOverride = CreateMaterial(ClubTheme.Gold) });
        }
    }

    public Vector2 SeatScreenPosition(int seat)
    {
        if (_camera == null || _positions.Count == 0) return GetGlobalRect().GetCenter();
        var projected = _camera.UnprojectPosition(_positions[Mathf.PosMod(seat, _positions.Count)] + new Vector3(0, -.5f, .5f));
        return GlobalPosition + projected * Size / (Vector2)_viewport.Size;
    }

    public Vector2 DeckScreenPosition()
    {
        if (_camera == null || Size.X < 100 || Size.Y < 100)
            return GetViewportRect().Size * new Vector2(.52f, .42f);
        return GlobalPosition + _camera.UnprojectPosition(new Vector3(-1.85f, .27f, .2f)) * Size / (Vector2)_viewport.Size;
    }

    public void React(int seat)
    {
        if (seat < 0 || seat >= _actors.Count) return;
        var actor = _actors[seat];
        _reactions[seat]?.Kill();
        actor.Texture = CharacterCatalog.Portrait(_cast[seat], true);
        var tween = CreateTween();
        _reactions[seat] = tween;
        if (SettingsManager.Instance?.ReduceMotion != true)
        {
            tween.TweenProperty(actor, "scale", new Vector3(1.05f, 1.05f, 1.05f), .15f);
            tween.TweenProperty(actor, "scale", Vector3.One, .2f);
        }
        tween.TweenInterval(.55f);
        tween.TweenCallback(Callable.From(() => { if (IsInstanceValid(actor)) actor.Texture = CharacterCatalog.Portrait(_cast[seat]); }));
    }

    public override void _Process(double delta)
    {
        if (_camera == null) return;
        _camera.Size = Mathf.Max(11, Size.X / Mathf.Max(1, Size.Y) * 4.8f);
        if (SettingsManager.Instance?.ReduceMotion == true) return;
        _time += (float)delta;
        _camera.Position = new Vector3(Mathf.Sin(_time * .2f) * .1f, 5.2f, 10.5f);
        _camera.LookAt(new Vector3(0, .65f, -.4f), Vector3.Up);
        for (int i = 0; i < _actors.Count; i++)
            _actors[i].Position = _positions[i] + new Vector3(0, Mathf.Sin(_time * 1.3f + i) * .025f, 0);
    }

    public override void _ExitTree()
    {
        foreach (var tween in _reactions) tween?.Kill();
        _reactions.Clear(); _actors.Clear(); _hands.Clear();
    }
}
