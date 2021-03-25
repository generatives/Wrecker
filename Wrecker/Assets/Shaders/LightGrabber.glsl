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

layout(set = 3, binding = 0, rgba32f) uniform image3D OpacityImage;

const ivec3 TEX_MIN = ivec3(0, 0, 0);

float GetLightValue(vec4 worldPos) {
    vec4 lightSpacePos = LightProjMatrix * LightViewMatrix * worldPos;
    vec3 projSpacePosition = lightSpacePos.xyz / lightSpacePos.w;

    if(projSpacePosition.z > 1.0)
        return 0;

    projSpacePosition.x = projSpacePosition.x * 0.5 + 0.5;
    projSpacePosition.y = -projSpacePosition.y * 0.5 + 0.5;
    float currentDepth = projSpacePosition.z;
    
    vec2 texelSize = 1.0 / vec2(1280, 1280);
    float sum = 0.0;
    for(int x = -1; x <= 1; x++) {
        for(int y = -1; y <= 1; y++) {
            vec2 index = projSpacePosition.xy + vec2(x, y) * texelSize;
	        float pcfDepth = texture(sampler2D(LightDepthTexture, LightDepthSampler), index).r;
            sum = sum + (pcfDepth > currentDepth ? 1.0 : 0.0);
        }
    }
    
    return (sum / 9.0) * 16;
}

float GetOpacityValue(ivec3 texIndex) {
    ivec3 clampedTexIndex = clamp(texIndex, TEX_MIN, imageSize(OpacityImage));
    return imageLoad(OpacityImage, clampedTexIndex).r;
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID.xyz);
    float ownOpacity = GetOpacityValue(texIndex);
    vec4 ownTexValue = imageLoad(LightImage, texIndex);

    vec3 localPosition = (texIndex - Offset.xyz) + vec3(0.5, 0.5, 0.5);
    vec4 worldPosition = World * vec4(localPosition, 1.0);

    float directLightValue = GetLightValue(worldPosition) * (1 - ownOpacity);

    imageStore(LightImage, texIndex, vec4(directLightValue, ownTexValue.g, ownTexValue.b, ownTexValue.a));
}