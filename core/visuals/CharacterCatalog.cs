using Godot;

namespace GameHub.Core.Visuals;

/// <summary>Original club characters, shared by portraits and the 2.5D seats.</summary>
public static class CharacterCatalog
{
    public static readonly string[] Ids = { "nina", "bento", "corvo", "onca" };
    public static readonly string[] Names = { "Nina", "Bento", "Seu Corvo", "Dona Onça" };
    public static readonly string[] Descriptions = {
        "A inventora · um bom plano antes de cada mão.",
        "O anfitrião · sempre cabe mais um blefe.",
        "O observador · de olho em cada carta.",
        "A veterana · presença forte à mesa."
    };

    public static int Find(string id) => Mathf.Max(0, System.Array.IndexOf(Ids, id));

    public static Texture2D Portrait(int index, bool react = false)
    {
        var sheet = GD.Load<Texture2D>("res://assets/sprites/characters/club/club-cast.png");
        if (sheet == null) return null;
        var frame = sheet.GetSize() / new Vector2(4, 2);
        return new AtlasTexture { Atlas = sheet, Region = new Rect2(new Vector2(Mathf.PosMod(index, 4) * frame.X, react ? frame.Y : 0), frame) };
    }
}
