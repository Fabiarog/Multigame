namespace GameHub.Core.Graphics;

/// <summary>
/// Pure data model describing the current ray-tracing configuration.
/// Has no engine dependencies — the <see cref="GraphicsQualityManager"/>
/// reads this object and translates it into RenderingServer calls.
/// </summary>
public class RayTracingSettings
{
    // ───────────────────────────── Quality Enum ──────────────────────

    /// <summary>
    /// Quality tiers for individual RT effects.
    /// Each tier maps to concrete parameters (ray count, resolution,
    /// bounce count) defined by <see cref="GraphicsQualityManager"/>.
    /// </summary>
    public enum RtQualityLevel
    {
        Low    = 0,
        Medium = 1,
        High   = 2,
        Ultra  = 3,
    }

    // ───────────────────────────── Master Toggle ─────────────────────

    /// <summary>
    /// Global on/off switch for all ray-tracing effects.
    /// When false, the engine falls back to purely rasterized rendering.
    /// </summary>
    public bool RayTracingEnabled { get; set; } = false;

    // ───────────────────────────── RTAO ──────────────────────────────

    /// <summary>Ray-traced Ambient Occlusion toggle.</summary>
    public bool RtaoEnabled { get; set; } = false;

    /// <summary>Quality tier for RTAO.</summary>
    public RtQualityLevel RtaoQuality { get; set; } = RtQualityLevel.Medium;

    // ───────────────────────────── Reflections ───────────────────────

    /// <summary>Ray-traced Reflections toggle.</summary>
    public bool RtReflectionsEnabled { get; set; } = false;

    /// <summary>Quality tier for reflections.</summary>
    public RtQualityLevel RtReflectionsQuality { get; set; } = RtQualityLevel.Medium;

    // ───────────────────────────── RTGI ──────────────────────────────

    /// <summary>Ray-traced Global Illumination toggle.</summary>
    public bool RtgiEnabled { get; set; } = false;

    /// <summary>Quality tier for GI.</summary>
    public RtQualityLevel RtgiQuality { get; set; } = RtQualityLevel.Medium;

    // ───────────────────────────── Presets ───────────────────────────

    /// <summary>
    /// Applies a uniform quality preset across all sub-effects.
    /// Individual toggles remain unchanged — only quality tiers are set.
    /// </summary>
    public void ApplyPreset(RtQualityLevel level)
    {
        RtaoQuality          = level;
        RtReflectionsQuality = level;
        RtgiQuality          = level;
    }

    /// <summary>
    /// Enables all RT sub-effects at the given quality level.
    /// Convenience for "turn everything on at Ultra" scenarios.
    /// </summary>
    public void EnableAll(RtQualityLevel level)
    {
        RayTracingEnabled    = true;
        RtaoEnabled          = true;
        RtReflectionsEnabled = true;
        RtgiEnabled          = true;
        ApplyPreset(level);
    }

    /// <summary>
    /// Disables all RT effects and resets to defaults.
    /// </summary>
    public void DisableAll()
    {
        RayTracingEnabled    = false;
        RtaoEnabled          = false;
        RtReflectionsEnabled = false;
        RtgiEnabled          = false;
        ApplyPreset(RtQualityLevel.Medium);
    }

    /// <summary>
    /// Returns true if at least one sub-effect is both enabled
    /// and the master toggle is on.
    /// </summary>
    public bool AnyEffectActive()
    {
        if (!RayTracingEnabled) return false;
        return RtaoEnabled || RtReflectionsEnabled || RtgiEnabled;
    }

    /// <summary>
    /// Creates a deep copy of this settings object.
    /// Useful for preview / revert workflows in a settings UI.
    /// </summary>
    public RayTracingSettings Clone()
    {
        return new RayTracingSettings
        {
            RayTracingEnabled    = RayTracingEnabled,
            RtaoEnabled          = RtaoEnabled,
            RtaoQuality          = RtaoQuality,
            RtReflectionsEnabled = RtReflectionsEnabled,
            RtReflectionsQuality = RtReflectionsQuality,
            RtgiEnabled          = RtgiEnabled,
            RtgiQuality          = RtgiQuality,
        };
    }
}
