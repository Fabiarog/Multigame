using Godot;

namespace GameHub.Core.Visuals;

/// <summary>Shared visual language for the club, its tables and every dialog.</summary>
public static class ClubTheme
{
    public static readonly Color Ink = new("#091a17");
    public static readonly Color Panel = new("#122923");
    public static readonly Color Paper = new("#f1e4c8");
    public static readonly Color Gold = new("#d6b574");
    public static readonly Color Muted = new("#a2b5a9");
    public static readonly Color Red = new("#ac493e");
    public static readonly Color Green = new("#315f48");
    public static readonly Color Border = new("#3d5141");

    public static Font BodyFont => ResourceLoader.Load<Font>("res://assets/fonts/DM-Sans.ttf");
    public static Font DisplayFont => ResourceLoader.Load<Font>("res://assets/fonts/Cormorant-Garamond.ttf");
    public static Font MonoFont => ResourceLoader.Load<Font>("res://assets/fonts/DM-Mono.ttf");

    public static StyleBoxFlat Box(Color fill, Color border, int padding = 16, int radius = 8)
    {
        var box = new StyleBoxFlat { BgColor = fill, BorderColor = border,
            ContentMarginLeft = padding, ContentMarginRight = padding,
            ContentMarginTop = padding, ContentMarginBottom = padding };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(radius);
        return box;
    }

    public static Theme Create()
    {
        var theme = new Theme { DefaultFont = BodyFont, DefaultFontSize = 16 };
        theme.SetColor("font_color", "Label", Paper);
        theme.SetColor("font_color", "LineEdit", Paper);
        theme.SetColor("font_placeholder_color", "LineEdit", Muted);
        theme.SetStylebox("normal", "LineEdit", Box(Ink, Border, 10, 5));
        theme.SetStylebox("focus", "LineEdit", Box(Colors.Transparent, Gold, 10, 5));
        theme.SetStylebox("panel", "PanelContainer", Box(Panel, Border));
        foreach (var type in new[] { "Button", "OptionButton", "CheckButton", "CheckBox" })
        {
            theme.SetStylebox("normal", type, Box(Panel, Border, 10, 5));
            theme.SetStylebox("hover", type, Box(Green.Darkened(.2f), Gold, 10, 5));
            theme.SetStylebox("pressed", type, Box(Green, Gold, 10, 5));
            theme.SetStylebox("disabled", type, Box(Ink, Border.Darkened(.2f), 10, 5));
            var focus = Box(Colors.Transparent, Gold, 10, 5);
            focus.SetBorderWidthAll(2);
            theme.SetStylebox("focus", type, focus);
            theme.SetColor("font_color", type, Paper);
            theme.SetColor("font_hover_color", type, Paper);
            theme.SetColor("font_pressed_color", type, Paper);
            theme.SetColor("font_disabled_color", type, Muted);
        }
        theme.SetStylebox("panel", "PopupMenu", Box(Panel, Border, 8, 5));
        theme.SetStylebox("hover", "PopupMenu", Box(Green, Gold, 6, 3));
        theme.SetFont("font", "PopupMenu", BodyFont);
        theme.SetFontSize("font_size", "PopupMenu", 16);
        theme.SetColor("font_color", "PopupMenu", Paper);
        theme.SetColor("font_hover_color", "PopupMenu", Paper);
        theme.SetStylebox("background", "ProgressBar", Box(Ink, Border, 0, 3));
        theme.SetStylebox("fill", "ProgressBar", Box(Gold, Gold, 0, 3));
        theme.SetStylebox("slider", "HSlider", Box(Ink, Border, 3, 3));
        theme.SetStylebox("grabber_area", "HSlider", Box(Green, Gold, 3, 3));
        theme.SetStylebox("grabber_area_highlight", "HSlider", Box(Gold, Gold, 3, 3));
        return theme;
    }

    public static Label Label(string text, int size = 16, Color? color = null)
    {
        var label = new Label { Text = text, MouseFilter = Control.MouseFilterEnum.Ignore };
        label.AddThemeColorOverride("font_color", color ?? Paper);
        label.AddThemeFontSizeOverride("font_size", size);
        return label;
    }

    public static Button Button(string text, bool primary = false)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(0, 44),
            MouseDefaultCursorShape = Control.CursorShape.PointingHand };
        button.Pressed += () => Systems.AudioManager.Instance?.PlaySound("select");
        if (primary)
        {
            button.AddThemeStyleboxOverride("normal", Box(Gold, Gold, 12, 5));
            button.AddThemeStyleboxOverride("hover", Box(Paper, Paper, 12, 5));
            button.AddThemeStyleboxOverride("pressed", Box(Gold.Darkened(.15f), Gold, 12, 5));
            button.AddThemeColorOverride("font_color", Ink);
            button.AddThemeColorOverride("font_hover_color", Ink);
            button.AddThemeColorOverride("font_pressed_color", Ink);
        }
        return button;
    }
}
