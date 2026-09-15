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
            // Dark inks maintain at least 5:1 contrast against the paper card face.
            1 => redSuit ? new Color("#845200") : new Color("#005a8b"),
            2 => redSuit ? new Color("#903d00") : new Color("#155988"),
            3 => redSuit ? new Color("#9b235f") : new Color("#00685c"),
            _ => redSuit ? regularRed : regularBlack
        };
    }
}
