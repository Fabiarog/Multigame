using Godot;

namespace GameHub.Core.Graphics;

/// <summary>
/// Singleton that bridges <see cref="RayTracingSettings"/> with the
/// Godot <see cref="RenderingServer"/>.
///
/// On _Ready() it probes hardware via <see cref="HardwareCapabilityDetector"/>,
/// disables RT if unsupported, and applies the stored configuration.
///
/// Because Godot 4.7 does not expose native hardware RT (DXR/Vulkan RT),
/// the "ray tracing" toggles currently control the best screen-space
/// approximations available (SSAO ↔ RTAO, SSR ↔ Reflections, SDFGI ↔ RTGI).
/// When native RT lands in Godot, only this class needs updating.
/// </summary>
public partial class GraphicsQualityManager : Node
{
    public static GraphicsQualityManager Instance { get; private set; }

    // ───────────────────────────── State ─────────────────────────────

    /// <summary>Current ray-tracing / quality configuration.</summary>
    public RayTracingSettings RtSettings { get; private set; } = new();

    // ───────────────────────────── Signals ───────────────────────────

    /// <summary>
    /// Emitted once during _Ready() when the hardware does not
    /// support ray tracing. UI can listen to show a warning badge.
    /// </summary>
    [Signal]
    public delegate void RayTracingNotSupportedEventHandler(string reason);

    /// <summary>Emitted after any successful ApplyGraphicsSettings() call.</summary>
    [Signal]
    public delegate void GraphicsSettingsAppliedEventHandler();

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
        // 1. Probe hardware
        HardwareCapabilityDetector.Detect();

        // 2. Load persisted settings
        LoadGraphicsSettings();

        // 3. Validate RT support
        ValidateRayTracingSupport();

