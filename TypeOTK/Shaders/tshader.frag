#version 400 core

in vec2 texCoord;
in vec4 vertexColor;

uniform vec4 ourColor;
uniform sampler2D texture1;
// When set, anything sampled outside the texture is transparent instead of repeating
uniform int discardOutside;

// How the color blends with the texture: 0-2 tint (multiply), 3 add, 4 subtract, 5 screen, 6 overlay, 7 darken, 8 lighten, 9 difference
uniform int blendMode;

out vec4 outColor;

vec3 blendColors(vec3 b, vec3 s)
{
    if (blendMode == 3) return min(b + s, 1.0);
    if (blendMode == 4) return max(b - s, 0.0);
    if (blendMode == 5) return 1.0 - (1.0 - b) * (1.0 - s);
    if (blendMode == 6) return mix(1.0 - 2.0 * (1.0 - b) * (1.0 - s), 2.0 * b * s, step(b, vec3(0.5)));
    if (blendMode == 7) return min(b, s);
    if (blendMode == 8) return max(b, s);
    if (blendMode == 9) return abs(b - s);
    return b * s;
}

void main(void)
{
    if (discardOutside != 0 && (texCoord.x < 0.0 || texCoord.y < 0.0 || texCoord.x > 1.0 || texCoord.y > 1.0))
        discard;
    vec4 tex = texture(texture1, texCoord);
    vec4 tint = ourColor * vertexColor;
    outColor = vec4(blendColors(tex.rgb, tint.rgb), tex.a * tint.a);
}