using Godot;
using System.Collections.Generic;

namespace GameHub.Core.Registry;

/// <summary>
/// Singleton responsible for discovering and holding all available games in the Hub.
/// </summary>
public partial class GameRegistry : Node
{
    public enum SoloDifficulty { Fácil, Normal, Difícil }
    public static GameRegistry Instance { get; private set; }
    
    public Dictionary<string, GameDefinition> AvailableGames { get; private set; } = new();

    public static bool IsTutorialMode { get; set; } = false;
    public static int SoloBotCount { get; set; } = 1;
    public static SoloDifficulty SelectedSoloDifficulty { get; set; } = SoloDifficulty.Normal;
    public static int TrucoTeamSize { get; set; } = 1;

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadGames();
        }
        else
        {
            QueueFree();
        }
    }

    private void LoadGames()
    {
        // Scan for known game definition resources
        GD.Print("[GameRegistry] Scanning for games...");

        // Load all registered game definitions
        LoadGameResource("res://games/poker_roguelike/resources/poker_def.tres");
        LoadGameResource("res://games/truco/resources/truco_def.tres");
        LoadGameResource("res://games/fodinha/resources/fodinha_def.tres");
    }

    private void LoadGameResource(string path)
    {
        if (ResourceLoader.Exists(path))
        {
            var def = ResourceLoader.Load<GameDefinition>(path);
            if (def != null && !string.IsNullOrEmpty(def.GameId))
            {
                AvailableGames[def.GameId] = def;
                GD.Print($"[GameRegistry] Registered game: {def.GameName} ({def.GameId})");
            }
        }
    }
}
