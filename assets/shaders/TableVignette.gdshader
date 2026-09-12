shader_type canvas_item;

// Cinematic post-processing for the 3D table viewport:
// vignette darkening, subtle film grain, and warm color grading.

uniform float vignette_radius : hint_range(0.3, 1.5) = 0.72;
uniform float vignette_softness : hint_range(0.1, 1.0) = 0.42;
uniform float vignette_intensity : hint_range(0.0, 1.0) = 0.35;
uniform float grain_intensity : hint_range(0.0, 0.1) = 0.018;
uniform float warmth : hint_range(0.0, 0.15) = 0.04;
uniform float contrast_boost : hint_range(0.9, 1.2) = 1.05;

// Fast pseudo-random for grain
float rand(vec2 co) {
    return fract(sin(dot(co, vec2(12.9898, 78.233))) * 43758.5453);
}

void fragment() {
    vec4 tex = texture(TEXTURE, UV);
    vec3 color = tex.rgb;

    // --- Contrast boost (subtle S-curve) ---
    color = (color - 0.5) * contrast_boost + 0.5;

    // --- Warm color grading: push shadows toward amber, highlights toward cream ---
    color.r += warmth * (1.0 - color.r) * 0.6;
    color.g += warmth * 0.15;
    color.b -= warmth * 0.4 * (1.0 - color.b);

    // --- Film grain ---
    float grain = (rand(UV * 847.3 + TIME * 13.7) - 0.5) * grain_intensity;
    color += vec3(grain);

    // --- Vignette ---
    vec2 center = UV - 0.5;
    float dist = length(center * vec2(1.0, 0.75)); // wider horizontally
    float vig = 1.0 - smoothstep(vignette_radius - vignette_softness, vignette_radius, dist);
    color *= mix(1.0 - vignette_intensity, 1.0, vig);

    // Clamp to valid range
    COLOR = vec4(clamp(color, 0.0, 1.0), tex.a);
}
