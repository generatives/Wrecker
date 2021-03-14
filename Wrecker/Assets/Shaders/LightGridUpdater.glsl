#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba32f) uniform image3D LightTexture;
layout(set = 1, binding = 0, rgba32f) uniform image3D OpacityTexture;
layout(set = 2, binding = 0) uniform GridIndexOffsetBinding
{
    ivec4 GridIndexOffset;
};

const ivec3 TEX_MIN = ivec3(0, 0, 0);

float getLightValue(ivec3 texIndex)
{
    float light = imageLoad(LightTexture, texIndex).r;
    return light;
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID) + GridIndexOffset.xyz;

    vec3 lights1 = vec3(getLightValue(texIndex + ivec3(0, 1, 0)), getLightValue(texIndex + ivec3(0, -1, 0)), getLightValue(texIndex + ivec3(1, 0, 0)));
    vec3 lights2 = vec3(getLightValue(texIndex + ivec3(-1, 0, 0)), getLightValue(texIndex + ivec3(0, 0, 1)), getLightValue(texIndex + ivec3(0, 0, -1)));
    vec3 maxLights = max(lights1, lights2);
    
    float opacity = imageLoad(OpacityTexture, texIndex).r;
    float externalLightValue = (max(maxLights.x, max(maxLights.y, maxLights.z)) - (0.0833f)) * (1.0 - opacity);

    float ownLightValue = imageLoad(LightTexture, texIndex).r;
    float lightValueToSet = max(ownLightValue, externalLightValue);
    imageStore(LightTexture, texIndex, vec4(lightValueToSet, 0, 0, 0));
}