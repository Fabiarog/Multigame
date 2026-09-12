using Godot;

namespace GameHub.Core.Visuals;

/// <summary>Original club characters, shared by portraits and the 2.5D seats.</summary>
public static class CharacterCatalog
{
    public const int PlayableCount = 7;
    public static readonly string[] Ids = { "nina", "bento", "corvo", "onca", "iara", "zeca", "aki", "barao", "dama", "morgana", "carnical" };
    public static readonly string[] Names = { "Nina", "Bento", "Seu Corvo", "Dona Onça", "Iara", "Zeca", "Aki", "Barão da Meia-Noite", "Dama de Copas", "Madame Morgana", "Lorde Carniçal" };
    public static readonly string[] Descriptions = {
        "A inventora · um bom plano antes de cada mão.",
        "O anfitrião · sempre cabe mais um blefe.",
        "O observador · de olho em cada carta.",
        "A veterana · presença forte à mesa.",
        "A capivara · calma no olhar, firme no palpite.",
        "A raposa · um floreio antes do blefe.",
        "A estrategista misteriosa · precisão afiada a cada jogada.",
        "Boss exclusivo · o dono da última palavra.",
        "Boss exclusivo · o sorriso antes do bote.",
        "Boss exclusivo · feitiços sombrios e apostas fatais.",
        "Boss exclusivo · a fome insaciável pelas suas fichas."
    };

    public static int Find(string id) => Mathf.Clamp(System.Array.IndexOf(Ids, id), 0, PlayableCount - 1);
    public static bool IsBoss(int index) => index >= PlayableCount;
    public static int BossForRound(int round) => PlayableCount + ((Mathf.Max(1, round) - 1) / 2) % 4;
    public static string ModelPath(int index) => $"res://assets/models/club/{Ids[Mathf.PosMod(index, Ids.Length)]}.glb";

    public static Texture2D Portrait(int index, bool react = false)
    {
        string id = Ids[Mathf.PosMod(index, Ids.Length)];
        string path3d = $"res://assets/models/club/{id}_3d.png";
        if (ResourceLoader.Exists(path3d))
        {
            var tex = GD.Load<Texture2D>(path3d);
            if (tex != null) return tex;
        }
        string path2d = $"res://assets/models/club/{id}.png";
        if (ResourceLoader.Exists(path2d))
        {
            var tex = GD.Load<Texture2D>(path2d);
            if (tex != null) return tex;
        }
        var sheet = GD.Load<Texture2D>("res://assets/sprites/characters/club/club-cast.png");
        if (sheet == null) return null;
        var frame = sheet.GetSize() / new Vector2(8, 2);
        return new AtlasTexture { Atlas = sheet, Region = new Rect2(new Vector2(Mathf.PosMod(index, 8) * frame.X, react ? frame.Y : 0), frame) };
    }

    public static Texture2D TrucoCallSprite(int index, int stakes)
    {
        string id = Ids[Mathf.PosMod(index, Ids.Length)];
        int validStakes = stakes switch { 6 => 6, 9 => 9, 12 => 12, _ => 3 };
        string path = $"res://assets/sprites/truco_calls/{id}_{validStakes}.png";
        if (ResourceLoader.Exists(path))
        {
            var tex = GD.Load<Texture2D>(path);
            if (tex != null) return tex;
        }
        return Portrait(index, react: stakes >= 6);
    }
}
