﻿#version 450

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
    float light = imageLoad(LightTexture, texIndex).g;
    return light;
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID) + GridIndexOffset.xyz;

    float opacity = imageLoad(OpacityTexture, texIndex).r;
    vec4 texValue = imageLoad(LightTexture, texIndex);

    bool beat = GridIndexOffset.w == 0;
    beat = beat ^^ (texIndex.x % 2 == 1);
    beat = beat ^^ (texIndex.y % 2 == 1);
    beat = beat ^^ (texIndex.z % 2 == 1);

    if(beat) {
        return;
    }
    
    vec3 lights1 = vec3(getLightValue(texIndex + ivec3(0, 1, 0)), getLightValue(texIndex + ivec3(0, -1, 0)), getLightValue(texIndex + ivec3(1, 0, 0)));
    vec3 lights2 = vec3(getLightValue(texIndex + ivec3(-1, 0, 0)), getLightValue(texIndex + ivec3(0, 0, 1)), getLightValue(texIndex + ivec3(0, 0, -1)));
    vec3 maxLights = max(lights1, lights2);

    float externalLightValue = (max(maxLights.x, max(maxLights.y, maxLights.z)) - 1);

    float directLightValue = texValue.r;

    float newIndirectLightValue = max(max(externalLightValue, directLightValue), 0) * (1.0 - opacity);

    imageStore(LightTexture, texIndex, vec4(texValue.r, newIndirectLightValue, texValue.b, texValue.b));
}