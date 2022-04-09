#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba8ui) uniform uimage3D LightTexture;
layout(set = 1, binding = 0, r8ui) uniform uimage3D OpacityTexture;
layout(set = 2, binding = 0) uniform GridIndexOffsetBinding
{
    ivec4 GridIndexOffset;
};

const ivec3 TEX_MIN = ivec3(0, 0, 0);

uvec3 getLightValue(ivec3 texIndex)
{
    uvec3 light = imageLoad(LightTexture, texIndex).rgb;
    return bitfieldExtract(light, 4, 4);
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID) + GridIndexOffset.xyz;

    uint opacity = imageLoad(OpacityTexture, texIndex).r;
    uvec4 texValue = imageLoad(LightTexture, texIndex);

    uvec3 light1 = getLightValue(texIndex + ivec3(0, 1, 0));
    uvec3 light2 = getLightValue(texIndex + ivec3(0, -1, 0));
    uvec3 light3 = getLightValue(texIndex + ivec3(1, 0, 0));
    uvec3 light4 = getLightValue(texIndex + ivec3(-1, 0, 0));
    uvec3 light5 = getLightValue(texIndex + ivec3(0, 0, 1));
    uvec3 light6 = getLightValue(texIndex + ivec3(0, 0, -1));

    uvec3 maxExternalLightValues = max(light1, max(light2, max(light3, max(light4, max(light5, light6)))));

    uvec3 externalLightValues = mix(maxExternalLightValues - 2, uvec3(0), lessThan(maxExternalLightValues, uvec3(2)));
    
    uvec3 currentLightValues = texValue.rgb;
    uvec3 directLightValues = bitfieldExtract(currentLightValues, 0, 4);

    uvec3 newIndirectLightValues = opacity != 1 ? max(max(externalLightValues, directLightValues), uvec3(2)) : uvec3(0);

    uvec3 newLightValues = bitfieldInsert(currentLightValues, newIndirectLightValues, 4, 4);

    imageStore(LightTexture, texIndex, uvec4(newLightValues, texValue.b));
}