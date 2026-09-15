using GameHub.Core.Systems;

namespace GameHub.Core.Visuals;

/// <summary>Small versioned cosmetic mission per playable character, independent of game rules.</summary>
public static class CharacterProgress
{
    public static int Wins(int character) => (int)(SettingsManager.Instance?.GetConfig()?.GetValue("CharacterMissionsV1",CharacterCatalog.Ids[character],0) ?? 0);
    public static string MissionText(int character) => CharacterCatalog.IsBoss(character) ? "Exclusivo de boss" : Wins(character)>=3
        ? "Conquista: presença de mesa · reação especial de truco desbloqueada."
        : $"Missão: vença 3 partidas com este personagem · {Wins(character)}/3.";
    public static string TrucoClip(int character) => !CharacterCatalog.IsBoss(character)&&Wins(character)>=3 ? "flourish" : "truco";
    public static void RecordWin()
    {
        var settings=SettingsManager.Instance;if(settings==null)return;
        int character=CharacterCatalog.Find(settings.CharacterId);
        settings.GetConfig().SetValue("CharacterMissionsV1",CharacterCatalog.Ids[character],Wins(character)+1);
        settings.FlushConfig();
    }
}
