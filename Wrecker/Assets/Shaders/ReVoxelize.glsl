#version 450

layout(set = 0, binding = 0) buffer BlockPositionsBinding
{
    ivec4 BlockPositions[];
};

layout(set = 0, binding = 1) buffer BlockSizesBinding
{
    ivec2 BlockSizes[];
};

layout(set = 1, binding = 0, rgba32f) uniform image3D SolidityTexture;

layout(set = 1, binding = 1) uniform ModelToTexBinding
{
    mat4 ModelToTex;
};

const ivec3 SOLID_TEX_MIN = ivec3(0, 0, 0);
const ivec3 SOLID_TEX_MAX = ivec3(128, 128, 128);

void main()
{
    ivec3 position = BlockPositions[gl_GlobalInvocationID.x].xyz;
    ivec2 size = BlockSizes[gl_GlobalInvocationID.x];
    for(int x = position.x; x < position.x + size.x; x++)
    {
        for(int z = position.z; z < position.z + size.y; z++)
        {
            vec3 localPosition = vec3(x + 0.5f, position.y + 0.5f, z + 0.5f);
            vec4 texLocPos = ModelToTex * vec4(localPosition, 1);
            ivec3 textureIndex = ivec3(floor(texLocPos.xyz)) + ivec3(64, 64, 64);
            if(all(greaterThanEqual(textureIndex, SOLID_TEX_MIN)) && all(lessThan(textureIndex, SOLID_TEX_MAX)))
            {
                imageStore(SolidityTexture, textureIndex, vec4(1.0, 0, 0, 0));
            }
        }
    }
}