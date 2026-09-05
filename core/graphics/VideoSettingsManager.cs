using Godot;
using System.Collections.Generic;
using System.Linq;

namespace GameHub.Core.Graphics;

/// <summary>
/// Manages video output settings: resolution, display mode, render scale, and VSync.
/// Persists all preferences through the shared settings.cfg via SettingsManager.
/// Registered as an autoload singleton.
/// </summary>
public partial class VideoSettingsManager : Node
{
    public static VideoSettingsManager Instance { get; private set; }

    // ───────────────────────────── Enums ─────────────────────────────

    /// <summary>
    /// Display modes supported by the engine.
    /// </summary>
    public enum DisplayMode
    {
        Windowed,
        Fullscreen,
        BorderlessFullscreen
    }

    // ───────────────────────────── Resolution Preset ─────────────────

    /// <summary>
    /// Immutable preset describing a standard resolution tier.
    /// </summary>
    public readonly struct ResolutionPreset
    {
        public string Label { get; }
        public Vector2I Size { get; }

        public ResolutionPreset(string label, int width, int height)
        {
            Label = label;
            Size = new Vector2I(width, height);
        }

        public override string ToString() => $"{Label} ({Size.X}×{Size.Y})";
    }

    // ───────────────────────────── Catalog ───────────────────────────

    /// <summary>
    /// All recognized resolution tiers, from lowest to highest.
    /// </summary>
    public static readonly ResolutionPreset[] Presets =
    {
        new("720p  — HD",        1280, 720),
        new("1080p — Full HD",   1920, 1080),
        new("1440p — Quad HD",   2560, 1440),
        new("4K    — Ultra HD",  3840, 2160),
    };

    // ───────────────────────────── Current State ─────────────────────

    /// <summary>Index into <see cref="Presets"/> for the chosen resolution.</summary>
    public int ResolutionIndex { get; set; } = 0;

    /// <summary>Current display mode.</summary>
    public DisplayMode CurrentDisplayMode { get; set; } = DisplayMode.Fullscreen;

    /// <summary>
    /// Internal render scale (0.5 – 1.0).
    /// Lower values render 3D at reduced resolution while keeping UI crisp.
    /// </summary>
    public float RenderScale { get; set; } = 1.0f;

    /// <summary>Vertical sync toggle.</summary>
    public bool VSyncEnabled { get; set; } = true;

    // ───────────────────────────── Monitor Info ──────────────────────

    /// <summary>The native resolution reported by the primary monitor.</summary>
    public Vector2I NativeScreenSize { get; private set; }

    /// <summary>
    /// Subset of <see cref="Presets"/> whose resolution fits on the current monitor.
    /// Populated on _Ready().
    /// </summary>
    public List<ResolutionPreset> AvailablePresets { get; private set; } = new();

    // ───────────────────────────── Signals ───────────────────────────

    [Signal]
    public delegate void VideoSettingsAppliedEventHandler();

    [Signal]
    public delegate void ResolutionChangedEventHandler(int width, int height);

    // ───────────────────────────── Lifecycle ─────────────────────────

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

    public override void _Ready()
    {
        DetectNativeResolution();
        BuildAvailablePresets();
        LoadVideoSettings();
        ApplyVideoSettings();
    }

    // ───────────────────────────── Detection ─────────────────────────

    /// <summary>
    /// Queries the OS for the primary display's native resolution.
    /// </summary>
    private void DetectNativeResolution()
    {
        NativeScreenSize = DisplayServer.ScreenGetSize();
        GD.Print($"[Video] Native screen resolution: {NativeScreenSize.X}×{NativeScreenSize.Y}");
    }

    /// <summary>
    /// Filters the preset catalog to only include resolutions that fit
    /// within the detected monitor size.
    /// </summary>
    private void BuildAvailablePresets()
    {
        AvailablePresets = Presets
            .Where(p => p.Size.X <= NativeScreenSize.X && p.Size.Y <= NativeScreenSize.Y)
            .ToList();

        if (AvailablePresets.Count == 0)
        {
            // Fallback: always offer at least 720p.
            AvailablePresets.Add(Presets[0]);
            GD.PrintErr("[Video] No preset fits the screen — falling back to 720p.");
        }

        GD.Print($"[Video] {AvailablePresets.Count} resolution preset(s) available.");
    }

    // ───────────────────────────── Persistence ───────────────────────

    /// <summary>
    /// Loads video preferences from the shared settings.cfg file.
    /// Falls back to sensible defaults if the section doesn't exist yet.
    /// </summary>
    public void LoadVideoSettings()
    {
        var sm = Systems.SettingsManager.Instance;
        if (sm == null) return;

        var cfg = sm.GetConfig();
        if (cfg == null) return;

        ResolutionIndex    = (int)cfg.GetValue("Video", "ResolutionIndex", FindBestDefaultIndex());
        CurrentDisplayMode = (DisplayMode)(int)cfg.GetValue("Video", "DisplayMode", (int)DisplayMode.Fullscreen);
        RenderScale        = Mathf.Clamp((float)cfg.GetValue("Video", "RenderScale", 1.0f), 0.5f, 1.0f);
        VSyncEnabled       = (bool)cfg.GetValue("Video", "VSync", true);

        // Clamp index to available range.
        ResolutionIndex = Mathf.Clamp(ResolutionIndex, 0, AvailablePresets.Count - 1);

        GD.Print($"[Video] Loaded: {AvailablePresets[ResolutionIndex]}, Mode={CurrentDisplayMode}, Scale={RenderScale:P0}, VSync={VSyncEnabled}");
    }

