shader_type spatial;

#include "res://assets/shaders/transformers.gdshaderinc"

uniform float border_size = 0.0;
uniform float cross_thickness = 0.0;
uniform float cross_length = 0.0;
uniform float world_scale = 1.0;
uniform sampler2D wave_alpha_ramp;
uniform float wave_frequency = 1.0;
uniform float wave_speed = 1.0;
uniform float wave_trough : hint_range(0.0, 1.0) = 0.0;
uniform float wave_peak : hint_range(0.0, 1.0) = 1.0;
uniform sampler2D cross_pattern_color_ramp;
uniform float cross_pattern_brightness = 1.0;
uniform float wave_brightness = 1.0;
uniform vec3 wave_color : source_color = vec3(1.0);

// Draws a simple borders and cross texture (2D).
// Returns: 0.0 or 1.0
float sample_shape_function(vec2 uv) {
	float border_col = 0.0;
	if (uv.y < border_size || uv.y > 1.0 - border_size || uv.x < border_size || uv.x > 1.0 - border_size) {
		border_col = 1.0;
	}

	vec2 p = abs(uv - 0.5);
	float vertical = step(p.x, cross_thickness * 0.5) * step(p.y, cross_length * 0.5);
	float horizontal = step(p.y, cross_thickness * 0.5) * step(p.x, cross_length * 0.5);
	float cross_col = max(vertical, horizontal);

	return max(border_col, cross_col);
}

// Returns: [0.0, 1.0]
float sample_triplanar_mapping(vec3 world_pos_cell, vec3 world_dir) {
    float xz_sample = sample_shape_function(world_pos_cell.xz);
    float yz_sample = sample_shape_function(world_pos_cell.yz);
    float xy_sample = sample_shape_function(world_pos_cell.xy);

    vec3 weights = abs(normalize(world_dir));
    weights /= max(weights.x + weights.y + weights.z, 1e-5);

    return (
        yz_sample * weights.x +
        xz_sample * weights.y +
        xy_sample * weights.z
    );
}

// Returns: rgb in [0.0, 1.0]
float sample_wave_alpha_ramp(float fac) {
	return texture(wave_alpha_ramp, vec2(fac, 0.5)).r;
}

// Returns: rgb in [0.0, 1.0]
vec3 sample_cross_pattern_color_ramp(float fac) {
	return texture(cross_pattern_color_ramp, vec2(fac, 0.5)).rgb;
}

// Returns: [wave_trough, wave_peak] within [0.0, 1.0] when trough/peak are in range
float sample_wave_function(vec3 world_pos) {
	float phase = (world_pos.z * wave_frequency) + (TIME * wave_speed);
	float t = sin(phase) * 0.5 + 0.5;
	return mix(wave_trough, wave_peak, t);
}



void fragment() {
	vec3 WORLD_POS = view_to_world(VERTEX, INV_VIEW_MATRIX, false) * world_scale;
	vec3 WORLD_POS_CELL = fract(WORLD_POS);
	vec3 WORLD_DIR = view_to_world(NORMAL, INV_VIEW_MATRIX, true);
	
	// Compute color.
	float cross_mask = sample_triplanar_mapping(WORLD_POS_CELL, WORLD_DIR);
	vec3 wave_col = sample_wave_function(WORLD_POS) * wave_color * wave_brightness;
	vec3 cross_col = sample_cross_pattern_color_ramp(cross_mask) * cross_pattern_brightness;
	vec3 final_col = mix(wave_col, cross_col, cross_mask);
	
	// Compute alpha.
	float wave_fac = sample_wave_alpha_ramp(sample_wave_function(WORLD_POS));
	float cross_fac = sample_triplanar_mapping(WORLD_POS_CELL, WORLD_DIR);
	float final_alpha = max(wave_fac, cross_fac);

	ALPHA = final_alpha;
	ALBEDO = vec3(0.0);
	EMISSION = final_col;
}
