shader_type spatial;
render_mode unshaded, depth_draw_opaque, cull_disabled;

uniform sampler2D sprite_texture : source_color, filter_nearest;
uniform vec3 chroma_color : source_color = vec3(0.0, 1.0, 0.0);
uniform float chroma_threshold : hint_range(0.0, 1.0) = 0.4;
uniform float chroma_smoothing : hint_range(0.0, 1.0) = 0.15;

void fragment() {
    // Sample the texture at the UV provided by Sprite3D
    // Sprite3D already handles hframes/vframes/frame slicing for us
    vec4 tex_color = texture(sprite_texture, UV);

    // Distance in RGB space from chroma key color
    float dist = distance(tex_color.rgb, chroma_color);

    // Smoothstep for anti-aliased edge
    float alpha = smoothstep(chroma_threshold, chroma_threshold + chroma_smoothing, dist);

    ALBEDO = tex_color.rgb;
    ALPHA = alpha;

    if (ALPHA < 0.05) {
        discard;
    }
}
