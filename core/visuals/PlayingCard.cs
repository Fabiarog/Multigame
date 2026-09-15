using Godot;

namespace GameHub.Core.Visuals;

/// <summary>Resolution independent card faces. Rank and suit always come from game data.</summary>
public partial class PlayingCard : Control
{
    private string _rankText = "A";
    private string _suitSymbol = "♠";
    private bool _faceDown, _special, _selected;
    public string RankText { get => _rankText; set { _rankText = value; QueueRedraw(); } }
    public string SuitSymbol { get => _suitSymbol; set { _suitSymbol = value; QueueRedraw(); } }
    public bool FaceDown { get => _faceDown; set { _faceDown = value; QueueRedraw(); } }
    public bool Special { get => _special; set { _special = value; QueueRedraw(); } }
    public bool Selected { get => _selected; set { _selected = value; QueueRedraw(); } }

    public PlayingCard()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        CustomMinimumSize = new Vector2(64, 92);
        Resized += QueueRedraw;
    }

    public override void _Draw()
    {
        var bounds = new Rect2(Vector2.Zero, Size);
        var shadow = ClubTheme.Box(new Color(0, 0, 0, .25f), Colors.Transparent, 0, 6);
        DrawStyleBox(shadow, new Rect2(new Vector2(0, 4), Size));
        var border = Selected || Special ? ClubTheme.Gold : new Color("#c7b792");
        var box = ClubTheme.Box(FaceDown ? ClubTheme.Ink : ClubTheme.Paper, border, 0, 6);
        box.SetBorderWidthAll(Selected || Special ? 3 : 1);
        DrawStyleBox(box, bounds);
        float scale = Mathf.Min(Size.X / 76f, Size.Y / 108f);
        var inset = bounds.Grow(-7 * scale);
        var line = new Color(border, .55f);
        DrawStyleBox(ClubTheme.Box(Colors.Transparent, line, 0, 3), inset);
        if (FaceDown)
        {
            var motif = new Color(ClubTheme.Gold, .22f);
            for (float x = 12 * scale; x < Size.X - 8 * scale; x += 10 * scale)
                for (float y = 12 * scale; y < Size.Y - 8 * scale; y += 10 * scale)
                    DrawCircle(new Vector2(x, y), 1.1f * scale, motif);
            DrawCircle(Size / 2, 19 * scale, ClubTheme.Panel);
            DrawArc(Size / 2, 19 * scale, 0, Mathf.Tau, 48, ClubTheme.Gold, scale, true);
            DrawSuit(Size / 2, 10 * scale, ClubTheme.Gold, "♠");
            return;
        }

        bool red = SuitSymbol == "♥" || SuitSymbol == "♦";
        var ink = AccessibilityVisuals.GetCardSuitColor(red, new Color("#9d332e"), ClubTheme.Ink);
        int fontSize = Mathf.RoundToInt(22 * scale);
        var font = ClubTheme.DisplayFont;
        DrawString(font, new Vector2(10 * scale, 26 * scale), RankText, HorizontalAlignment.Left, -1, fontSize, ink);
        DrawSuit(new Vector2(17 * scale, 36 * scale), 4 * scale, ink, SuitSymbol);
        DrawSuit(Size / 2, 15 * scale, ink, SuitSymbol);

        DrawSetTransform(Size, Mathf.Pi);
        DrawString(font, new Vector2(10 * scale, 26 * scale), RankText, HorizontalAlignment.Left, -1, fontSize, ink);
        DrawSuit(new Vector2(17 * scale, 36 * scale), 4 * scale, ink, SuitSymbol);
        DrawSetTransform(Vector2.Zero);
        if (Special || Selected)
        {
            DrawCircle(new Vector2(Size.X - 7 * scale, 7 * scale), 6 * scale, ClubTheme.Gold);
            if (Special)
                DrawString(ClubTheme.MonoFont, new Vector2(Size.X - 10 * scale, 10 * scale), "M", HorizontalAlignment.Left, -1, Mathf.RoundToInt(9 * scale), ClubTheme.Ink);
            else
            {
                DrawLine(new Vector2(Size.X - 10 * scale, 7 * scale), new Vector2(Size.X - 8 * scale, 9 * scale), ClubTheme.Ink, 1.5f * scale);
                DrawLine(new Vector2(Size.X - 8 * scale, 9 * scale), new Vector2(Size.X - 4 * scale, 4 * scale), ClubTheme.Ink, 1.5f * scale);
            }
        }
    }

    // Suit geometry does not depend on system font glyph coverage.
    private void DrawSuit(Vector2 c, float r, Color color, string suit)
    {
        if (suit == "♦")
        {
            DrawColoredPolygon(new[] { c + new Vector2(0, -r), c + new Vector2(r * .7f, 0), c + new Vector2(0, r), c + new Vector2(-r * .7f, 0) }, color);
            return;
        }
        if (suit == "♣")
        {
            DrawCircle(c + new Vector2(0, -r * .5f), r * .48f, color);
            DrawCircle(c + new Vector2(-r * .47f, r * .05f), r * .48f, color);
            DrawCircle(c + new Vector2(r * .47f, r * .05f), r * .48f, color);
        }
        else
        {
            float sign = suit == "♥" ? 1 : -1;
            DrawCircle(c + new Vector2(-r * .43f, -r * .32f * sign), r * .52f, color);
            DrawCircle(c + new Vector2(r * .43f, -r * .32f * sign), r * .52f, color);
            DrawColoredPolygon(new[] { c + new Vector2(-r * .89f, -.07f * r * sign), c + new Vector2(r * .89f, -.07f * r * sign), c + new Vector2(0, r * sign) }, color);
        }
        if (suit != "♥")
            DrawColoredPolygon(new[] { c + new Vector2(0, r * .15f), c + new Vector2(-r * .35f, r), c + new Vector2(r * .35f, r) }, color);
    }
}
