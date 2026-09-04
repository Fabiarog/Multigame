shader_type canvas_item;
render_mode unshaded;

uniform sampler2D screen_texture : hint_screen_texture, repeat_disable, filter_nearest;
uniform int mode = 0;

void fragment() {
    vec4 source = texture(screen_texture, SCREEN_UV);
    mat3 transform = mat3(1.0);

    if (mode == 1) {
        transform = mat3(
            vec3(0.567, 0.433, 0.000),
            vec3(0.558, 0.442, 0.000),
            vec3(0.000, 0.242, 0.758)
        );
    } else if (mode == 2) {
        transform = mat3(
            vec3(0.625, 0.375, 0.000),
            vec3(0.700, 0.300, 0.000),
            vec3(0.000, 0.300, 0.700)
        );
    } else if (mode == 3) {
        transform = mat3(
            vec3(0.950, 0.050, 0.000),
            vec3(0.000, 0.433, 0.567),
            vec3(0.000, 0.475, 0.525)
        );
    }

    COLOR = vec4(clamp(transform * source.rgb, vec3(0.0), vec3(1.0)), source.a);
}
