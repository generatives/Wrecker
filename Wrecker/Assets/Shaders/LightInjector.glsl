#version 450

layout (local_size_x = 8, local_size_y = 8, local_size_z = 1) in;

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

const ivec3 LIGHT_TEX_MIN = ivec3(0, 0, 0);

// this is supposed to get the world position from the depth buffer
vec3 WorldPosFromDepth(float depth, vec2 texCoord) {
    //float z = depth * 2.0 - 1.0;
    float z = depth - 0.005f;

    vec2 texCoordNegOneToOne = texCoord * 2.0 - 1.0;
    vec4 clipSpacePosition = vec4(texCoordNegOneToOne.x, -texCoordNegOneToOne.y, z, 1.0);
    vec4 viewSpacePosition = inverse(LightProjMatrix) * clipSpacePosition;

    // Perspective division
    viewSpacePosition /= viewSpacePosition.w;

    vec4 worldSpacePosition = inverse(LightViewMatrix) * viewSpacePosition;

    return worldSpacePosition.xyz;
}

void main()
{
    vec2 texCoord = gl_GlobalInvocationID.xy / vec2(1024 * 2, 1024 * 2);
    float depth = texture(sampler2D(LightDepthTexture, LightDepthSampler), texCoord).r;
    vec3 worldPosition = WorldPosFromDepth(depth, texCoord);

    vec4 localPosition = inverse(World) * vec4(worldPosition, 1);
    ivec3 lightImageCoord = ivec3(floor(localPosition)) + Offset.xyz;
    
    if(all(greaterThanEqual(lightImageCoord, LIGHT_TEX_MIN)) && all(lessThan(lightImageCoord, imageSize(LightImage))))
    {
        imageStore(LightImage, lightImageCoord, vec4(1.0, 0, 0, 0));
    }
}