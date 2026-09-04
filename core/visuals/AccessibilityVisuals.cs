using Godot;
using GameHub.Core.Systems;

namespace GameHub.Core.Visuals;

/// <summary>Shared visual accessibility treatment for every game and hub screen.</summary>
public static class AccessibilityVisuals
{
    public static void AddGlobalFilter(Control root)
    {
        var settings = SettingsManager.Instance;
        if (settings == null || settings.ColorblindMode == 0 || settings.ColorblindScope != 0) return;

        var shader = ResourceLoader.Load<Shader>("res://assets/shaders/ColorblindFilter.gdshader");
        if (shader == null) return;

        var filter = new ColorRect
        {
            Name = "GlobalColorblindFilter",
            Color = Colors.White,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Material = new ShaderMaterial { Shader = shader }
        };
        ((ShaderMaterial)filter.Material).SetShaderParameter("mode", settings.ColorblindMode);
        filter.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        filter.ZIndex = 4096;
        root.AddChild(filter);
    }

    public static Color GetCardSuitColor(bool redSuit, Color regularRed, Color regularBlack)
    {
        var settings = SettingsManager.Instance;
        if (settings == null || settings.ColorblindMode == 0 || settings.ColorblindScope != 1)
            return redSuit ? regularRed : regularBlack;

        return settings.ColorblindMode switch
        {
            1 => redSuit ? new Color(1.0f, 0.76f, 0.18f) : new Color(0.35f, 0.72f, 1.0f),
            2 => redSuit ? new Color(1.0f, 0.60f, 0.15f) : new Color(0.28f, 0.68f, 1.0f),
            3 => redSuit ? new Color(1.0f, 0.40f, 0.76f) : new Color(0.22f, 0.90f, 0.78f),
            _ => redSuit ? regularRed : regularBlack
        };
    }
}
