using Godot;
using System.Text.RegularExpressions;

namespace GameHub.Core.Graphics;

/// <summary>
/// Probes the GPU and rendering backend at runtime to determine
/// hardware capabilities, especially ray-tracing readiness.
///
/// Detection heuristics are based on known GPU architecture families:
///   • NVIDIA Turing (RTX 20xx) and newer → DXR / Vulkan RT capable
///   • AMD RDNA 2 (RX 6xxx) and newer     → Vulkan RT capable
///   • Intel Arc (A-series)                → Vulkan RT capable
///
/// Because Godot 4.7 does not yet expose a native hardware RT API,
/// this class prepares the ground for future engine support while
/// gating the current screen-space effect toggles.
/// </summary>
public static class HardwareCapabilityDetector
{
    // ───────────────────────────── Public State ──────────────────────

    /// <summary>Full adapter name reported by the driver.</summary>
    public static string GpuName { get; private set; } = "Unknown";

    /// <summary>Graphics API version string (e.g. "Vulkan 1.3.274").</summary>
    public static string ApiVersion { get; private set; } = "Unknown";

    /// <summary>Active rendering method: "Forward+" or "Compatibility".</summary>
    public static string CurrentRenderer { get; private set; } = "Unknown";

    /// <summary>
    /// Estimated VRAM in megabytes.  Godot does not expose a direct query,
    /// so we fall back to a conservative default when unavailable.
    /// </summary>
    public static int EstimatedVramMb { get; private set; } = 0;

    /// <summary>
    /// Whether the detected GPU is likely to support hardware ray tracing
    /// (DXR 1.1 / Vulkan Ray Tracing).  This is a best-effort heuristic.
    /// </summary>
    public static bool SupportsRayTracing { get; private set; } = false;

    /// <summary>
    /// True when the active renderer is Forward+, which is required for
    /// advanced effects like SDFGI, SSR, and VoxelGI.
    /// </summary>
    public static bool IsForwardPlusRenderer { get; private set; } = false;

    /// <summary>True after <see cref="Detect"/> has been called at least once.</summary>
    public static bool HasProbed { get; private set; } = false;

    // ───────────────────────────── Detection ─────────────────────────

    /// <summary>
    /// Runs all hardware probes.  Safe to call more than once (idempotent).
    /// Should be called early — typically in a graphics manager's _Ready().
    /// </summary>
    public static void Detect()
    {
        GpuName         = RenderingServer.GetVideoAdapterName();
        ApiVersion      = RenderingServer.GetVideoAdapterApiVersion();
        CurrentRenderer = DetermineRenderer();
        IsForwardPlusRenderer = CurrentRenderer.Contains("Forward");
        EstimatedVramMb = EstimateVram();
        SupportsRayTracing = EvaluateRayTracingSupport();
        HasProbed = true;

        GD.Print($"[HW] GPU: {GpuName}");
        GD.Print($"[HW] API: {ApiVersion}");
        GD.Print($"[HW] Renderer: {CurrentRenderer}");
        GD.Print($"[HW] Est. VRAM: {EstimatedVramMb} MB");
        GD.Print($"[HW] RT support: {SupportsRayTracing}");
    }

    // ───────────────────────────── Internals ─────────────────────────

    /// <summary>
    /// Determines the current rendering method by inspecting the
    /// ProjectSettings rendering method string.
    /// </summary>
    private static string DetermineRenderer()
    {
        // Godot exposes the configured method; at runtime the actual
        // method may differ if the driver doesn't support it.
        var method = ProjectSettings.GetSetting("rendering/renderer/rendering_method").AsString();
        return string.IsNullOrEmpty(method) ? "Unknown" : method;
    }

    /// <summary>
    /// Attempts to estimate VRAM from the adapter name (rough heuristic).
    /// Returns 0 when the name doesn't contain a recognizable pattern.
    /// </summary>
    private static int EstimateVram()
    {
        // Many drivers include VRAM in the adapter string.
        // Example: "NVIDIA GeForce RTX 3070 (8192 MB)"
        var match = Regex.Match(GpuName, @"(\d{3,6})\s*MB", RegexOptions.IgnoreCase);
        if (match.Success && int.TryParse(match.Groups[1].Value, out int mb))
            return mb;

        // Fallback: assume mid-range.
        return 0;
    }

    /// <summary>
    /// Heuristic evaluation of hardware ray-tracing capability.
    /// Checks the GPU name against known architecture families.
    /// </summary>
    private static bool EvaluateRayTracingSupport()
    {
        if (string.IsNullOrEmpty(GpuName))
            return false;

        string upper = GpuName.ToUpperInvariant();

        // ── NVIDIA ──────────────────────────────────────────────────
        // RTX series (Turing / Ampere / Ada Lovelace / Blackwell)
        if (upper.Contains("RTX"))
            return true;

        // ── AMD ─────────────────────────────────────────────────────
        // RDNA 2+ (RX 6xxx, RX 7xxx, RX 9xxx)
        if (Regex.IsMatch(upper, @"RX\s*(6\d{3}|7\d{3}|9\d{3})"))
            return true;

        // ── Intel ───────────────────────────────────────────────────
        // Arc A-series (A310, A380, A580, A750, A770 …)
        if (Regex.IsMatch(upper, @"ARC\s*A\d{3}"))
            return true;

        return false;
    }

    // ───────────────────────────── Utility ───────────────────────────

    /// <summary>
    /// Returns a human-readable summary of detected capabilities,
    /// suitable for a diagnostics overlay or log dump.
    /// </summary>
    public static string GetSummary()
    {
        return
            $"GPU: {GpuName}\n" +
            $"API: {ApiVersion}\n" +
            $"Renderer: {CurrentRenderer}\n" +
            $"VRAM (est.): {(EstimatedVramMb > 0 ? $"{EstimatedVramMb} MB" : "unknown")}\n" +
            $"RT capable: {SupportsRayTracing}\n" +
            $"Forward+: {IsForwardPlusRenderer}";
    }
}
