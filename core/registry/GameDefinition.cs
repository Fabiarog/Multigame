using Godot;

namespace GameHub.Core.Registry;

/// <summary>
/// Defines a playable game within the Game Hub.
/// Each game (like Poker or Truco) will have a Resource instance of this class.
/// </summary>
[GlobalClass]
public partial class GameDefinition : Resource
{
    [Export] public string GameId { get; set; } = "";
    [Export] public string GameName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    
    [Export] public Texture2D Icon { get; set; }
    [Export] public Texture2D Thumbnail { get; set; }
    
    [Export] public int MinPlayers { get; set; } = 1;
    [Export] public int MaxPlayers { get; set; } = 6;
    
    // Path to the main scene of this game, which the Hub will load.
    [Export(PropertyHint.File, "*.tscn")] public string MainScenePath { get; set; } = "";
    
    [Export] public string Version { get; set; } = "1.0.0";
}
