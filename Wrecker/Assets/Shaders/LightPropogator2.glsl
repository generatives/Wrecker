#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba8ui) uniform uimage3D LightTexture;
layout(set = 1, binding = 0, r8ui) uniform uimage3D OpacityTexture;
layout(set = 2, binding = 0) uniform GridIndexOffsetBinding
{
    ivec4 GridIndexOffset;
};

const ivec3 TEX_MIN = ivec3(0, 0, 0);

const ivec3 distinct_points[25] = ivec3[](ivec3(0, -1, -1), ivec3(0, -1, 1), ivec3(0, 1, 0), ivec3(0, 0, -1), ivec3(0, 0, -2), ivec3(0, 0, 1), ivec3(1, 0, 1), ivec3(1, 1, 0), ivec3(1, 0, -1), ivec3(0, -1, 0), ivec3(0, -2, 0), ivec3(-1, 1, 0), ivec3(1, -1, 0), ivec3(-1, 0, 1), ivec3(-1, 0, -1), ivec3(0, 2, 0), ivec3(0, 0, 0), ivec3(1, 0, 0), ivec3(2, 0, 0), ivec3(-1, -1, 0), ivec3(0, 1, -1), ivec3(0, 1, 1), ivec3(-1, 0, 0), ivec3(-2, 0, 0), ivec3(0, 0, 2));
const int connections[49] = int[](3, 8, 14, 20, 0, 16, 4, 5, 6, 13, 21, 1, 24, 16, 9, 12, 19, 16, 10, 1, 0, 2, 7, 11, 15, 16, 21, 20, 22, 16, 23, 11, 19, 13, 14, 17, 18, 16, 7, 12, 6, 8, 16, 17, 22, 2, 9, 5, 3);
const int origin = 16;

uvec3 getExternalLightValue(ivec3 texIndex)
{
    uvec3 light = imageLoad(LightTexture, texIndex).rgb;
    return bitfieldExtract(light, 4, 4);
}

uvec3 getDirectLightValue(ivec3 texIndex)
{
    uvec3 light = imageLoad(LightTexture, texIndex).rgb;
    return bitfieldExtract(light, 0, 4);
}

uint GetOpacityValue(ivec3 texIndex) {
    return imageLoad(OpacityTexture, texIndex).r;
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID) + GridIndexOffset.xyz;

    //uvec3 opacity1 = uvec3(GetOpacityValue(texIndex + ivec3(0, 1, 0)), GetOpacityValue(texIndex + ivec3(0, -1, 0)), GetOpacityValue(texIndex + ivec3(1, 0, 0)));
    //uvec3 opacity2 = uvec3(GetOpacityValue(texIndex + ivec3(-1, 0, 0)), GetOpacityValue(texIndex + ivec3(0, 0, 1)), GetOpacityValue(texIndex + ivec3(0, 0, -1)));

    //bool hasOpaqueNeighbour = any(greaterThan(opacity1, vec3(0, 0, 0))) || any(greaterThan(opacity2, vec3(0, 0, 0)));

    //if (!hasOpaqueNeighbour) {
    //    return;
    //}

    uvec3 lightValues[25];

    for (int i = 0; i < distinct_points.length(); i++) {
        ivec3 offset = distinct_points[i];
        ivec3 index = texIndex + offset;
        lightValues[i] = getExternalLightValue(index);
    }

    for (int i = 0; i < connections.length(); i+=7) {
        int pointIndex = connections[i];

        uvec3 maxExternalLightValues = uvec3(0, 0, 0);
        maxExternalLightValues = max(maxExternalLightValues, lightValues[connections[i + 1]]);
        maxExternalLightValues = max(maxExternalLightValues, lightValues[connections[i + 2]]);
        maxExternalLightValues = max(maxExternalLightValues, lightValues[connections[i + 3]]);
        maxExternalLightValues = max(maxExternalLightValues, lightValues[connections[i + 4]]);
        maxExternalLightValues = max(maxExternalLightValues, lightValues[connections[i + 5]]);
        maxExternalLightValues = max(maxExternalLightValues, lightValues[connections[i + 6]]);

        uvec3 externalLightValues = mix(maxExternalLightValues - 2, uvec3(2), lessThan(maxExternalLightValues, uvec3(3)));

        uvec3 directLightValues = lightValues[pointIndex];

        ivec3 offset = distinct_points[pointIndex];
        ivec3 index = texIndex + offset;
        uint opacity = imageLoad(OpacityTexture, index).r;

        uvec3 newLightValues = opacity != 1 ? max(externalLightValues, directLightValues) : uvec3(0);
        lightValues[pointIndex] = newLightValues;
    }

    uvec3 newIndirectLightValues = lightValues[origin];

    uvec4 texValue = imageLoad(LightTexture, texIndex);
    uvec3 currentLightValues = texValue.rgb;

    uvec3 newLightValues = bitfieldInsert(currentLightValues, newIndirectLightValues, 4, 4);

    imageStore(LightTexture, texIndex, uvec4(newLightValues, texValue.b));
}