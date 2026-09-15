using Godot;
using GameHub.Core.Graphics;

namespace GameHub.Core.Systems;

/// <summary>
/// Singleton that manages saving/loading of all game configuration settings.
/// Handles Visuals, Audio, Controls, Profile, and Accessibility.
/// </summary>
public partial class SettingsManager : Node
{
    public static SettingsManager Instance { get; private set; }

    private const string SETTINGS_PATH = "user://settings.cfg";
    private ConfigFile _config = new ConfigFile();

    /// <summary>
    /// Provides read/write access to the shared config file for
    /// subsystem managers (Video, Graphics) that own their own sections.
    /// </summary>
    public ConfigFile GetConfig() => _config;

    /// <summary>
    /// Flushes the shared config file to disk.
    /// Called by subsystem managers after writing their sections.
    /// </summary>
    public void FlushConfig()
    {
        _config.Save(SETTINGS_PATH);
        GD.Print("[Settings] Config flushed to disk.");
    }

    // -- Profile --
    public string PlayerNickname { get; set; } = "Player";
    public string CharacterId { get; set; } = "corvo";
    public int CharacterOutfit { get; set; } = 0;
    public string AvatarBase { get; set; } = "default_base";
    public string AvatarShirt { get; set; } = "default_shirt";
    public string AvatarPants { get; set; } = "default_pants";
    public string AvatarHair { get; set; } = "default_hair";

    // -- Visuals --
    public bool IsFullscreen { get; set; } = true;
    public float ResolutionScale { get; set; } = 1.0f;
    public bool VfxEnabled { get; set; } = true;
    public string RoomTheme { get; set; } = "classic_club";
    public string DefaultCameraMode { get; set; } = "pov";

    // -- Audio --
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;
    public int MusicTrack { get; set; } = 0; // 0 follows the current table; 1..3 selects a loop.

    // -- Accessibility --
    public bool ScreenShakeEnabled { get; set; } = true;
    public bool ReduceMotion { get; set; } = false;
    public int ColorblindMode { get; set; } = 0; // 0 = None, 1 = Protanopia, etc.
    // 0 = entire interface, 1 = cards only
    public int ColorblindScope { get; set; } = 1;

    public override void _EnterTree()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadSettings();
        }
        else
        {
            QueueFree();
        }
    }

    public void LoadSettings()
    {
        Error err = _config.Load(SETTINGS_PATH);
        if (err == Error.Ok)
        {
            // Profile
            PlayerNickname = (string)_config.GetValue("Profile", "Nickname", PlayerNickname);
            CharacterId = (string)_config.GetValue("Profile", "CharacterId", CharacterId);
            CharacterOutfit = (int)_config.GetValue("Profile", "CharacterOutfit", CharacterOutfit);
            AvatarBase = (string)_config.GetValue("Profile", "AvatarBase", AvatarBase);
            AvatarShirt = (string)_config.GetValue("Profile", "AvatarShirt", AvatarShirt);
            AvatarPants = (string)_config.GetValue("Profile", "AvatarPants", AvatarPants);
            AvatarHair = (string)_config.GetValue("Profile", "AvatarHair", AvatarHair);

            // Visuals
            IsFullscreen = (bool)_config.GetValue("Visuals", "Fullscreen", IsFullscreen);
            ResolutionScale = (float)_config.GetValue("Visuals", "ResolutionScale", ResolutionScale);
            VfxEnabled = (bool)_config.GetValue("Visuals", "VfxEnabled", VfxEnabled);
            RoomTheme = (string)_config.GetValue("Visuals", "RoomTheme", RoomTheme);
            DefaultCameraMode = (string)_config.GetValue("Visuals", "DefaultCameraMode", DefaultCameraMode);

            // Audio
            MasterVolume = (float)_config.GetValue("Audio", "MasterVolume", MasterVolume);
            MusicVolume = (float)_config.GetValue("Audio", "MusicVolume", MusicVolume);
            SfxVolume = (float)_config.GetValue("Audio", "SfxVolume", SfxVolume);
            MusicTrack = (int)_config.GetValue("Audio", "MusicTrack", MusicTrack);

            // Accessibility
            ScreenShakeEnabled = (bool)_config.GetValue("Accessibility", "ScreenShake", ScreenShakeEnabled);
            ReduceMotion = (bool)_config.GetValue("Accessibility", "ReduceMotion", ReduceMotion);
            ColorblindMode = (int)_config.GetValue("Accessibility", "ColorblindMode", ColorblindMode);
            ColorblindScope = (int)_config.GetValue("Accessibility", "ColorblindScope", ColorblindScope);

            ApplySettings();
        }
        else
        {
            GD.Print("[Settings] No settings file found, creating default.");
            SaveSettings();
            ApplySettings();
        }
    }

    public void SaveSettings()
    {
        _config.SetValue("Profile", "Nickname", PlayerNickname);
        _config.SetValue("Profile", "CharacterId", CharacterId);
        _config.SetValue("Profile", "CharacterOutfit", CharacterOutfit);
        _config.SetValue("Profile", "AvatarBase", AvatarBase);
        _config.SetValue("Profile", "AvatarShirt", AvatarShirt);
        _config.SetValue("Profile", "AvatarPants", AvatarPants);
        _config.SetValue("Profile", "AvatarHair", AvatarHair);

        _config.SetValue("Visuals", "Fullscreen", IsFullscreen);
        _config.SetValue("Visuals", "ResolutionScale", ResolutionScale);
        _config.SetValue("Visuals", "VfxEnabled", VfxEnabled);
        _config.SetValue("Visuals", "RoomTheme", RoomTheme);
        _config.SetValue("Visuals", "DefaultCameraMode", DefaultCameraMode);

        _config.SetValue("Audio", "MasterVolume", MasterVolume);
        _config.SetValue("Audio", "MusicVolume", MusicVolume);
        _config.SetValue("Audio", "SfxVolume", SfxVolume);
        _config.SetValue("Audio", "MusicTrack", MusicTrack);
        _config.SetValue("Accessibility", "ScreenShake", ScreenShakeEnabled);
        _config.SetValue("Accessibility", "ReduceMotion", ReduceMotion);
        _config.SetValue("Accessibility", "ColorblindMode", ColorblindMode);
        _config.SetValue("Accessibility", "ColorblindScope", ColorblindScope);

        _config.Save(SETTINGS_PATH);
        GD.Print("[Settings] Settings saved.");
    }

    public void ApplySettings()
    {
        // Legacy fullscreen toggle — kept for backward compatibility.
        // The new VideoSettingsManager provides granular display-mode control.
        if (VideoSettingsManager.Instance == null)
        {
            // Fallback when VideoSettingsManager hasn't loaded yet.
            if (IsFullscreen)
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
            else
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
        }

        // Delegate to subsystem managers when available.
        VideoSettingsManager.Instance?.ApplyVideoSettings();
        GraphicsQualityManager.Instance?.ApplyGraphicsSettings();

        AudioManager.Instance?.ApplyVolumes();
        AudioManager.Instance?.RefreshTrack();

        GD.Print("[Settings] Applied current settings to the engine.");
    }
}
