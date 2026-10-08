shader_type spatial;

uniform sampler2D VIEWPORT_SCREEN_TEXTURE;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec3 text_col = texture(VIEWPORT_SCREEN_TEXTURE, SCREEN_UV).rgb;
	ALBEDO = text_col;
}