    /// <summary>
    /// Writes current video preferences to the shared settings.cfg file.
    /// </summary>
    public void SaveVideoSettings()
    {
        var sm = Systems.SettingsManager.Instance;
        if (sm == null) return;

        var cfg = sm.GetConfig();
        if (cfg == null) return;

        cfg.SetValue("Video", "ResolutionIndex", ResolutionIndex);
        cfg.SetValue("Video", "DisplayMode",     (int)CurrentDisplayMode);
        cfg.SetValue("Video", "RenderScale",     RenderScale);
        cfg.SetValue("Video", "VSync",           VSyncEnabled);

        sm.FlushConfig();
        GD.Print("[Video] Settings saved.");
    }

    // ───────────────────────────── Apply ─────────────────────────────

    /// <summary>
    /// Applies all current video settings to the engine in one shot.
    /// Safe to call at any time — validates against monitor limits first.
    /// </summary>
    public void ApplyVideoSettings()
    {
        // --- Display mode ---
        switch (CurrentDisplayMode)
        {
            case DisplayMode.Windowed:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, false);
                break;

            case DisplayMode.Fullscreen:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen);
                break;

            case DisplayMode.BorderlessFullscreen:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
                DisplayServer.WindowSetFlag(DisplayServer.WindowFlags.Borderless, true);
                break;
        }

        // --- Resolution ---
        ResolutionIndex = Mathf.Clamp(ResolutionIndex, 0, AvailablePresets.Count - 1);
        var chosen = AvailablePresets[ResolutionIndex];

        if (CurrentDisplayMode == DisplayMode.Windowed)
        {
            // In windowed mode, resize the window itself.
            DisplayServer.WindowSetSize(chosen.Size);
            CenterWindow();
        }

        // Always update the viewport base size so the engine scales content.
        GetTree().Root.ContentScaleSize = chosen.Size;

        EmitSignal(SignalName.ResolutionChanged, chosen.Size.X, chosen.Size.Y);
        GD.Print($"[Video] Resolution set to {chosen}");

        // --- Render scale (3D) ---
        RenderScale = Mathf.Clamp(RenderScale, 0.5f, 1.0f);
        GetViewport().Scaling3DScale = RenderScale;
        GD.Print($"[Video] Render scale: {RenderScale:P0}");

        // --- VSync ---
        DisplayServer.WindowSetVsyncMode(
            VSyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled
        );

        EmitSignal(SignalName.VideoSettingsApplied);
        GD.Print("[Video] All video settings applied.");
    }

    // ───────────────────────────── Helpers ───────────────────────────

    /// <summary>
    /// Centers the window on the primary monitor (useful after a windowed resize).
    /// </summary>
    private void CenterWindow()
    {
        var screenSize = DisplayServer.ScreenGetSize();
        var winSize    = DisplayServer.WindowGetSize();
        var pos = (screenSize - winSize) / 2;
        DisplayServer.WindowSetPosition(pos);
    }

    /// <summary>
    /// Picks the highest preset that fits the native screen as the default.
    /// </summary>
    private int FindBestDefaultIndex()
    {
        for (int i = AvailablePresets.Count - 1; i >= 0; i--)
        {
            if (AvailablePresets[i].Size.X <= NativeScreenSize.X &&
                AvailablePresets[i].Size.Y <= NativeScreenSize.Y)
                return i;
        }
        return 0;
    }

    // ───────────────────────────── Public API ────────────────────────

    /// <summary>
    /// Convenience: sets resolution by preset index, applies and saves.
    /// </summary>
    public void SetResolution(int index)
    {
        ResolutionIndex = Mathf.Clamp(index, 0, AvailablePresets.Count - 1);
        ApplyVideoSettings();
        SaveVideoSettings();
    }

    /// <summary>
    /// Convenience: sets display mode, applies and saves.
    /// </summary>
    public void SetDisplayMode(DisplayMode mode)
    {
        CurrentDisplayMode = mode;
        ApplyVideoSettings();
        SaveVideoSettings();
    }

    /// <summary>
    /// Convenience: sets render scale (clamped to 0.5–1.0), applies and saves.
    /// </summary>
    public void SetRenderScale(float scale)
    {
        RenderScale = Mathf.Clamp(scale, 0.5f, 1.0f);
        ApplyVideoSettings();
        SaveVideoSettings();
    }

    /// <summary>
    /// Convenience: toggles VSync, applies and saves.
    /// </summary>
    public void SetVSync(bool enabled)
    {
        VSyncEnabled = enabled;
        ApplyVideoSettings();
        SaveVideoSettings();
    }
}
