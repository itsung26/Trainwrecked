shader_type spatial;

uniform sampler2D texture_1;
uniform sampler2D texture_2;
uniform sampler2D texture_3;
uniform float texture_scale = 1.0;
// Higher = that axis dominates sooner / sharper plane transitions.
uniform float xz_sharpness = 4.0; // ±Y faces (top/bottom)
uniform float yz_sharpness = 4.0; // ±X faces (left/right)
uniform float xy_sharpness = 4.0; // ±Z faces (forward/back)

vec3 view_to_world(vec3 xyz, mat4 inv_view_matrix, bool direction) {
	if (direction) {
		return normalize(mat3(inv_view_matrix) * xyz);
	}
	return (inv_view_matrix * vec4(xyz, 1.0)).xyz;
}

vec3 sample_triplanar_mapping(vec3 world_pos, vec3 world_dir) {
	vec3 xz_sample = texture(texture_1, world_pos.xz * texture_scale).rgb;
	vec3 yz_sample = texture(texture_2, world_pos.yz * texture_scale).rgb;
	vec3 xy_sample = texture(texture_3, world_pos.xy * texture_scale).rgb;

	vec3 n = abs(normalize(world_dir));
	float yz_fac = pow(n.x, yz_sharpness);
	float xz_fac = pow(n.y, xz_sharpness);
	float xy_fac = pow(n.z, xy_sharpness);
	float sum = max(yz_fac + xz_fac + xy_fac, 1e-5);

	return (yz_sample * yz_fac + xz_sample * xz_fac + xy_sample * xy_fac) / sum;
}

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec3 VERTEX_WORLD = view_to_world(VERTEX, INV_VIEW_MATRIX, false);
	vec3 NORMAL_WORLD = view_to_world(NORMAL, INV_VIEW_MATRIX, true);
	
	ALBEDO = vec3(0.0);
	EMISSION = vec3(sample_triplanar_mapping(VERTEX_WORLD, NORMAL_WORLD));
	//EMISSION = vec3(abs(dot(NORMAL_WORLD, vec3(0.0, 1.0, 0.0))));
}
