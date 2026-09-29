shader_type spatial;

uniform float band_width;
uniform float band_spacing;
uniform vec3 band_color:source_color = vec3(1.0);
uniform sampler2D underlying_texture;
uniform vec2 uv_scale = vec2(1.0);
uniform vec2 uv_offset = vec2(0.0);

#include "res://assets/shaders/transformers.gdshaderinc"

// Returns a 0..1 mask for contour lines at multiples of band_spacing.
// band_width is half-thickness in the same units as value (world height).
float sample_stepping_function(float value) {
	float spacing = max(band_spacing, 1e-5);
	float m = mod(value, spacing);
	// Distance to nearest contour plane (0 on the line, peaks at spacing/2).
	float dist = min(m, spacing - m);

	// Screen-stable AA: thicken by at least ~1 pixel of height change.
	float aa = fwidth(value);
	float half_width = max(band_width, aa);
	return 1.0 - smoothstep(0.0, half_width, dist);
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	float fragment_height = distance(view_to_world(VERTEX, INV_VIEW_MATRIX, false).y, NODE_POSITION_WORLD.y);
	vec3 tex_col = texture(underlying_texture, (UV * uv_scale) + uv_offset).rgb;
	
	ALBEDO = vec3(mix(tex_col, band_color, sample_stepping_function(fragment_height)));
	EMISSION = vec3(0.0);
}
