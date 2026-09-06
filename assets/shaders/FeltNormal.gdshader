shader_type spatial;
render_mode cull_back;

// Procedural felt fabric with fibrous normal map and worn roughness variation.
// Used on the table's green playing surface.

uniform vec4 felt_color : source_color = vec4(0.086, 0.329, 0.239, 1.0);
uniform float roughness_base : hint_range(0.5, 1.0) = 0.82;
uniform float roughness_wear : hint_range(0.0, 0.3) = 0.08;
uniform float normal_strength : hint_range(0.0, 2.0) = 0.45;
uniform float fiber_scale : hint_range(1.0, 80.0) = 38.0;
uniform float wear_scale : hint_range(0.5, 20.0) = 6.0;

// Simple hash-based noise (no external texture required)
float hash21(vec2 p) {
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float value_noise(vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);
    f = f * f * (3.0 - 2.0 * f); // smoothstep
    float a = hash21(i);
    float b = hash21(i + vec2(1.0, 0.0));
    float c = hash21(i + vec2(0.0, 1.0));
    float d = hash21(i + vec2(1.0, 1.0));
    return mix(mix(a, b, f.x), mix(c, d, f.x), f.y);
}

float fbm(vec2 p, int octaves) {
    float value = 0.0;
    float amplitude = 0.5;
    float frequency = 1.0;
    for (int i = 0; i < octaves; i++) {
        value += amplitude * value_noise(p * frequency);
        frequency *= 2.17;
        amplitude *= 0.48;
    }
    return value;
}

void fragment() {
    vec2 uv = UV * fiber_scale;

    // Fibrous normal map: two directional noise layers simulate warp and weft threads
    float warp = fbm(uv * vec2(1.0, 3.2), 4);
    float weft = fbm(uv * vec2(3.2, 1.0) + 73.1, 4);
    float fiber = (warp + weft) * 0.5;

    // Compute normal from height field via partial derivatives
    float eps = 0.8;
    float h_center = fiber;
    float h_right  = (fbm((uv + vec2(eps, 0.0)) * vec2(1.0, 3.2), 4)
                     + fbm((uv + vec2(eps, 0.0)) * vec2(3.2, 1.0) + 73.1, 4)) * 0.5;
    float h_up     = (fbm((uv + vec2(0.0, eps)) * vec2(1.0, 3.2), 4)
                     + fbm((uv + vec2(0.0, eps)) * vec2(3.2, 1.0) + 73.1, 4)) * 0.5;

    vec3 normal_map;
    normal_map.x = (h_center - h_right) * normal_strength;
    normal_map.y = (h_center - h_up) * normal_strength;
    normal_map.z = 1.0;
    normal_map = normalize(normal_map);

    // Worn areas: large-scale noise makes some zones shinier (pressed flat by cards/elbows)
    float wear = fbm(UV * wear_scale, 3);
    float roughness = roughness_base - roughness_wear * smoothstep(0.35, 0.7, wear);

    // Subtle color variation to break uniformity
    vec3 color = felt_color.rgb * (0.95 + 0.10 * fbm(UV * 12.0, 2));

    ALBEDO = color;
    ROUGHNESS = roughness;
    NORMAL_MAP = normal_map * 0.5 + 0.5;
    NORMAL_MAP_DEPTH = normal_strength;
    METALLIC = 0.0;
}
