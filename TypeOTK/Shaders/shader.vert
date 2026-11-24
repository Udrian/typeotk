#version 400 core

layout(location = 0) in vec3 aPosition;
layout(location = 1) in vec4 aColor;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;
uniform int useVertexColor;

out vec4 vertexColor;

void main(void)
{
    gl_Position = vec4(aPosition, 1.0) * model * view * projection;
    vertexColor = useVertexColor == 0 ? vec4(1.0) : aColor;
}