#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba32f) uniform image3D LightImage;
layout(set = 0, binding = 1) uniform ImageData
{
    ivec4 Offset;
};

layout(set = 1, binding = 0) uniform LightInputs
{
    mat4 LightProjMatrix;
};
layout(set = 1, binding = 1) uniform LightInputss
{
    mat4 LightViewMatrix;
};
layout(set = 1, binding = 2) uniform texture2D LightDepthTexture;
layout(set = 1, binding = 3) uniform sampler LightDepthSampler;

layout(set = 2, binding = 0) uniform WorldBuffer
{
    mat4 World;
};

float GetLightValue(vec4 worldPos) {
    vec4 lightSpacePos = LightProjMatrix * LightViewMatrix * worldPos;
    vec3 projSpacePosition = lightSpacePos.xyz / lightSpacePos.w;

    if(projSpacePosition.z > 1.0)
        return 0.0;

    projSpacePosition.x = projSpacePosition.x * 0.5 + 0.5;
    projSpacePosition.y = -projSpacePosition.y * 0.5 + 0.5;
    float currentDepth = projSpacePosition.z;
    
	float pcfDepth = texture(sampler2D(LightDepthTexture, LightDepthSampler), projSpacePosition.xy).r;

    return pcfDepth > currentDepth ? 1.0 : 0.0;
}

void main()
{
    vec3 localPosition = (ivec3(gl_GlobalInvocationID.xyz) - Offset.xyz) + vec3(0.5, 0.5, 0.5);
    vec4 worldPosition = World * vec4(localPosition, 1.0);

    float lightValue = GetLightValue(worldPosition);
    
    imageStore(LightImage, ivec3(gl_GlobalInvocationID.xyz), vec4(lightValue, 0, 0, 0));
}