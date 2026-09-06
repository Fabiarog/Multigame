using Godot;

namespace GameHub.Core.Visuals;

/// <summary>Original club characters, shared by portraits and the 2.5D seats.</summary>
public static class CharacterCatalog
{
    public const int PlayableCount = 6;
    public static readonly string[] Ids = { "nina", "bento", "corvo", "onca", "iara", "zeca", "barao", "dama" };
    public static readonly string[] Names = { "Nina", "Bento", "Seu Corvo", "Dona Onça", "Iara", "Zeca", "Barão da Meia-Noite", "Dama de Copas" };
    public static readonly string[] Descriptions = {
        "A inventora · um bom plano antes de cada mão.",
        "O anfitrião · sempre cabe mais um blefe.",
        "O observador · de olho em cada carta.",
        "A veterana · presença forte à mesa.",
        "A capivara · calma no olhar, firme no palpite.",
        "A raposa · um floreio antes do blefe.",
        "Boss exclusivo · o dono da última palavra.",
        "Boss exclusivo · o sorriso antes do bote."
    };

    public static int Find(string id) => Mathf.Clamp(System.Array.IndexOf(Ids, id), 0, PlayableCount - 1);
    public static bool IsBoss(int index) => index >= PlayableCount;
    public static int BossForRound(int round) => PlayableCount + ((Mathf.Max(1,round)-1)/2)%2;
    public static string ModelPath(int index) => $"res://assets/models/club/{Ids[Mathf.PosMod(index,Ids.Length)]}.glb";

    public static Texture2D Portrait(int index, bool react = false)
    {
        var sheet = GD.Load<Texture2D>("res://assets/sprites/characters/club/club-cast.png");
        if (sheet == null) return GD.Load<Texture2D>($"res://assets/models/club/{Ids[Mathf.PosMod(index, Ids.Length)]}.png");
        var frame = sheet.GetSize() / new Vector2(8, 2);
        return new AtlasTexture { Atlas = sheet, Region = new Rect2(new Vector2(Mathf.PosMod(index, 8) * frame.X, react ? frame.Y : 0), frame) };
    }
}
