#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba32f) uniform image3D LightTexture;
layout(set = 0, binding = 1, rgba32f) uniform image3D SolidityTexture;

const ivec3 LIGHT_TEX_MIN = ivec3(0, 0, 0);
const ivec3 LIGHT_TEX_MAX = ivec3(128, 128, 128);

float getLightValue(ivec3 texIndex)
{
    ivec3 clampedTexIndex = clamp(texIndex, LIGHT_TEX_MIN, LIGHT_TEX_MAX);
    return imageLoad(LightTexture, clampedTexIndex).r;
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
    
    if(texIndex.y == 127)
    {
        imageStore(LightTexture, texIndex, vec4(1.0, 0, 0, 0));
        return;
    }
    
    float aboveValue = getLightValue(texIndex + ivec3(0, 1, 0));
    if(aboveValue == 1.0f)
    {
        imageStore(LightTexture, texIndex, vec4(1.0, 0, 0, 0));
        return;
    }
    
    vec3 lights1 = vec3(getLightValue(texIndex + ivec3(0, 1, 0)), getLightValue(texIndex + ivec3(0, -1, 0)), getLightValue(texIndex + ivec3(1, 0, 0)));
    vec3 lights2 = vec3(getLightValue(texIndex + ivec3(-1, 0, 0)), getLightValue(texIndex + ivec3(0, 0, 1)), getLightValue(texIndex + ivec3(0, 0, -1)));
    vec3 maxLights = max(lights1, lights2);
    float lightValue = max(maxLights.x, max(maxLights.y, maxLights.z));
    imageStore(LightTexture, texIndex, vec4(lightValue - 0.0625f, 0, 0, 0));
    
}