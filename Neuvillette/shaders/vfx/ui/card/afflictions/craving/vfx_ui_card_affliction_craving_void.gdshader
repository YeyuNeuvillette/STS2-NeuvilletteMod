shader_type canvas_item;
render_mode blend_mix;

uniform vec4 deep_color : source_color = vec4(0.008, 0.003, 0.035, 1.0);
uniform vec4 void_color : source_color = vec4(0.105, 0.025, 0.28, 1.0);
uniform vec4 energy_color : source_color = vec4(0.365, 0.12, 0.78, 1.0);
uniform vec4 spark_color : source_color = vec4(0.39, 0.72, 1.0, 1.0);
uniform float intensity : hint_range(0.0, 1.5) = 0.82;
uniform float speed : hint_range(0.0, 2.0) = 0.72;

float hash21(vec2 p)
{
    p = fract(p * vec2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return fract(p.x * p.y);
}

float value_noise(vec2 p)
{
    vec2 cell = floor(p);
    vec2 local = fract(p);
    local = local * local * (3.0 - 2.0 * local);

    float a = hash21(cell);
    float b = hash21(cell + vec2(1.0, 0.0));
    float c = hash21(cell + vec2(0.0, 1.0));
    float d = hash21(cell + vec2(1.0, 1.0));
    return mix(mix(a, b, local.x), mix(c, d, local.x), local.y);
}

float fbm(vec2 p)
{
    float result = 0.0;
    float weight = 0.5;
    for (int i = 0; i < 4; i++)
    {
        result += value_noise(p) * weight;
        p = p * 2.03 + vec2(13.1, 7.7);
        weight *= 0.5;
    }
    return result;
}

void fragment()
{
    vec2 centered = UV - vec2(0.5);
    vec2 space = vec2(centered.x * 0.76, centered.y);
    float radius = length(space);
    float angle = atan(space.y, space.x);
    float time = TIME * speed;

    // Two uneven counter-flowing bands make the void feel organic rather than geometric.
    float inward_flow = fbm(vec2(angle * 1.35 - time * 0.38, radius * 12.0 - time * 1.7));
    float cross_flow = fbm(vec2(angle * -2.1 + time * 0.24, radius * 19.0 + time * 0.9));
    float wisps = smoothstep(0.48, 0.83, inward_flow * 0.72 + cross_flow * 0.42);
    wisps *= smoothstep(0.04, 0.24, radius) * (1.0 - smoothstep(0.34, 0.63, radius));

    float pulse = sin(time * 2.3 + inward_flow * 4.0) * 0.5 + 0.5;
    float rim_radius = 0.155 + (inward_flow - 0.5) * 0.025;
    float rim = 1.0 - smoothstep(0.012, 0.045, abs(radius - rim_radius));
    rim *= 0.55 + pulse * 0.35;

    float core = 1.0 - smoothstep(0.015, 0.145, radius);
    float halo = (1.0 - smoothstep(0.12, 0.42, radius)) * (1.0 - core);
    float edge_hunger = smoothstep(0.2, 0.66, radius) * (0.25 + 0.3 * inward_flow);

    vec2 star_grid = floor((UV + vec2(0.0, time * 0.025)) * vec2(34.0, 48.0));
    vec2 star_uv = fract((UV + vec2(0.0, time * 0.025)) * vec2(34.0, 48.0)) - 0.5;
    float star_seed = hash21(star_grid);
    float stars = step(0.965, star_seed) * (1.0 - smoothstep(0.035, 0.12, length(star_uv)));
    stars *= smoothstep(0.18, 0.52, radius);

    vec3 color = deep_color.rgb;
    color = mix(color, void_color.rgb, halo * 0.85 + edge_hunger * 0.35);
    color += energy_color.rgb * (wisps * 0.62 + rim * 0.8);
    color += spark_color.rgb * stars * (0.55 + pulse * 0.35);
    color *= 1.0 - core * 0.96;

    float alpha = (0.12 + edge_hunger * 0.18 + halo * 0.24 + wisps * 0.34 + rim * 0.42 + stars * 0.7) * intensity;
    alpha *= smoothstep(0.005, 0.075, radius);
    COLOR = vec4(color, clamp(alpha, 0.0, 0.88));
}
