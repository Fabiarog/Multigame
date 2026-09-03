using Godot;

namespace GameHub.Core.Scenarios;

/// <summary>
/// Defines a background scenario/environment that can be loaded during a match.
/// Each scenario has its own visual theme (Casino, Pirate Ship, etc.)
/// and is stored as a Resource so the game can select one at random or by player choice.
/// </summary>
[GlobalClass]
public partial class ScenarioDefinition : Resource
{
    [Export] public string ScenarioId { get; set; } = "";
    [Export] public string DisplayName { get; set; } = "";
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = "";
    [Export] public Texture2D Thumbnail { get; set; }

    /// <summary>
    /// Path to the PackedScene that contains the 3D environment (or 2.5D background layers).
    /// This scene will be instanced behind the card table during a match.
    /// </summary>
    [Export(PropertyHint.File, "*.tscn")] public string EnvironmentScenePath { get; set; } = "";

    /// <summary>
    /// Optional: Path to an ambient music track for this scenario.
    /// </summary>
    [Export(PropertyHint.File, "*.ogg,*.wav,*.mp3")] public string AmbientMusicPath { get; set; } = "";

    /// <summary>
    /// Color grading / post-processing tint for this environment.
    /// </summary>
    [Export] public Color AmbientColor { get; set; } = new Color(1, 1, 1, 1);

    /// <summary>
    /// Light intensity multiplier for this scenario (e.g., dim bar vs. bright casino).
    /// </summary>
    [Export(PropertyHint.Range, "0.1,2.0")] public float LightIntensity { get; set; } = 1.0f;
}
