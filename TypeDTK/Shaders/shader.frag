#version 400 core

uniform vec4 ourColor;

out vec4 outColor;

void main(void)
{
    outColor = ourColor;
}