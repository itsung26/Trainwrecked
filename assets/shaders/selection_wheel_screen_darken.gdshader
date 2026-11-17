shader_type canvas_item;

float sample_falloff_function(vec2 uv) {
	return distance(uv, vec2(0.5, 0.5));
}

void fragment() {
	
	vec3 color = vec3(0.0);
	float alpha = sample_falloff_function(UV);
	COLOR = vec4(vec3(color), alpha);
}
