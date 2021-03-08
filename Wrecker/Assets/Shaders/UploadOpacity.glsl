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

layout(set = 1, binding = 0, rgba32f) uniform image3D Image;

layout(set = 1, binding = 1) uniform ImageData
{
    ivec4 Offset;
};

const ivec3 SOLID_TEX_MIN = ivec3(0, 0, 0);

void main()
{
    ivec3 position = BlockPositions[gl_GlobalInvocationID.x].xyz;
    ivec2 size = BlockSizes[gl_GlobalInvocationID.x];
    for(int x = position.x; x < position.x + size.x; x++)
    {
        for(int z = position.z; z < position.z + size.y; z++)
        {
            ivec3 localIndex = ivec3(x, position.y, z) + BlockToLocalOffset.xyz;
            ivec3 textureIndex = localIndex + Offset.xyz;
            if(all(greaterThanEqual(textureIndex, SOLID_TEX_MIN)) && all(lessThan(textureIndex, imageSize(Image))))
            {
                imageStore(Image, textureIndex, vec4(1.0, 0, 0, 0));
            }
        }
    }
}