        // 4. Apply everything
        ApplyGraphicsSettings();
    }

    // ───────────────────────────── Validation ────────────────────────

    /// <summary>
    /// Checks whether the current hardware/renderer can sustain RT effects.
    /// If not, force-disables them and emits a signal.
    /// </summary>
    private void ValidateRayTracingSupport()
    {
        if (!RtSettings.RayTracingEnabled) return;

        // Gate 1: Compatibility renderer cannot run SDFGI/SSR/SSAO.
        if (!HardwareCapabilityDetector.IsForwardPlusRenderer)
        {
            string reason = "O renderer ativo é Compatibility — RT requer Forward+.";
            GD.PrintErr($"[Graphics] {reason}");
            RtSettings.DisableAll();
            EmitSignal(SignalName.RayTracingNotSupported, reason);
            return;
        }

        // Gate 2: GPU heuristic.
        if (!HardwareCapabilityDetector.SupportsRayTracing)
        {
            string reason = $"GPU '{HardwareCapabilityDetector.GpuName}' não é reconhecida como compatível com RT.";
            GD.PrintErr($"[Graphics] {reason}");
            RtSettings.DisableAll();
            EmitSignal(SignalName.RayTracingNotSupported, reason);
            return;
        }

        GD.Print("[Graphics] RT hardware validation passed.");
    }

    // ───────────────────────────── Persistence ───────────────────────

    /// <summary>
    /// Loads graphics quality preferences from the shared settings.cfg.
    /// </summary>
    public void LoadGraphicsSettings()
    {
        var sm = Systems.SettingsManager.Instance;
        if (sm == null) return;

        var cfg = sm.GetConfig();
        if (cfg == null) return;

        RtSettings.RayTracingEnabled    = (bool)cfg.GetValue("Graphics", "RayTracingEnabled",    false);
        RtSettings.RtaoEnabled          = (bool)cfg.GetValue("Graphics", "RtaoEnabled",          false);
        RtSettings.RtaoQuality          = (RayTracingSettings.RtQualityLevel)(int)cfg.GetValue("Graphics", "RtaoQuality", 1);
        RtSettings.RtReflectionsEnabled = (bool)cfg.GetValue("Graphics", "RtReflectionsEnabled", false);
        RtSettings.RtReflectionsQuality = (RayTracingSettings.RtQualityLevel)(int)cfg.GetValue("Graphics", "RtReflectionsQuality", 1);
        RtSettings.RtgiEnabled          = (bool)cfg.GetValue("Graphics", "RtgiEnabled",          false);
        RtSettings.RtgiQuality          = (RayTracingSettings.RtQualityLevel)(int)cfg.GetValue("Graphics", "RtgiQuality", 1);

        GD.Print($"[Graphics] Loaded: RT={RtSettings.RayTracingEnabled}, AO={RtSettings.RtaoEnabled}, Refl={RtSettings.RtReflectionsEnabled}, GI={RtSettings.RtgiEnabled}");
    }

    /// <summary>
    /// Persists the current graphics quality configuration.
    /// </summary>
    public void SaveGraphicsSettings()
    {
        var sm = Systems.SettingsManager.Instance;
        if (sm == null) return;

        var cfg = sm.GetConfig();
        if (cfg == null) return;

        cfg.SetValue("Graphics", "RayTracingEnabled",    RtSettings.RayTracingEnabled);
        cfg.SetValue("Graphics", "RtaoEnabled",          RtSettings.RtaoEnabled);
        cfg.SetValue("Graphics", "RtaoQuality",          (int)RtSettings.RtaoQuality);
        cfg.SetValue("Graphics", "RtReflectionsEnabled", RtSettings.RtReflectionsEnabled);
        cfg.SetValue("Graphics", "RtReflectionsQuality", (int)RtSettings.RtReflectionsQuality);
        cfg.SetValue("Graphics", "RtgiEnabled",          RtSettings.RtgiEnabled);
        cfg.SetValue("Graphics", "RtgiQuality",          (int)RtSettings.RtgiQuality);

        sm.FlushConfig();
        GD.Print("[Graphics] Settings saved.");
    }

    // ───────────────────────────── Apply ─────────────────────────────

    /// <summary>
    /// Translates the current <see cref="RtSettings"/> into concrete
    /// RenderingServer / Environment parameters.
    /// </summary>
    public void ApplyGraphicsSettings()
    {
        var env = GetViewportEnvironment();

        if (env == null)
        {
            GD.PrintErr("[Graphics] No Environment found on the current WorldEnvironment — skipping apply.");
            return;
        }

        // ── SSAO (stands in for RTAO) ──────────────────────────────
        bool useAo = RtSettings.RayTracingEnabled && RtSettings.RtaoEnabled;
        env.SsaoEnabled = useAo;
        if (useAo)
            ApplySsaoQuality(env, RtSettings.RtaoQuality);

        // ── SSR (stands in for RT Reflections) ─────────────────────
        bool useRefl = RtSettings.RayTracingEnabled && RtSettings.RtReflectionsEnabled;
        env.SsrEnabled = useRefl;
        if (useRefl)
            ApplySsrQuality(env, RtSettings.RtReflectionsQuality);

        // ── SDFGI (stands in for RTGI) ─────────────────────────────
        bool useGi = RtSettings.RayTracingEnabled && RtSettings.RtgiEnabled;
        env.SdfgiEnabled = useGi;
        if (useGi)
            ApplySdfgiQuality(env, RtSettings.RtgiQuality);

        EmitSignal(SignalName.GraphicsSettingsApplied);
        GD.Print("[Graphics] All graphics settings applied.");
    }

    // ───────────────────────────── Quality Mapping ──────────────────

    /// <summary>
    /// Maps the RTAO quality tier to SSAO parameters.
    /// </summary>
    private static void ApplySsaoQuality(Godot.Environment env, RayTracingSettings.RtQualityLevel level)
    {
        // Intensity and radius scale with quality.
        switch (level)
        {
            case RayTracingSettings.RtQualityLevel.Low:
                env.SsaoIntensity = 0.5f;
                env.SsaoRadius    = 0.5f;
                break;
            case RayTracingSettings.RtQualityLevel.Medium:
                env.SsaoIntensity = 1.0f;
                env.SsaoRadius    = 1.0f;
                break;
            case RayTracingSettings.RtQualityLevel.High:
                env.SsaoIntensity = 1.5f;
                env.SsaoRadius    = 1.5f;
                break;
            case RayTracingSettings.RtQualityLevel.Ultra:
                env.SsaoIntensity = 2.0f;
                env.SsaoRadius    = 2.0f;
                break;
        }
    }

    /// <summary>
    /// Maps the RT Reflections quality tier to SSR parameters.
    /// </summary>
    private static void ApplySsrQuality(Godot.Environment env, RayTracingSettings.RtQualityLevel level)
    {
        switch (level)
        {
            case RayTracingSettings.RtQualityLevel.Low:
                env.SsrMaxSteps = 32;
                break;
            case RayTracingSettings.RtQualityLevel.Medium:
                env.SsrMaxSteps = 64;
                break;
            case RayTracingSettings.RtQualityLevel.High:
                env.SsrMaxSteps = 128;
                break;
            case RayTracingSettings.RtQualityLevel.Ultra:
                env.SsrMaxSteps = 256;
                break;
        }
    }

    /// <summary>
    /// Maps the RTGI quality tier to SDFGI parameters.
    /// </summary>
    private static void ApplySdfgiQuality(Godot.Environment env, RayTracingSettings.RtQualityLevel level)
    {
        // SDFGI cascades and energy control fidelity vs. cost.
        switch (level)
        {
            case RayTracingSettings.RtQualityLevel.Low:
                env.SdfgiCascades  = 2;
                env.SdfgiEnergy    = 0.5f;
                break;
            case RayTracingSettings.RtQualityLevel.Medium:
                env.SdfgiCascades  = 4;
                env.SdfgiEnergy    = 1.0f;
                break;
            case RayTracingSettings.RtQualityLevel.High:
                env.SdfgiCascades  = 6;
                env.SdfgiEnergy    = 1.0f;
                break;
            case RayTracingSettings.RtQualityLevel.Ultra:
                env.SdfgiCascades  = 8;
                env.SdfgiEnergy    = 1.5f;
                break;
        }
    }

    // ───────────────────────────── Helpers ───────────────────────────

    /// <summary>
    /// Locates the <see cref="Godot.Environment"/> resource attached to
    /// the first <see cref="WorldEnvironment"/> in the scene tree,
    /// or the Camera3D's environment as fallback.
    /// </summary>
    private Godot.Environment GetViewportEnvironment()
    {
        // 1. Try WorldEnvironment node (preferred).
        var worldEnv = GetTree().Root.FindChild("WorldEnvironment", true, false) as WorldEnvironment;
        if (worldEnv?.Environment != null)
            return worldEnv.Environment;

        // 2. Fallback: active Camera3D.
        var cam = GetViewport().GetCamera3D();
        if (cam?.Environment != null)
            return cam.Environment;

        return null;
    }

    // ───────────────────────────── Public API ────────────────────────

    /// <summary>
    /// Toggles the master RT switch, validates, applies, and saves.
    /// </summary>
    public void SetRayTracingEnabled(bool enabled)
    {
        RtSettings.RayTracingEnabled = enabled;
        if (enabled) ValidateRayTracingSupport();
        ApplyGraphicsSettings();
        SaveGraphicsSettings();
    }

    /// <summary>
    /// Applies a uniform quality preset across all RT sub-effects, saves.
    /// </summary>
    public void SetGlobalQualityPreset(RayTracingSettings.RtQualityLevel level)
    {
        RtSettings.ApplyPreset(level);
        ApplyGraphicsSettings();
        SaveGraphicsSettings();
    }

    /// <summary>
    /// Overrides the entire settings object (e.g. from a settings UI),
    /// validates, applies, and saves.
    /// </summary>
    public void ApplyNewSettings(RayTracingSettings newSettings)
    {
        RtSettings = newSettings;
        ValidateRayTracingSupport();
        ApplyGraphicsSettings();
        SaveGraphicsSettings();
    }
}
