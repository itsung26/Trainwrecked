shader_type spatial;

void vertex() {
	POSITION = vec4(VERTEX, 1.0);
}

void fragment() {
	ALBEDO = vec3(1.0, 0.0, 0.0);
}
