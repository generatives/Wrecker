#version 450

layout(set = 0, binding = 0) uniform WorldBuffer
{
    mat4 World;
};

layout(set = 1, binding = 0) uniform LightInputs
{
    mat4 LightProjMatrix;
};
layout(set = 1, binding = 1) uniform LightInputss
{
    mat4 LightViewMatrix;
};

layout(location = 0) in vec3 Position;

void main()
{
    vec4 worldPosition = World * vec4(Position, 1); 
    vec4 clipPosition = LightProjMatrix * LightViewMatrix * worldPosition;
    gl_Position = clipPosition;
}