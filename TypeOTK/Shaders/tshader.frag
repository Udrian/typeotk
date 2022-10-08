#version 400 core

in vec2 texCoord;

uniform vec4 ourColor;
uniform sampler2D texture1;

void main(void)
{
    gl_FragColor = texture(texture1, texCoord) * ourColor;
}