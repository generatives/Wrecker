#version 450
layout(set = 0, binding = 0) uniform ProjectionBuffer
{
    mat4 Projection;
};
layout(set = 0, binding = 1) uniform ViewBuffer
{
    mat4 View;
};
layout(set = 1, binding = 0) uniform WorldBuffer
{
    mat4 World;
};
layout(set = 3, binding = 0) uniform CameraInputs
{
    vec3 CameraPosition;
    float ViewDistance;
    float BlurLength;
    vec3 Spacing;
};

layout(set = 4, binding = 0, rgba32f) uniform image3D LightTexture;
layout(set = 4, binding = 1) uniform ImageData
{
    ivec4 Offset;
};

layout(set = 5, binding = 0) uniform ToVoxelSpaceTransformBinding
{
    mat4 ToVoxelSpace;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in vec3 Normal;

layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out vec3 fsin_normal;
layout(location = 2) out float fsin_OpacityScale;
layout(location = 3) out float fsin_light;

const ivec3 LIGHT_TEX_MIN = ivec3(0, 0, 0);

float lightFromGrid(ivec3 position)
{
    ivec3 texIndex = position + Offset.xyz;
    texIndex = clamp(texIndex, LIGHT_TEX_MIN, imageSize(LightTexture));
    return imageLoad(LightTexture, texIndex).r;
}

void main()
{
    // Transform to clip position
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;

    // Pass vertex attrs to frag shader
    fsin_texCoords = TexCoords;
    fsin_normal = Normal;
    
    // Get light value from light grid
    vec4 localLightProbePos = vec4(Normal, 1);
    vec4 voxelSpaceLightProbPos = ToVoxelSpace * World *  localLightProbePos;
    fsin_light = lightFromGrid(ivec3(floor(voxelSpaceLightProbPos.xyz)));

    // Blur geometry near max view distance
    float cameraDistance = length(worldPosition.xyz - CameraPosition);
    float blurAmount = (ViewDistance - cameraDistance) / BlurLength;
    fsin_OpacityScale = clamp(blurAmount, 0, 1);
}