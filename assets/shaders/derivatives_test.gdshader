shader_type spatial;

uniform sampler2D tex;
uniform float scale = 1.0;
uniform vec2 offset;
uniform float back_layer_brightness = 0.0;
uniform float normal_strength = 1.0;

//float sample_color_ramp(float fac) {
	//float color = texture(texture_color_ramp, vec2(fac, 0.5)).r;
	//color = clamp(color, 0.0, 1.0);
	//return color;
//}

float sample_texture(vec2 uv) {
	float tex_col = texture(tex, uv * scale + offset).r;
	tex_col += back_layer_brightness;
	tex_col = clamp(tex_col, 0.0, 1.0);
	return tex_col;
}

vec3 construct_normal_vector(float up_sample, float down_sample, float left_sample, float right_sample) {
	float bitangent = (right_sample - left_sample) / 2.0; // TODO: multiply 2 by delta x in pixel distance
	float tangent = (up_sample - down_sample) / 2.0; // TODO: multiply 2 by delta y in pixel distance

	vec3 normal = vec3(bitangent, tangent, 1.0) * 0.5 + 0.5;
	return normalize(normal);
}

void fragment() {
	float EPSILON = 1.0 / float(textureSize(tex, 0).x);
	vec3 rebuilt_normal = construct_normal_vector(
		sample_texture(UV + vec2(0.0, EPSILON)),
		sample_texture(UV - vec2(0.0, EPSILON)),
		sample_texture(UV + vec2(EPSILON, 0.0)),
		sample_texture(UV - vec2(EPSILON, 0.0))
	);
	
	//ALBEDO = vec3(0.0);
	ALBEDO = vec3(0.5, 0.5, 0.0);
	NORMAL_MAP = vec3(rebuilt_normal.x * normal_strength, rebuilt_normal.y * normal_strength, rebuilt_normal.z);
	//EMISSION = vec3(rebuilt_normal.x * normal_strength, rebuilt_normal.y * normal_strength, rebuilt_normal.z);
}
