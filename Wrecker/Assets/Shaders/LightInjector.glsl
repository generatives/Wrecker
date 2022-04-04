#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba8ui) uniform uimage3D LightImage;
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
layout(set = 1, binding = 4) uniform LightProperties
{
    vec4 nearColour;
    vec4 farColour;
    float minDistance;
    float maxDistance;
    float pad1;
    float pad2;
    vec4 lightWorldPosition;
};

layout(set = 2, binding = 0) uniform WorldBuffer
{
    mat4 World;
};

layout(set = 3, binding = 0, r8ui) uniform uimage3D OpacityImage;

layout(set = 4, binding = 0) uniform GridIndexOffsetBinding
{
    ivec4 GridIndexOffset;
};

const ivec3 TEX_MIN = ivec3(0, 0, 0);

uvec3 GetLightValue(vec4 worldPos) {
    vec4 lightSpacePos = LightProjMatrix * LightViewMatrix * worldPos;
    vec3 projSpacePosition = lightSpacePos.xyz / lightSpacePos.w;

    projSpacePosition.x = projSpacePosition.x * 0.5 + 0.5;
    projSpacePosition.y = -projSpacePosition.y * 0.5 + 0.5;
    float currentDepth = projSpacePosition.z;
    
    vec2 texelSize = 1.0 / vec2(1024, 1024);
    float sum = 0.0;
    for(int x = -1; x <= 1; x++) {
        for(int y = -1; y <= 1; y++) {
            vec2 index = projSpacePosition.xy + vec2(x, y) * texelSize;
	        float pcfDepth = texture(sampler2D(LightDepthTexture, LightDepthSampler), index).r;
            sum = sum + (pcfDepth > currentDepth ? 1.0 : 0.0);
        }
    }
    float avg = sum / 9.0;

    float distance = length(worldPos - lightWorldPosition);
    float weight = (distance - minDistance) / (maxDistance - minDistance);
    vec3 str = mix(nearColour.rgb, farColour.rgb, weight);
    
    return uvec3(str * avg);
}

uint GetOpacityValue(ivec3 texIndex) {
    ivec3 clampedTexIndex = clamp(texIndex, TEX_MIN, imageSize(OpacityImage));
    return imageLoad(OpacityImage, clampedTexIndex).r;
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID.xyz) + GridIndexOffset.xyz;
    uint ownOpacity = GetOpacityValue(texIndex);
    uvec4 ownTexValue = imageLoad(LightImage, texIndex);

    uvec3 opacity1 = uvec3(GetOpacityValue(texIndex + ivec3(0, 1, 0)), GetOpacityValue(texIndex + ivec3(0, -1, 0)), GetOpacityValue(texIndex + ivec3(1, 0, 0)));
    uvec3 opacity2 = uvec3(GetOpacityValue(texIndex + ivec3(-1, 0, 0)), GetOpacityValue(texIndex + ivec3(0, 0, 1)), GetOpacityValue(texIndex + ivec3(0, 0, -1)));

    bool hasOpaqueNeighbour = any(greaterThan(opacity1, vec3(0, 0, 0))) || any(greaterThan(opacity2, vec3(0, 0, 0)));

    vec3 localPosition = (texIndex - Offset.xyz) + vec3(0.5, 0.5, 0.5);
    vec4 worldPosition = World * vec4(localPosition, 1.0);

    // true is for hasOpaqueNeighbour
    uvec3 lightValue = (ownOpacity != 1 && true) ? GetLightValue(worldPosition) : uvec3(0);

    uvec3 ownLightValue = ownTexValue.rgb;
    uvec3 currentLightValue = bitfieldExtract(ownLightValue, 0, 4);
    uvec3 newDirectLightValue = clamp(currentLightValue + lightValue, uvec3(0), uvec3(15));
    uvec3 newLightValue = bitfieldInsert(ownLightValue, newDirectLightValue, 0, 4);

    imageStore(LightImage, texIndex, uvec4(newLightValue, ownTexValue.a));
}