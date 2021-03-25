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

float lightFromGrid(ivec3 position, int smoothingOffsetStart)
{
    ivec3 smoothingOffsets[12] = ivec3[](
        // XZ (0)
        ivec3(0, 0, 0), ivec3(0, 0, -1), ivec3(1, 0, -1), ivec3(1, 0, 0),
        // XY (4)
        ivec3(0, 0, 0), ivec3(0, 1, 0), ivec3(1, 1, 0), ivec3(1, 0, 0),
        // YZ (8)
        ivec3(0, 0, 0), ivec3(0, 1, 0), ivec3(0, 1, -1), ivec3(0, 0, -1)
    );
    float directSum = 0;
    float indirectSum = 0;
    ivec3 texIndex = position + Offset.xyz;
    for(int i = smoothingOffsetStart; i < smoothingOffsetStart + 4; i++) {
        ivec3 smoothingTexIndex = clamp(texIndex + smoothingOffsets[i], LIGHT_TEX_MIN, imageSize(LightTexture));
        vec4 value = imageLoad(LightTexture, smoothingTexIndex);
        directSum = directSum + value.r;
        indirectSum = indirectSum + value.g;
    }
    float direct = float(directSum) / 16.0 / 4.0;
    float indirect = float(indirectSum) / 16.0 / 4.0;
    return (indirect * 0.9) + (direct * 0.1);
}

vec3 getLightProbeOffset(vec3 normal) {
    // XZ
    if(normal.y > 0.5 || normal.y < -0.5) {
        return vec3(-0.5, normal.y * 0.5, 0.5);
    }
    // XY
    if(normal.z > 0.5 || normal.z < -0.5) {
        return vec3(-0.5, -0.5, normal.z * 0.5);
    }
    // YZ
    if(normal.x > 0.5 || normal.x < -0.5) {
        return vec3(normal.x * 0.5, -0.5, 0.5);
    }
}

int getSmoothingOffsetStart(vec3 normal) {
    // XZ
    if(normal.y > 0.5 || normal.y < -0.5) {
        return 0;
    }
    // XY
    if(normal.z > 0.5 || normal.z < -0.5) {
        return 4;
    }
    // YZ
    if(normal.x > 0.5 || normal.x < -0.5) {
        return 8;
    }
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
    vec4 voxelSpacePos = ToVoxelSpace * World * vec4(Position, 1);
    vec3 lightProbePos = voxelSpacePos.xyz + getLightProbeOffset(Normal);
    fsin_light = lightFromGrid(ivec3(floor(lightProbePos)), getSmoothingOffsetStart(Normal));

    // Blur geometry near max view distance
    float cameraDistance = length(worldPosition.xyz - CameraPosition);
    float blurAmount = (ViewDistance - cameraDistance) / BlurLength;
    fsin_OpacityScale = clamp(blurAmount, 0, 1);
}