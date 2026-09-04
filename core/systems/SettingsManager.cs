using Godot;

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

    // -- Profile --
    public string PlayerNickname { get; set; } = "Player";
    public string AvatarBase { get; set; } = "default_base";
    public string AvatarShirt { get; set; } = "default_shirt";
    public string AvatarPants { get; set; } = "default_pants";
    public string AvatarHair { get; set; } = "default_hair";

    // -- Visuals --
    public bool IsFullscreen { get; set; } = true;
    public float ResolutionScale { get; set; } = 1.0f;
    public bool VfxEnabled { get; set; } = true;

    // -- Audio --
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float SfxVolume { get; set; } = 1.0f;

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
            AvatarBase = (string)_config.GetValue("Profile", "AvatarBase", AvatarBase);
            AvatarShirt = (string)_config.GetValue("Profile", "AvatarShirt", AvatarShirt);
            AvatarPants = (string)_config.GetValue("Profile", "AvatarPants", AvatarPants);
            AvatarHair = (string)_config.GetValue("Profile", "AvatarHair", AvatarHair);

            // Visuals
            IsFullscreen = (bool)_config.GetValue("Visuals", "Fullscreen", IsFullscreen);
            ResolutionScale = (float)_config.GetValue("Visuals", "ResolutionScale", ResolutionScale);
            VfxEnabled = (bool)_config.GetValue("Visuals", "VfxEnabled", VfxEnabled);

            // Audio
            MasterVolume = (float)_config.GetValue("Audio", "MasterVolume", MasterVolume);
            MusicVolume = (float)_config.GetValue("Audio", "MusicVolume", MusicVolume);
            SfxVolume = (float)_config.GetValue("Audio", "SfxVolume", SfxVolume);

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
        _config.SetValue("Profile", "AvatarBase", AvatarBase);
        _config.SetValue("Profile", "AvatarShirt", AvatarShirt);
        _config.SetValue("Profile", "AvatarPants", AvatarPants);
        _config.SetValue("Profile", "AvatarHair", AvatarHair);

        _config.SetValue("Visuals", "Fullscreen", IsFullscreen);
        _config.SetValue("Visuals", "ResolutionScale", ResolutionScale);
        _config.SetValue("Visuals", "VfxEnabled", VfxEnabled);

        _config.SetValue("Audio", "MasterVolume", MasterVolume);
        _config.SetValue("Audio", "MusicVolume", MusicVolume);
        _config.SetValue("Audio", "SfxVolume", SfxVolume);

        _config.SetValue("Accessibility", "ScreenShake", ScreenShakeEnabled);
        _config.SetValue("Accessibility", "ReduceMotion", ReduceMotion);
        _config.SetValue("Accessibility", "ColorblindMode", ColorblindMode);
        _config.SetValue("Accessibility", "ColorblindScope", ColorblindScope);

        _config.Save(SETTINGS_PATH);
        GD.Print("[Settings] Settings saved.");
    }

    public void ApplySettings()
    {
        // Visuals
        if (IsFullscreen)
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
        else
            DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);

        // Here we would also update AudioServer buses based on volumes.
        // int masterBus = AudioServer.GetBusIndex("Master");
        // AudioServer.SetBusVolumeDb(masterBus, Mathf.LinearToDb(MasterVolume));

        GD.Print("[Settings] Applied current settings to the engine.");
    }
}
