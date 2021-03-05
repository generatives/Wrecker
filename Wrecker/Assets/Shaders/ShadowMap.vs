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

layout(set = 2, binding = 0, rgba32f) uniform image3D LightTexture;

layout(location = 0) in vec3 Position;
layout(location = 2) in vec3 Normal; 

layout(location = 0) out vec3 fsin_worldPosition;

void main()
{
    vec4 worldLightProbePos = World * vec4(Normal, 1);
    fsin_worldPosition = worldLightProbePos.xyz;
	//ivec3 texIndex = ivec3(floor(worldLightProbePos.xyz) + ivec3(64, 64, 64));
	//imageStore(LightTexture, texIndex, vec4(1, 0, 0, 0));
    
    vec4 worldPosition = World * vec4(Position, 1); 
    vec4 clipPosition = LightProjMatrix * LightViewMatrix * worldPosition;
    gl_Position = clipPosition;
}