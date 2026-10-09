shader_type spatial; render_mode unshaded;

uniform sampler2D VIEWPORT_SCREEN_TEXTURE : source_color;

void vertex() {
	// Called for every vertex the material is visible on.
}

void fragment() {
	vec3 texture_col = texture(VIEWPORT_SCREEN_TEXTURE, SCREEN_UV).rgb;
	ALBEDO = vec3(texture_col);
}