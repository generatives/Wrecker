#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba8ui) uniform uimage3D LightImage;

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID.xyz);
    uvec4 ownTexValue = imageLoad(LightImage, texIndex);
    uint ownLightValue = ownTexValue.r;
    uint newLightValue = bitfieldInsert(ownLightValue, 0, 0, 4);
    imageStore(LightImage, texIndex, uvec4(newLightValue, ownTexValue.g, ownTexValue.b, ownTexValue.a));
}