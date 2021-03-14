#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba32f) uniform image3D LightTexture;
layout(set = 1, binding = 0, rgba32f) uniform image3D SolidityTexture;

const ivec3 TEX_MIN = ivec3(0, 0, 0);

float getLightValue(ivec3 texIndex)
{
    ivec3 clampedTexIndex = clamp(texIndex, TEX_MIN, imageSize(LightTexture));
    float light = imageLoad(LightTexture, clampedTexIndex).r;
    return light;
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID);

    float solidity = imageLoad(SolidityTexture, texIndex).r;

    if(solidity == 1.0)
    {
        imageStore(LightTexture, texIndex, vec4(0, 0, 0, 0));
        return;
    }

    float ownLightValue = getLightValue(texIndex);
    if(ownLightValue == 1.0)
    {
        return;
    }
    
    vec3 lights1 = vec3(getLightValue(texIndex + ivec3(0, 1, 0)), getLightValue(texIndex + ivec3(0, -1, 0)), getLightValue(texIndex + ivec3(1, 0, 0)));
    vec3 lights2 = vec3(getLightValue(texIndex + ivec3(-1, 0, 0)), getLightValue(texIndex + ivec3(0, 0, 1)), getLightValue(texIndex + ivec3(0, 0, -1)));
    vec3 maxLights = max(lights1, lights2);
    float externalLightValue = max(maxLights.x, max(maxLights.y, maxLights.z)) - (1.0f / 12.0f);
    float lightValueToSet = max(ownLightValue, externalLightValue);
    imageStore(LightTexture, texIndex, vec4(lightValueToSet, 0, 0, 0));
    
}