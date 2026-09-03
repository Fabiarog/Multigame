shader_type canvas_item;

uniform vec3 chroma_color : source_color = vec3(0.0, 1.0, 0.0);
uniform float chroma_threshold : hint_range(0.0, 1.0) = 0.3;
uniform float chroma_smoothing : hint_range(0.0, 1.0) = 0.1;

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // Distance in RGB space
    float dist = distance(tex_color.rgb, chroma_color);
    
    // Smoothstep for anti-aliased edge
    float alpha_mask = smoothstep(chroma_threshold, chroma_threshold + chroma_smoothing, dist);
    
    COLOR = tex_color;
    COLOR.a *= alpha_mask;
}
