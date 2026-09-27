shader_type spatial;

uniform sampler2D texture_1;
uniform sampler2D texture_2;
uniform float texture_scale = 1.0;
uniform float p1 = 1.0;
uniform float p2 = 0.5;
uniform float p3 = 1.0;

// Height along the surface normal, sampled in tangent-space (T, B) coordinates.
float sample_height_function(vec2 tangent_pos) {
	float x = abs(tangent_pos.x);

	if (x >= p1) {
		return 0.0;
	}
	if (x <= p2) {
		return p3;
	}
	// p2 < abs(x) < p1
	return p3 * (p1 - x) / (p1 - p2);
}

// Returns a view-space direction transformed into world space.
vec3 view_to_world(vec3 dir, mat4 inv_view_matrix) {
	return normalize(mat3(inv_view_matrix) * dir);
}

// Dual-texture triplanar: texture_1 on ±Y, texture_2 on ±X and ±Z.
vec3 sample_triplanar_mapping_function(vec3 world_pos, vec3 world_dir) {
	vec3 up_down_sample = texture(texture_1, world_pos.xz * texture_scale).rgb;
	vec3 left_right_sample = texture(texture_2, world_pos.yz * texture_scale).rgb;
	vec3 forward_back_sample = texture(texture_2, world_pos.xy * texture_scale).rgb;

	float forward_back_fac = abs(dot(world_dir, vec3(0.0, 0.0, 1.0)));
    float left_right_fac = abs(dot(world_dir, vec3(1.0, 0.0, 0.0)));
    float up_down_fac = abs(dot(world_dir, vec3(0.0, 1.0, 0.0)));

	return (left_right_sample * left_right_fac) + (up_down_sample * up_down_fac) + (forward_back_sample * forward_back_fac);
}

// Model-space position → tangent-space (T, B) coordinates.
vec2 model_to_tangent(vec3 model_pos, vec3 tangent, vec3 binormal) {
	return vec2(dot(model_pos, tangent), dot(model_pos, binormal));
}

// Tangent-space normal from central differences of the height function.
// Frame: X = T, Y = N, Z = B.
vec3 calculate_normal_tangent(vec2 tangent_pos, float epsilon) {
	float h_l = sample_height_function(tangent_pos + vec2(-epsilon, 0.0));
	float h_r = sample_height_function(tangent_pos + vec2(epsilon, 0.0));
	float h_d = sample_height_function(tangent_pos + vec2(0.0, -epsilon));
	float h_u = sample_height_function(tangent_pos + vec2(0.0, epsilon));

	vec3 along_t = vec3(2.0 * epsilon, h_r - h_l, 0.0);
	vec3 along_b = vec3(0.0, h_u - h_d, 2.0 * epsilon);
	return normalize(cross(along_b, along_t));
}

// Tangent-space normal → model space via TBN.
vec3 calculate_normal_model(vec2 tangent_pos, float epsilon, vec3 tangent, vec3 normal, vec3 binormal) {
	vec3 n_ts = calculate_normal_tangent(tangent_pos, epsilon);
	mat3 tbn = mat3(normalize(tangent), normalize(normal), normalize(binormal));
	return normalize(tbn * n_ts);
}

void vertex() {
	vec3 tangent = normalize(TANGENT);
	vec3 normal = normalize(NORMAL);
	vec3 binormal = normalize(BINORMAL);

	vec2 tangent_pos = model_to_tangent(VERTEX, tangent, binormal);

	VERTEX += normal * sample_height_function(tangent_pos);

	NORMAL = calculate_normal_model(tangent_pos, 0.05, tangent, normal, binormal);
}

void fragment() {
	vec3 world_pos = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;
	vec3 NORMAL_WORLD = view_to_world(NORMAL, INV_VIEW_MATRIX);

	ALBEDO = sample_triplanar_mapping_function(world_pos, NORMAL_WORLD);
}
