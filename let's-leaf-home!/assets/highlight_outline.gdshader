shader_type canvas_item;

uniform vec4 highlight_color : source_color = vec4(1.0, 0.85, 0.2, 0.0);
uniform float outline_width = 2.0;

void fragment() {
	float center_alpha = texture(TEXTURE, UV).a;
	vec2 offset = TEXTURE_PIXEL_SIZE * outline_width;
	float surrounding_alpha = center_alpha;
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV + vec2(offset.x, 0.0)).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV - vec2(offset.x, 0.0)).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV + vec2(0.0, offset.y)).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV - vec2(0.0, offset.y)).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV + offset).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV - offset).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV + vec2(offset.x, -offset.y)).a);
	surrounding_alpha = max(surrounding_alpha, texture(TEXTURE, UV + vec2(-offset.x, offset.y)).a);
	float outline_alpha = max(0.0, surrounding_alpha - center_alpha);
	COLOR = vec4(highlight_color.rgb, highlight_color.a * outline_alpha);
}
