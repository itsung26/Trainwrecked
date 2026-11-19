shader_type canvas_item;

uniform vec3 base_color : source_color;
uniform float wave_size = 1.0;
uniform float wave_amplitude = 0.0;
uniform int wave_period = 0;
uniform float wave_phase = 0.0;
uniform float uv_scale : hint_range(0.0, 4.0) = 1.0;
uniform vec2 uv_offset;
uniform float line_thickness = 0.1;
uniform sampler2D line_thickness_falloff_ramp : repeat_enable;

float sample_falloff_ramp(float x) {
	float col = texture(line_thickness_falloff_ramp, vec2(x, 0.5)).r;
	return col;
}

vec2 to_polar(vec2 xy) {
	return vec2(length(xy), atan(xy.y, xy.x));
}

vec2 to_rectangular(float radius, float theta) {
	return vec2(radius * cos(theta), radius * sin(theta));
}

float sample_polar_func(float radius, float theta) {
	vec2 rect_coord = to_rectangular(radius, theta);
	float r = wave_size + wave_amplitude * cos(float(wave_period) * theta + wave_phase);
	float r_inverted = 1.0 - distance(to_rectangular(r, theta), rect_coord);
	float final_col = sample_falloff_ramp(r_inverted);
	return final_col;
}

void fragment() {
	// UV unwrap is [0,1] with (0.5, 0.5) at center. *2 maps the circle rim to radius 1.
	vec2 UV_CENTERED = ((UV - vec2(0.5)) * 2.0 * uv_scale) + uv_offset;
	vec2 UV_POLAR = to_polar(UV_CENTERED);
	float wave = sample_polar_func(UV_POLAR.x, UV_POLAR.y);

	vec3 COLOR_FINAL = vec3(sample_polar_func(UV_POLAR.x, UV_POLAR.y));
	COLOR = vec4(COLOR_FINAL, 1.0);
}
