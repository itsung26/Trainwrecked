shader_type spatial; render_mode cull_disabled;

uniform vec3 color:source_color = vec3(0.0);
uniform float wing_flap_speed = 1.0;
uniform float wing_flap_range = 1.0;
const float dist_threshold = 0.025;

// Implementation of Rodrigues' algorithim for vector rotation in space.
// Returns a vector3 rotated around axis by degrees.
vec3 rotate_vector(vec3 vector, vec3 axis, float dgrs) {
	float rdns = radians(dgrs);
	vec3 k = normalize(axis);
	float cos_theta = cos(rdns);
	float sin_theta = sin(rdns);
	return vector * cos_theta
		+ cross(k, vector) * sin_theta
		+ k * dot(k, vector) * (1.0 - cos_theta);
}

vec3 sample_flapping_function(vec3 vertex, bool invert) {
	float oscillating_fac = wing_flap_range * sin(TIME * wing_flap_speed);
	if (invert) {
		return rotate_vector(vertex, vec3(0.0, 0.0, 1.0), -oscillating_fac);
	} else {
		return rotate_vector(vertex, vec3(0.0, 0.0, 1.0), oscillating_fac);
	}
}

float magnitude(vec3 vector){
	return sqrt(pow(vector.x, 2.0) + pow(vector.y, 2.0) + pow(vector.z, 2.0));
}

void vertex() {
	vec3 old_vertex = VERTEX;
	// If this vertex is to the left of the origin by threshold.
	if (VERTEX.x < -dist_threshold) {
		VERTEX = sample_flapping_function(VERTEX, false);
	} else if (VERTEX.x > dist_threshold) {
		VERTEX = sample_flapping_function(VERTEX, true);
	}
}

void fragment() {
	ALBEDO = color;
	METALLIC = 0.0;
	ROUGHNESS = 1.0;
}
