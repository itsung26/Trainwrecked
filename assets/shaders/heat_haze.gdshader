shader_type canvas_item;

uniform sampler2D noise_texture : source_color, repeat_enable;
uniform float strength : hint_range(0.0, 0.05, 0.001) = 0.005;
uniform vec2 speed = vec2(0.1, 0.05);
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;


void fragment() {
    vec2 noise_uv = UV + TIME * speed;

    vec2 offset = (texture(noise_texture, noise_uv).xy * 2.0 - 1.0);

    offset *= strength;
 vec2 distorted_uv = SCREEN_UV + offset;
    
    COLOR = texture(SCREEN_TEXTURE, distorted_uv);
}