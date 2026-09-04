using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Visuals;

/// <summary>
/// A composite 3D character made of multiple layered Sprite3Ds.
/// Each spritesheet is a 4x4 grid of poses:
///   Row 0: Front  (col0), Side-L (col1), Back   (col2), Side-R (col3)
///   Row 1: Walk front-L, Walk side-L, Walk back-L, Walk side-R-L
///   Row 2: Walk front-R, Walk side-R, Walk back-R, Walk side-R-R
///   Row 3: Action/Emote  (4 different action poses)
///
/// For a card game the character stands still facing front (frame 0),
/// and we only switch to action row frames for reactions.
/// </summary>
public partial class AvatarComposite : Node3D
{
    public enum AnimState
    {
        Idle,       // Standing front-facing (frame 0)
        React,      // Celebration/surprise (row 3 frames)
        Action,     // Playing card gesture  (row 3 frames, cycling)
        Truco       // Calling Truco / card-game emote (row 3 frames, cycling)
    }

    private AnimState _currentState = AnimState.Idle;

    // Layers
    public Sprite3D BaseLayer { get; private set; }
    public Sprite3D ShirtLayer { get; private set; }
    public Sprite3D PantsLayer { get; private set; }
    public Sprite3D HairLayer { get; private set; }

    private List<Sprite3D> _layers = new();

    private float _frameTimer = 0f;
    private int _currentFrame = 0;

    // Spritesheet grid config
    private const int H_FRAMES = 4;
    private const int V_FRAMES = 4;

    // How fast action/react animations cycle (seconds per frame)
    private const float ANIM_SPEED = 0.5f;

    // Which frame index to show when idle (front-facing, row 0 col 0)
    private const int IDLE_FRAME = 0;

    // Action row start index (row 3 = frames 12..15)
    private const int ACTION_ROW_START = 12;
    private const int ACTION_ROW_FRAMES = 4;

    public override void _Ready()
    {
        // Create layers in draw order (base on bottom, hair on top)
        BaseLayer  = CreateLayer("Base");
        PantsLayer = CreateLayer("Pants");
        ShirtLayer = CreateLayer("Shirt");
        HairLayer  = CreateLayer("Hair");

        // Small Z-offset to prevent z-fighting between layers
        PantsLayer.Position = new Vector3(0, 0, 0.001f);
        ShirtLayer.Position = new Vector3(0, 0, 0.002f);
        HairLayer.Position  = new Vector3(0, 0, 0.003f);

        LoadLayersFromSettings();

        // Start idle — show front-facing pose
        SetAllFrames(IDLE_FRAME);
    }

    private Sprite3D CreateLayer(string name)
    {
        var sprite = new Sprite3D();
        sprite.Name = name;
        // A 1024px sheet has 256px frames; 0.004 keeps the avatar proportional
        // to the table instead of filling the entire foreground.
        sprite.PixelSize = 0.004f;
        sprite.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        sprite.Transparent = true;
        sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Discard;
        sprite.Hframes = H_FRAMES;
        sprite.Vframes = V_FRAMES;
        sprite.Frame = IDLE_FRAME;

        AddChild(sprite);
        _layers.Add(sprite);

        return sprite;
    }

    public void LoadLayersFromSettings()
    {
        var settings = Systems.SettingsManager.Instance;
        if (settings == null) return;

        LoadLayers(settings.AvatarBase, settings.AvatarPants, settings.AvatarShirt, settings.AvatarHair);
    }

    public void LoadLayers(string baseId, string pantsId, string shirtId, string hairId)
    {
        SetLayerTexture(BaseLayer,  $"res://assets/sprites/characters/base/{baseId}.jpg");

        // The supplied shirt and pants sheets are legacy full-body composites,
        // not alpha-only cutouts. Showing both over the hair sheet hid the
        // clothes. Use the outfit composite until true cutout assets are added.
        SetLayerTexture(ShirtLayer, $"res://assets/sprites/characters/shirt/{shirtId}.jpg");
        PantsLayer.Texture = null;
        PantsLayer.Visible = false;
        HairLayer.Texture = null;
        HairLayer.Visible = false;
    }

    private void SetLayerTexture(Sprite3D layer, string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var tex = ResourceLoader.Load<Texture2D>(path);
            layer.Texture = tex;

            // Apply chroma key shader to remove green background
            var shader = ResourceLoader.Load<Shader>("res://assets/shaders/SpatialChromaKey.gdshader");
            if (shader != null)
            {
                var mat = new ShaderMaterial();
                mat.Shader = shader;
                mat.SetShaderParameter("chroma_color", new Color(0.0f, 1.0f, 0.0f));
                mat.SetShaderParameter("chroma_threshold", 0.4f);
                mat.SetShaderParameter("chroma_smoothing", 0.15f);
                mat.SetShaderParameter("sprite_texture", tex);
                layer.MaterialOverride = mat;
            }
        }
        else
        {
            layer.Texture = null;
            GD.Print($"[AvatarComposite] Texture not found: {path}");
        }
    }

    public void SetState(AnimState state)
    {
        if (_currentState == state) return;
        _currentState = state;
        _currentFrame = 0;
        _frameTimer = 0f;

        if (state == AnimState.Idle)
        {
            SetAllFrames(IDLE_FRAME);
        }
        else
        {
            SetAllFrames(ACTION_ROW_START);
        }
    }

    public override void _Process(double delta)
    {
        // Idle = static front pose, no animation needed
        if (_currentState == AnimState.Idle) return;

        _frameTimer += (float)delta;
        if (_frameTimer >= ANIM_SPEED)
        {
            _frameTimer = 0f;
            _currentFrame = (_currentFrame + 1) % ACTION_ROW_FRAMES;
            SetAllFrames(ACTION_ROW_START + _currentFrame);
        }
    }

    private void SetAllFrames(int frameIndex)
    {
        foreach (var layer in _layers)
        {
            if (layer.Texture != null)
            {
                layer.Frame = frameIndex;
            }
        }
    }
}
