using Godot;

namespace GameHub.Core.Systems;

/// <summary>
/// Handles playing SFX and Music. 
/// In a real project, this would manage AudioStreamPlayers, buses, and volume settings.
/// </summary>
public partial class AudioManager : Node
{
    public static AudioManager Instance { get; private set; }

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            QueueFree();
        }
    }

    public void PlaySound(string soundName)
    {
        // Dummy implementation for now.
        GD.Print($"[Audio] Playing sound: {soundName}");
    }

    public void PlayMusic(string trackName)
    {
        GD.Print($"[Audio] Playing music: {trackName}");
    }
}
