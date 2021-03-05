#version 450

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0, rgba32f) uniform image3D LightImage;

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

const ivec3 LIGHT_TEX_MIN = ivec3(0, 0, 0);
const ivec3 LIGHT_TEX_MAX = ivec3(128, 128, 128);

// this is supposed to get the world position from the depth buffer
vec3 WorldPosFromDepth(float depth, vec2 texCoord) {
    //float z = depth * 2.0 - 1.0;
    float z = depth;

    vec4 clipSpacePosition = vec4(texCoord * 2.0 - 1.0, z, 1.0);
    vec4 viewSpacePosition = inverse(LightProjMatrix) * clipSpacePosition;

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    vec4 worldSpacePosition = inverse(LightViewMatrix) * viewSpacePosition;

    return worldSpacePosition.xyz;
}

void main()
{
    vec2 texCoord = gl_GlobalInvocationID.xy / vec2(1024, 1024);
    float depth = texture(sampler2D(LightDepthTexture, LightDepthSampler), texCoord).r;
    vec3 worldPosition = WorldPosFromDepth(depth, texCoord);

    // vec4 lightImageLocalPosition = LightImageWorldMatrixInv * worldPosition;
    // ivec3 lightImageCoord = ivec3(floor(lightImageLocalPosition.xyz));

    ivec3 lightImageCoord = ivec3(floor(worldPosition)) + ivec3(64, 64, 64);
    
    ivec3 clampedLightImageCoord = clamp(lightImageCoord, LIGHT_TEX_MIN, LIGHT_TEX_MAX);
    imageStore(LightImage, clampedLightImageCoord, vec4(1.0, 0, 0, 0));
}