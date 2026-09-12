using Godot;
using System;
using System.Linq;
using GameHub.Core.Systems;

namespace GameHub.Core.Visuals;

/// <summary>
/// Interactive 3D Trophy / Character Model Viewer (Batman: Arkham City style).
/// Displays characters on an illuminated circular pedestal with 360° mouse drag orbit,
/// smooth mouse-wheel zoom for inspecting anatomy, fabrics, and accessories,
/// and instant pose / animation clip triggers.
/// </summary>
public partial class CharacterViewer3D : Control
{
    private SubViewportContainer _viewportContainer;
    private SubViewport _viewport;
    private Node3D _world;
    private Camera3D _camera;
    private Node3D _pedestal;
    private Node3D _characterRoot;
    private Node3D _characterModel;
    private AnimationPlayer _animator;

    private int _characterIndex = 0;
    private int _outfitIndex = 0;

    // Orbit camera state
    private float _yaw = 0f;
    private float _pitch = 10f;
    private float _cameraDistance = 2.85f;
    private bool _isDragging = false;
    private Vector2 _lastMousePos;

    private Label _animLabel;
    private HBoxContainer _animControls;

    public int CharacterIndex => _characterIndex;
    public int OutfitIndex => _outfitIndex;

    public CharacterViewer3D()
    {
        CustomMinimumSize = new Vector2(360, 420);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        ClipContents = true;
    }

    public override void _Ready()
    {
        EnsureInitialized();
        LoadCharacter(_characterIndex);
    }

    private void EnsureInitialized()
    {
        if (_world != null) return;

        _viewportContainer = new SubViewportContainer
        {
            Stretch = true,
            MouseFilter = MouseFilterEnum.Pass
        };
        _viewportContainer.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(_viewportContainer);

        _viewport = new SubViewport
        {
            OwnWorld3D = true,
            TransparentBg = false,
            Msaa3D = Viewport.Msaa.Msaa4X,
            ScreenSpaceAA = Viewport.ScreenSpaceAAEnum.Disabled,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
            HandleInputLocally = false
        };
        _viewportContainer.AddChild(_viewport);

        _world = new Node3D { Name = "ViewerWorld" };
        _viewport.AddChild(_world);

        // Studio Environment & Lighting
        SetupLighting();

        // Trophy Pedestal
        SetupPedestal();

        // 3D Orbit Camera
        _camera = new Camera3D
        {
            Fov = 42f,
            Near = 0.1f,
            Far = 50f,
            Current = true
        };
        _world.AddChild(_camera);
        UpdateCameraTransform();

        // Character Root Node
        _characterRoot = new Node3D { Name = "CharacterRoot", Position = new Vector3(0, 0.12f, 0) };
        _world.AddChild(_characterRoot);

        // UI Overlay for Animations and Camera Hints
        BuildControlsOverlay();
    }

    private void SetupLighting()
    {
        // World Environment with dark studio background
        var env = new Godot.Environment
        {
            BackgroundMode = Godot.Environment.BGMode.Color,
            BackgroundColor = new Color(0.04f, 0.05f, 0.07f, 1.0f),
            AmbientLightSource = Godot.Environment.AmbientSource.Color,
            AmbientLightColor = new Color(0.22f, 0.24f, 0.28f),
            AmbientLightEnergy = 0.85f,
            TonemapMode = Godot.Environment.ToneMapper.Aces,
            GlowEnabled = true,
            GlowIntensity = 0.5f,
            GlowBloom = 0.15f
        };
        var envNode = new WorldEnvironment { Environment = env };
        _world.AddChild(envNode);

        // Warm Key Light (Front-Left)
        var keyLight = new DirectionalLight3D
        {
            Position = new Vector3(-2.5f, 3.2f, 3.0f),
            RotationDegrees = new Vector3(-42f, -38f, 0f),
            LightColor = new Color(1.0f, 0.94f, 0.85f),
            LightEnergy = 1.35f,
            ShadowEnabled = true
        };
        _world.AddChild(keyLight);

        // Cool Fill Light (Front-Right)
        var fillLight = new DirectionalLight3D
        {
            Position = new Vector3(3.0f, 2.0f, 2.5f),
            RotationDegrees = new Vector3(-28f, 48f, 0f),
            LightColor = new Color(0.72f, 0.85f, 1.0f),
            LightEnergy = 0.65f,
            ShadowEnabled = false
        };
        _world.AddChild(fillLight);

        // High-Contrast Rim Light (Back-Top)
        var rimLight = new DirectionalLight3D
        {
            Position = new Vector3(0.0f, 3.8f, -3.5f),
            RotationDegrees = new Vector3(-45f, 180f, 0f),
            LightColor = new Color(1.0f, 0.88f, 0.65f),
            LightEnergy = 1.45f,
            ShadowEnabled = false
        };
        _world.AddChild(rimLight);
    }

