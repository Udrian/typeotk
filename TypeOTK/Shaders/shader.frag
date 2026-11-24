#version 400 core

uniform vec4 ourColor;

in vec4 vertexColor;
out vec4 outColor;

void main(void)
{
    outColor = ourColor * vertexColor;
}