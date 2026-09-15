shader_type particles;

// General radius of the rings.
uniform float primary_ring_radius : hint_range(0.0, 100.0, 0.1) = 5.0;
// How much rings can differ from the primary radius in their own radius.
uniform float radius_spread : hint_range(0.0, 50.0, 0.1) = 1.0;
// Base angular speed in radians per second.
uniform float orbit_speed : hint_range(0.0, 10.0, 0.01) = 0.5;
// Per-particle angular speed variation.
uniform float speed_spread : hint_range(0.0, 5.0, 0.01) = 0.1;
// Vertical spread of ring heights.
uniform float height_spread : hint_range(0.0, 50.0, 0.1) = 0.0;
// Chance a particle orbits the opposite direction (0–1).
uniform float reverse_chance : hint_range(0.0, 1.0, 0.01) = 0.0;

float rand_from_seed(inout uint seed) {
	int k = int(seed);
	int s = 214013 * k + 2531011;
	seed = uint(s);
	return float((s >> 16) & 0x7FFF) / 32767.0;
}

void start() {
	uint seed = NUMBER + RANDOM_SEED;

	float radius = primary_ring_radius + (rand_from_seed(seed) * 2.0 - 1.0) * radius_spread;
	float phase = rand_from_seed(seed) * TAU;
	float speed = orbit_speed + (rand_from_seed(seed) * 2.0 - 1.0) * speed_spread;
	if (rand_from_seed(seed) < reverse_chance) {
		speed = -speed;
	}
	float height = (rand_from_seed(seed) * 2.0 - 1.0) * height_spread;

	CUSTOM = vec4(max(radius, 0.0), phase, speed, height);
	VELOCITY = vec3(0.0);

	float angle = CUSTOM.y;
	TRANSFORM[3].xyz = vec3(cos(angle) * CUSTOM.x, CUSTOM.w, sin(angle) * CUSTOM.x);
}

void process() {
	float angle = CUSTOM.y + TIME * CUSTOM.z;
	float c = cos(angle);
	float s = sin(angle);

	TRANSFORM[3].xyz = vec3(c * CUSTOM.x, CUSTOM.w, s * CUSTOM.x);

	// Face along the orbit tangent (mesh -Z forward).
	float dir = sign(CUSTOM.z);
	if (dir == 0.0) {
		dir = 1.0;
	}
	vec3 forward = normalize(vec3(-s * dir, 0.0, c * dir));
	vec3 up = vec3(0.0, 1.0, 0.0);
	vec3 basis_z = -forward;
	vec3 basis_x = normalize(cross(up, basis_z));
	vec3 basis_y = cross(basis_z, basis_x);

	TRANSFORM[0].xyz = basis_x;
	TRANSFORM[1].xyz = basis_y;
	TRANSFORM[2].xyz = basis_z;

	VELOCITY = vec3(0.0);
}
