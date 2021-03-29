#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba8ui) uniform uimage3D LightTexture;
layout(set = 1, binding = 0, r8ui) uniform uimage3D OpacityTexture;
layout(set = 2, binding = 0) uniform GridIndexOffsetBinding
{
    ivec4 GridIndexOffset;
};

const ivec3 TEX_MIN = ivec3(0, 0, 0);

uint getLightValue(ivec3 texIndex)
{
    uint light = imageLoad(LightTexture, texIndex).r;
    return bitfieldExtract(light, 4, 4);
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID) + GridIndexOffset.xyz;

    uint opacity = imageLoad(OpacityTexture, texIndex).r;
    uvec4 texValue = imageLoad(LightTexture, texIndex);

    bool beat = GridIndexOffset.w == 0;
    beat = beat ^^ (texIndex.x % 2 == 1);
    beat = beat ^^ (texIndex.y % 2 == 1);
    beat = beat ^^ (texIndex.z % 2 == 1);

    if(beat) {
        return;
    }
    
    uvec3 lights1 = uvec3(getLightValue(texIndex + ivec3(0, 1, 0)), getLightValue(texIndex + ivec3(0, -1, 0)), getLightValue(texIndex + ivec3(1, 0, 0)));
    uvec3 lights2 = uvec3(getLightValue(texIndex + ivec3(-1, 0, 0)), getLightValue(texIndex + ivec3(0, 0, 1)), getLightValue(texIndex + ivec3(0, 0, -1)));
    uvec3 maxLights = max(lights1, lights2);

    uint maxExternalLightValue = max(maxLights.x, max(maxLights.y, maxLights.z));
    uint externalLightValue = maxExternalLightValue != 0 ? maxExternalLightValue - 1 : 0;
    
    uint lightValue = texValue.r;
    uint directLightValue = bitfieldExtract(lightValue, 0, 4);

    uint newIndirectLightValue = opacity != 1 ? max(max(externalLightValue, directLightValue), 0) : 0;

    uint newLightValue = bitfieldInsert(lightValue, newIndirectLightValue, 4, 4);

    imageStore(LightTexture, texIndex, uvec4(newLightValue, texValue.g, texValue.b, texValue.b));
}