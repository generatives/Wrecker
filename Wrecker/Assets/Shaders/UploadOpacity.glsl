#version 450

layout (local_size_x = 1, local_size_y = 1, local_size_z = 1) in;

layout(set = 0, binding = 0) buffer BlockPositionsBinding
{
    ivec4 BlockPositions[];
};

layout(set = 0, binding = 1) buffer BlockSizesBinding
{
    ivec2 BlockSizes[];
};

layout(set = 0, binding = 2) uniform BlockToLocalOffsetBinding
{
    ivec4 BlockToLocalOffset;
};

layout(set = 1, binding = 0, r8ui) uniform uimage3D Image;

layout(set = 1, binding = 1) uniform ImageData
{
    ivec4 Offset;
};

void main()
{
    ivec3 position = BlockPositions[gl_GlobalInvocationID.x].xyz;
    ivec2 size = BlockSizes[gl_GlobalInvocationID.x];
    ivec3 basePosition = position + BlockToLocalOffset.xyz + Offset.xyz;

    for(int x = 0; x < size.x; x++)
    {
        for(int z = 0; z < size.y; z++)
        {
            ivec3 textureIndex = ivec3(x, 0, z) + basePosition;
            imageStore(Image, textureIndex, uvec4(1, 0, 0, 0));
        }
    }
}