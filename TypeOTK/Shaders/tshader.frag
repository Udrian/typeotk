#version 400 core

in vec2 texCoord;

uniform vec4 ourColor;
uniform sampler2D texture1;

out vec4 outColor;

void main(void)
{
    outColor = texture(texture1, texCoord) * ourColor;
}