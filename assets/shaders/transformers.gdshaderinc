// Transforms a view-space vector into world space.
// When is_direction is true, treats xyz as a direction (no translation, normalized).
// When false, treats xyz as a position (full affine transform).
vec3 view_to_world(vec3 xyz, mat4 inv_view_matrix, bool is_direction) {
	if (is_direction) {
		return normalize(mat3(inv_view_matrix) * xyz);
	}
	return (inv_view_matrix * vec4(xyz, 1.0)).xyz;
}

// Transforms a local/model-space vector into world space.
// When is_direction is true, treats xyz as a direction (no translation, normalized).
// When false, treats xyz as a position (full affine transform).
// Pass MODEL_MATRIX from the spatial shader.
vec3 local_to_world(vec3 xyz, mat4 model_matrix, bool is_direction) {
	if (is_direction) {
		return normalize(mat3(model_matrix) * xyz);
	}
	return (model_matrix * vec4(xyz, 1.0)).xyz;
}
