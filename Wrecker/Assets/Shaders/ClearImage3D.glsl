#version 450

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout(set = 0, binding = 0, rgba32f) uniform image3D Image;

void main()
{
    imageStore(Image, ivec3(gl_GlobalInvocationID), vec4(0.0, 0, 0, 0));
}