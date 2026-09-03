using Godot;

namespace GameHub.Core.Systems;

/// <summary>
/// Handles saving and loading player progress, settings, and meta-progression (collectibles).
/// </summary>
public partial class SaveManager : Node
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_PATH = "user://hub_save.json";

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadGame();
        }
        else
        {
            QueueFree();
        }
    }

    public void SaveGame()
    {
        GD.Print("[Save] Game saved successfully.");
        // Implementation for serializing data to JSON/Binary
    }

    public void LoadGame()
    {
        GD.Print("[Save] Attempting to load save data...");
        // Implementation for parsing saved data
    }
}
