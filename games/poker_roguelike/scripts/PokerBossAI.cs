using Godot;
using System.Collections.Generic;

namespace GameHub.Games.PokerRoguelike;

/// <summary>
/// A visual AI "Boss" opponent for the Poker Roguelike.
/// It sits at the table, reacts to your hands, and provides the target score to beat.
/// </summary>
public partial class PokerBossAI : Core.AI.BotController
{
    private Sprite3D _sprite;
    private Tween _animationTween;

    // Different textures for different rounds/bosses
    private Dictionary<int, string> _bossTextures = new()
    {
        { 1, "res://assets/sprites/characters/turtle/turtle_spritesheet.jpg" },
        { 2, "res://assets/sprites/characters/spider/spider_spritesheet.jpg" },
        // Fallback to turtle if no specific sprite for later rounds
    };

    public override void _Ready()
    {
        _sprite = GetNode<Sprite3D>("Sprite3D");
        
        // Apply Chroma Key Shader
        var shader = ResourceLoader.Load<Shader>("res://assets/shaders/SpatialChromaKey.gdshader");
        if (shader != null)
        {
            var mat = new ShaderMaterial();
            mat.Shader = shader;
            // Bright green chroma key to match the spritesheet green backgrounds
            mat.SetShaderParameter("chroma_color", new Color(0.0f, 1.0f, 0.0f));
            mat.SetShaderParameter("chroma_threshold", 0.35f);
            mat.SetShaderParameter("chroma_smoothing", 0.1f);
            mat.SetShaderParameter("hframes", 2);
            mat.SetShaderParameter("vframes", 2);
            if (_sprite.Texture != null)
            {
                mat.SetShaderParameter("sprite_texture", _sprite.Texture);
            }
            _sprite.MaterialOverride = mat;
        }
    }

    private float _frameTimer = 0f;
    private int _currentFrame = 0;

    public override void _Process(double delta)
    {
        if (_sprite != null && _sprite.Texture != null)
        {
            _frameTimer += (float)delta;
            if (_frameTimer >= 0.2f) // 5 FPS idle animation
            {
                _frameTimer = 0f;
                _currentFrame = (_currentFrame + 1) % 4; // 2x2 grid = 4 frames
                _sprite.Frame = _currentFrame;

                if (_sprite.MaterialOverride is ShaderMaterial smat)
                {
                    smat.SetShaderParameter("frame", _currentFrame);
                }
            }
        }
    }

    public void SetupForRound(int round)
    {
        IsActive = true;
        
        string texPath = _bossTextures.ContainsKey(round) 
            ? _bossTextures[round] 
            : _bossTextures[1];

        if (ResourceLoader.Exists(texPath))
        {
            _sprite.Texture = ResourceLoader.Load<Texture2D>(texPath);
            if (_sprite.MaterialOverride is ShaderMaterial smat)
            {
                smat.SetShaderParameter("sprite_texture", _sprite.Texture);
            }
        }

        BotName = round switch
        {
            1 => "Tartaruga",
            2 => "Aranha",
            _ => $"Chefe {round}"
        };

        // Entrance animation
        _sprite.Position = new Vector3(0, 5, -2);
        AnimateJump(new Vector3(0, 1.5f, -2));
        GD.Print($"[PokerBoss] Round {round} Boss spawned: {BotName}");
    }

    public void ReactToPlayerHand(int score, int targetScore)
    {
        if (score >= targetScore)
        {
            // Player won the round, Boss is defeated
            AnimateDefeat();
        }
        else if (score > targetScore / 2)
        {
            // Good hand, Boss is nervous
            AnimateShake();
        }
        else
        {
            // Weak hand, Boss taunts
            AnimateJump(new Vector3(0, 1.5f, -2));
        }
    }

    private void AnimateJump(Vector3 targetPos)
    {
        if (_animationTween != null && _animationTween.IsValid())
            _animationTween.Kill();

        _animationTween = CreateTween();
        _animationTween.TweenProperty(_sprite, "position:y", targetPos.Y + 1.0f, 0.2f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        _animationTween.TweenProperty(_sprite, "position:y", targetPos.Y, 0.2f).SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
    }

    private void AnimateShake()
    {
        if (_animationTween != null && _animationTween.IsValid())
            _animationTween.Kill();

        _animationTween = CreateTween();
        Vector3 basePos = _sprite.Position;
        for (int i = 0; i < 4; i++)
        {
            _animationTween.TweenProperty(_sprite, "position:x", basePos.X + 0.2f, 0.05f);
            _animationTween.TweenProperty(_sprite, "position:x", basePos.X - 0.2f, 0.05f);
        }
        _animationTween.TweenProperty(_sprite, "position:x", basePos.X, 0.05f);
    }

    private void AnimateDefeat()
    {
        if (_animationTween != null && _animationTween.IsValid())
            _animationTween.Kill();

        _animationTween = CreateTween();
        _animationTween.TweenProperty(_sprite, "position:y", -5.0f, 1.0f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
        _animationTween.Parallel().TweenProperty(_sprite, "modulate:a", 0.0f, 1.0f);
    }

    // --- BotController overrides (unused for pure visual boss, but required by architecture) ---

    protected override void ExecuteDecision()
    {
        // For score attack, the boss doesn't actually play cards, so do nothing.
    }

    public override void UpdateGameState(Dictionary<string, Variant> stateData)
    {
        // State update ignored
    }
}
