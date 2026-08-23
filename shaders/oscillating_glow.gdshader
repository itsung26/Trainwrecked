shader_type spatial;

uniform float frequency;
uniform vec3 color:source_color = vec3(0.0);
uniform vec3 color_2:source_color = vec3(1.0);

float sample_sine_function(float x) {
	return sin(x * frequency);
}

// Expects input values ranging from 0.0 - 1.0.
vec3 sample_color_function(float x) {
	return mix(color, color_2, x);
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	ALBEDO = vec3(0.0);
	EMISSION = sample_color_function(sample_sine_function(TIME));
}