    private void SetupPedestal()
    {
        _pedestal = new Node3D { Name = "TrophyPedestal", Position = Vector3.Zero };
        _world.AddChild(_pedestal);

        // Base Plinth Cylinder
        var plinthMesh = new CylinderMesh { TopRadius = 0.95f, BottomRadius = 1.05f, Height = 0.14f, RadialSegments = 48 };
        var matPlinth = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.12f, 0.10f, 0.08f),
            Metallic = 0.65f,
            Roughness = 0.28f
        };
        var plinth = new MeshInstance3D { Mesh = plinthMesh, Position = new Vector3(0, 0.06f, 0), MaterialOverride = matPlinth };
        _pedestal.AddChild(plinth);

        // Glowing Trim Ring on Pedestal Edge
        var ringMesh = new TorusMesh { InnerRadius = 0.92f, OuterRadius = 0.97f, Rings = 48, RingSegments = 16 };
        var matRing = new StandardMaterial3D
        {
            AlbedoColor = ClubTheme.Gold,
            EmissionEnabled = true,
            Emission = ClubTheme.Gold,
            EmissionEnergyMultiplier = 2.4f,
            Metallic = 0.85f,
            Roughness = 0.2f
        };
        var ring = new MeshInstance3D { Mesh = ringMesh, Position = new Vector3(0, 0.13f, 0), MaterialOverride = matRing };
        _pedestal.AddChild(ring);
    }

    public void LoadCharacter(int characterIndex)
    {
        EnsureInitialized();
        _characterIndex = Mathf.Clamp(characterIndex, 0, CharacterCatalog.Ids.Length - 1);

        if (_characterModel != null && IsInstanceValid(_characterModel))
        {
            _characterRoot?.RemoveChild(_characterModel);
            _characterModel.QueueFree();
            _characterModel = null;
        }

        try
        {
            string modelPath = CharacterCatalog.ModelPath(_characterIndex);
            var scene = GD.Load<PackedScene>(modelPath);
            if (scene != null && _characterRoot != null)
            {
                _characterModel = scene.Instantiate<Node3D>();
                _characterRoot.AddChild(_characterModel);
                _characterModel.Scale = Vector3.One * 0.95f;

                _animator = null;
                foreach (var node in _characterModel.FindChildren("*", "AnimationPlayer", true, false))
                {
                    _animator = (AnimationPlayer)node;
                    break;
                }

                if (_animator != null)
                {
                    foreach (string a in _animator.GetAnimationList())
                    {
                        var animA = _animator.GetAnimation(a);
                        if (animA != null && (a.Equals("idle", StringComparison.OrdinalIgnoreCase) || a.EndsWith("/idle", StringComparison.OrdinalIgnoreCase)))
                        {
                            animA.LoopMode = Animation.LoopModeEnum.Linear;
                        }
                        foreach (string b in _animator.GetAnimationList())
                        {
                            if (a != b) _animator.SetBlendTime(a, b, 0.25f);
                        }
                    }
                    PlayAnimation("idle");
                }
            }
        }
        catch (Exception ex)
        {
            GD.PushWarning($"[CharacterViewer3D] Failed to load character model {_characterIndex}: {ex.Message}");
        }

        ApplyOutfitColors();
    }

    public void SetOutfit(int outfitIndex)
    {
        _outfitIndex = Mathf.Clamp(outfitIndex, 0, 2);
        ApplyOutfitColors();
    }

    private void ApplyOutfitColors()
    {
        if (_characterModel == null || !IsInstanceValid(_characterModel)) return;

        Color coatTint = _outfitIndex switch
        {
            1 => new Color(0.18f, 0.16f, 0.25f), // Alta Noite (Escuro e Veludo)
            2 => new Color(0.65f, 0.52f, 0.22f), // Clube Vintage Dourado
            _ => Colors.White                     // Traje Nobre Clássico
        };

        foreach (var node in _characterModel.FindChildren("*", "MeshInstance3D", true, false))
        {
            if (node is MeshInstance3D meshInstance)
            {
                for (int s = 0; s < meshInstance.GetSurfaceOverrideMaterialCount(); s++)
                {
                    if (meshInstance.GetSurfaceOverrideMaterial(s) is StandardMaterial3D sm)
                    {
                        if (sm.ResourceName.Contains("coat") || sm.ResourceName.Contains("vest") || sm.ResourceName.Contains("suit"))
                        {
                            sm.AlbedoColor = coatTint;
                        }
                    }
                }
            }
        }
    }

    public void PlayAnimation(string animName)
    {
        if (_animator == null || !IsInstanceValid(_animator)) return;

        if (_animator.HasAnimation(animName))
        {
            var anim = _animator.GetAnimation(animName);
            if (anim != null)
            {
                if (animName.Equals("idle", StringComparison.OrdinalIgnoreCase) || animName.EndsWith("/idle", StringComparison.OrdinalIgnoreCase))
                {
                    anim.LoopMode = Animation.LoopModeEnum.Linear;
                    _animator.Play(animName, 0.25f);
                }
                else
                {
                    anim.LoopMode = Animation.LoopModeEnum.None;
                    _animator.Play(animName, 0.20f);
                    foreach (string idle in _animator.GetAnimationList())
                    {
                        if (idle.Equals("idle", StringComparison.OrdinalIgnoreCase) || idle.EndsWith("/idle", StringComparison.OrdinalIgnoreCase))
                        {
                            var idleAnim = _animator.GetAnimation(idle);
                            if (idleAnim != null) idleAnim.LoopMode = Animation.LoopModeEnum.Linear;
                            _animator.Queue(idle);
                            break;
                        }
                    }
                }
            }
            if (_animLabel != null)
            {
                string displayName = animName switch
                {
                    "idle"       => "Repouso",
                    "truco"      => "Desafio & Blefe",
                    "victory"    => "Comemoração",
                    "play_card"  => "Jogada de Carta",
                    "flourish"   => "Floreio",
                    "boss_intro" => "Entrada Imponente",
                    _            => animName
                };
                _animLabel.Text = $"Pose: {displayName}";
            }
        }
    }

    private void UpdateCameraTransform()
    {
        if (_camera == null) return;

        float yawRad = Mathf.DegToRad(_yaw);
        float pitchRad = Mathf.DegToRad(_pitch);

        float hDist = _cameraDistance * Mathf.Cos(pitchRad);
        float y = 1.05f + _cameraDistance * Mathf.Sin(pitchRad);
        float x = hDist * Mathf.Sin(yawRad);
        float z = hDist * Mathf.Cos(yawRad);

        var eyePos = new Vector3(x, y, z);
        var targetPos = new Vector3(0, 1.05f, 0);

        if (_camera.IsInsideTree())
        {
            _camera.Position = eyePos;
            _camera.LookAt(targetPos, Vector3.Up);
        }
        else
        {
            var forward = (targetPos - eyePos).Normalized();
            if (forward.LengthSquared() > 0.001f)
            {
                var right = forward.Cross(Vector3.Up).Normalized();
                var up = right.Cross(forward).Normalized();
                _camera.Transform = new Transform3D(new Basis(right, up, -forward), eyePos);
            }
            else
            {
                _camera.Position = eyePos;
            }
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mb)
        {
            if (mb.ButtonIndex == MouseButton.Left || mb.ButtonIndex == MouseButton.Right)
            {
                _isDragging = mb.Pressed;
                _lastMousePos = mb.Position;
                AcceptEvent();
            }
            else if (mb.ButtonIndex == MouseButton.WheelUp && mb.Pressed)
            {
                _cameraDistance = Mathf.Clamp(_cameraDistance - 0.15f, 1.2f, 4.5f);
                UpdateCameraTransform();
                AcceptEvent();
            }
            else if (mb.ButtonIndex == MouseButton.WheelDown && mb.Pressed)
            {
                _cameraDistance = Mathf.Clamp(_cameraDistance + 0.15f, 1.2f, 4.5f);
                UpdateCameraTransform();
                AcceptEvent();
            }
        }
        else if (@event is InputEventMouseMotion mm && _isDragging)
        {
            Vector2 delta = mm.Position - _lastMousePos;
            _lastMousePos = mm.Position;

            _yaw += delta.X * 0.45f;
            _pitch = Mathf.Clamp(_pitch + delta.Y * 0.32f, -18f, 42f);
            UpdateCameraTransform();
            AcceptEvent();
        }
    }

    private void BuildControlsOverlay()
    {
        var overlay = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        overlay.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        overlay.AddThemeConstantOverride("margin_left", 12);
        overlay.AddThemeConstantOverride("margin_right", 12);
        overlay.AddThemeConstantOverride("margin_top", 10);
        overlay.AddThemeConstantOverride("margin_bottom", 10);
        AddChild(overlay);

        var topRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        overlay.AddChild(topRow);

        _animLabel = ClubTheme.Label("Pose: Repouso", 12, ClubTheme.Gold);
        topRow.AddChild(_animLabel);
        topRow.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

        var hintLabel = ClubTheme.Label("🖱 Arraste p/ girar 360° · Roda p/ zoom", 11, ClubTheme.Muted);
        topRow.AddChild(hintLabel);

        overlay.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore });

        // Animation triggers row at the bottom
        _animControls = new HBoxContainer { MouseFilter = MouseFilterEnum.Pass };
        _animControls.AddThemeConstantOverride("separation", 6);
        overlay.AddChild(_animControls);

        AddAnimButton("Repouso", "idle");
        AddAnimButton("Desafio", "truco");
        AddAnimButton("Vitória", "victory");
        AddAnimButton("Jogar", "play_card");
        AddAnimButton("Floreio", "flourish");

        var resetCamBtn = ClubTheme.Button("↺ Câmera");
        resetCamBtn.CustomMinimumSize = new Vector2(0, 28);
        resetCamBtn.Pressed += () => {
            _yaw = 0f;
            _pitch = 10f;
            _cameraDistance = 2.15f;
            UpdateCameraTransform();
        };
        _animControls.AddChild(resetCamBtn);
    }

    private void AddAnimButton(string title, string clip)
    {
        var btn = ClubTheme.Button(title);
        btn.CustomMinimumSize = new Vector2(0, 28);
        btn.Pressed += () => PlayAnimation(clip);
        _animControls.AddChild(btn);
    }
}
