using Godot;
namespace GameHub.Core.Visuals;

public partial class TableStage
{
    private ReflectionProbe _tableReflection;
    private Node3D _premiumTable;
    private Mesh _physicalCardMesh;
    private readonly System.Collections.Generic.List<SpotLight3D> _seatAccents = new();
    private void ApplyVisualTarget(bool forward, bool detailed)
    {
        bool classic = _currentRoomTheme == "classic_club";
        bool madrid = _currentRoomTheme == "madrid_salon";
        bool mexico = _currentRoomTheme == "mexico_recuerdos";
        _environment.GlowBloom = classic ? 0 : mexico ? .14f : madrid ? .12f : .16f;
        _environment.TonemapExposure = classic ? 1.05f : mexico ? 1.08f : madrid ? 1.08f : 1.10f;
        _environment.AmbientLightColor = new Color(classic ? "#80949e" : mexico ? "#6e4a38" : madrid ? "#7d5844" : "#92aba2");
        _key.LightEnergy = classic ? 1.1f : 1.35f;
        _key.LightAngularDistance = classic && detailed ? .65f : 0;
        _key.ShadowNormalBias = classic ? .35f : 1.8f;
        _key.ShadowBias = classic ? .035f : .08f;
        _basePendantEnergy = classic ? 1.55f : 1.6f;
        _pendant.LightEnergy = _basePendantEnergy;
        _pendant.OmniRange = classic ? 6.2f : 9f;
        _pendant.LightSize = classic && detailed ? .30f : 0;
        _fillLight.Position = classic ? new Vector3(-1.8f, 2.2f, .3f) : new Vector3(0,-1.8f,.2f);
        _fillLight.LightColor = new Color(classic ? "#afc5ce" : "#4a3525");
        _fillLight.LightEnergy = classic ? 1.1f : .28f;
        _fillLight.Visible = classic || forward;
        _rimLight.LightEnergy = classic ? .48f : _currentRoomTheme == "cyber_casino" ? .95f : mexico ? .80f : madrid ? .85f : .75f;
        if (_tableReflection != null) _tableReflection.Visible = classic;
        ApplyPremiumTable(classic);
        if (_seatAccents.Count != _positions.Count)
        {
            foreach (var light in _seatAccents) light.QueueFree();
            _seatAccents.Clear();
            foreach (var seat in _positions)
            {
                var inward = -new Vector3(seat.X, 0, seat.Z).Normalized();
                var light = new SpotLight3D { Name = "TableBounceAccent", Position = seat + inward * 1.6f + Vector3.Up * 2.5f,
                    LightColor = new Color("#e4d6be"), LightEnergy = 2.0f, SpotRange = 4,
                    SpotAngle = 48, SpotAttenuation = .8f, ShadowEnabled = false, LightCullMask = 4 };
                _world.AddChild(light);
                light.LookAt(seat + Vector3.Up * 1.35f);
                _seatAccents.Add(light);
            }
        }
        foreach (var light in _seatAccents) light.Visible = classic;
        if (!classic) return;
        _environment.AmbientLightEnergy = .48f;
        _environment.SsaoRadius = .32f;
        _environment.SsaoIntensity = .85f;
        _environment.SsaoPower = 1.1f;
        _environment.GlowBloom = 0;
        _environment.GlowIntensity = .18f;
        foreach (var light in _sconceLights)
        {
            light.LightEnergy = light == _fireplaceLight ? .85f : .55f;
            light.OmniRange = light == _fireplaceLight ? 4.5f : 4.8f;
        }
        if (_roomInstance != null)
            foreach (var child in _roomInstance.FindChildren("*", "MeshInstance3D", true, false))
            {
                var mesh = (MeshInstance3D)child;
                mesh.Layers |= 2; // Only static scenery is captured; actors/cards remain on layer 1.
                mesh.GIMode = GeometryInstance3D.GIModeEnum.Static;
            }
        if (_tableReflection == null)
        {
            _tableReflection = new ReflectionProbe {
                Name = "TableStaticReflection", Position = new Vector3(0,1.3f,-.6f),
                Size = new Vector3(12,5,10), Interior = true, BoxProjection = true,
                CullMask = 2, Intensity = .65f, AmbientMode = ReflectionProbe.AmbientModeEnum.Disabled,
                UpdateMode = ReflectionProbe.UpdateModeEnum.Once, EnableShadows = false
            };
            _world.AddChild(_tableReflection);
        }
    }

    private string ChairAssetPath => _currentRoomTheme == "classic_club" && ResourceLoader.Exists("res://assets/models/club/club_chair_premium.glb")
        ? "res://assets/models/club/club_chair_premium.glb" : "res://assets/models/club/club_chair.glb";

    private void RefreshChairTheme()
    {
        if (_chairs.Count == 0) return;
        var scene = GD.Load<PackedScene>(ChairAssetPath);
        for (int i = 0; i < _chairs.Count; i++)
        {
            var previous = _chairs[i];
            var replacement = scene.Instantiate<Node3D>();
            replacement.Transform = previous.Transform;
            replacement.Visible = previous.Visible;
            replacement.Name = previous.Name;
            _world.RemoveChild(previous);
            previous.QueueFree();
            _world.AddChild(replacement);
            _chairs[i] = replacement;
        }
    }

    private Mesh PhysicalCardMesh()
    {
        const string path = "res://assets/models/club/club_card_blank.glb";
        if (_currentRoomTheme != "classic_club" || !ResourceLoader.Exists(path))
            return new BoxMesh { Size = new Vector3(.58f,.018f,.82f) };
        if (_physicalCardMesh == null)
        {
            var source = GD.Load<PackedScene>(path).Instantiate<Node3D>();
            foreach (var node in source.FindChildren("*", "MeshInstance3D", true, false))
            { _physicalCardMesh = ((MeshInstance3D)node).Mesh; break; }
            source.Free();
        }
        return _physicalCardMesh;
    }

    private void ApplyPremiumTable(bool classic)
    {
        const string path = "res://assets/models/club/club_table_premium.glb";
        if (!ResourceLoader.Exists(path)) return;
        if (_premiumTable == null)
        {
            _premiumTable = GD.Load<PackedScene>(path).Instantiate<Node3D>();
            _premiumTable.Name = "PremiumTable";
            _world.AddChild(_premiumTable);
            foreach (var node in _premiumTable.FindChildren("*", "MeshInstance3D", true, false))
            {
                var mesh = (MeshInstance3D)node;
                mesh.Layers |= 2;
                if (mesh.Mesh.SurfaceGetMaterial(0)?.ResourceName.Contains("TargetFelt") == true)
                {
                    var material = new ShaderMaterial { Shader = GD.Load<Shader>("res://assets/shaders/FeltNormal.gdshader") };
                    material.SetShaderParameter("felt_color", new Color("#174d42"));
                    material.SetShaderParameter("roughness_base", .94f);
                    material.SetShaderParameter("roughness_wear", .03f);
                    material.SetShaderParameter("normal_strength", .08f);
                    mesh.MaterialOverride = material;
                }
            }
        }
        _premiumTable.Visible = classic;
        foreach (var node in _world.GetChildren())
            if (node is MeshInstance3D mesh && (mesh.Name.ToString() is "WalnutRim" or "WalnutBevel" or "LeatherInlay" or "BrassInlay" or "GreenFelt" || mesh.Name.ToString().StartsWith("Stud_")))
                mesh.Visible = !classic;
    }
}
