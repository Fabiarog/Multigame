using Godot;

namespace GameHub.Core.Visuals;

/// <summary>Quiet felt and brass details that keep the hand readable at any resolution.</summary>
public partial class ClubBackdrop : Control
{
    public bool ShowTable { get; set; } = true;

    public ClubBackdrop()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Resized += QueueRedraw;
    }

    public override void _Ready()
    {
        // Keep the project name/user:// path stable so existing settings survive.
        DisplayServer.WindowSetTitle("MultiGame · Clube de cartas");
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), ClubTheme.Ink);
        // A static weave avoids both flicker and continuous redraw costs.
        var weave = new Color(ClubTheme.Gold, .028f);
        for (int y = 4; y < Size.Y; y += 8)
            DrawLine(new Vector2(0, y), new Vector2(Size.X, y), weave);
        for (int x = 4; x < Size.X; x += 8)
            DrawLine(new Vector2(x, 0), new Vector2(x, Size.Y), new Color(ClubTheme.Green, .05f));

        if (ShowTable)
        {
            var rect = new Rect2(Size.X * .235f, Size.Y * .18f, Size.X * .73f, Size.Y * .58f);
            var table = ClubTheme.Box(new Color("#12382c"), new Color("#806744"), 0, 150);
            table.SetBorderWidthAll(3);
            DrawStyleBox(table, rect);
            DrawStyleBox(ClubTheme.Box(Colors.Transparent, new Color("#355640"), 0, 130), rect.Grow(-14));
        }
        DrawRect(new Rect2(new Vector2(12, 12), Size - new Vector2(24, 24)), new Color(ClubTheme.Gold, .2f), false);
        var marks = new[] { new Vector2(12, 12), new Vector2(Size.X - 12, 12), new Vector2(12, Size.Y - 12), Size - new Vector2(12, 12) };
        foreach (var p in marks) DrawCircle(p, 3, ClubTheme.Gold);
    }
}
