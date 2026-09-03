shader_type spatial;
render_mode unshaded, depth_draw_opaque, cull_disabled;

uniform sampler2D sprite_texture : source_color, filter_nearest;
uniform vec3 chroma_color : source_color = vec3(0.0, 1.0, 0.0);
uniform float chroma_threshold : hint_range(0.0, 1.0) = 0.3;
uniform float chroma_smoothing : hint_range(0.0, 1.0) = 0.1;
uniform int hframes = 1;
uniform int vframes = 1;
uniform int frame = 0;

void fragment() {
    // Calculate sprite frame UVs
    vec2 frame_size = vec2(1.0 / float(hframes), 1.0 / float(vframes));
    float x = float(frame % hframes) * frame_size.x;
    float y = float(frame / hframes) * frame_size.y;
    vec2 uv = vec2(x, y) + UV * frame_size;

    vec4 tex_color = texture(sprite_texture, uv);
    
    // Distance in RGB space
    float dist = distance(tex_color.rgb, chroma_color);
    
    // Smoothstep for anti-aliased edge
    float alpha = smoothstep(chroma_threshold, chroma_threshold + chroma_smoothing, dist);
    
    ALBEDO = tex_color.rgb;
    ALPHA = alpha;
    
    if (ALPHA < 0.1) {
        discard;
    }
}
