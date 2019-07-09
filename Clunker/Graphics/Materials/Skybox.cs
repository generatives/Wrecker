using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Materials
{
    public class Skybox
    {
        public const string VertexCode = @"
#version 450

layout(set = 0, binding = 0) uniform Projection
{
    mat4 _Proj;
};

layout(set = 0, binding = 1) uniform View
{
    mat4 _View;
};

layout(location = 0) in vec3 vsin_Position;
layout(location = 0) out vec3 fsin_0;

void main()
{
    mat4 view3x3 = mat4(
        _View[0][0], _View[0][1], _View[0][2], 0,
        _View[1][0], _View[1][1], _View[1][2], 0,
        _View[2][0], _View[2][1], _View[2][2], 0,
        0, 0, 0, 1);
    fsin_0 = vsin_Position;
    gl_Position = _Proj * view3x3 * vec4(vsin_Position, 1.0);
}";

        public const string FragmentCode = @"
#version 450

layout(set = 0, binding = 2) uniform textureCube CubeTexture;
layout(set = 0, binding = 3) uniform sampler CubeSampler;

layout(location = 0) in vec3 fsin_0;
layout(location = 0) out vec4 OutputColor;

void main()
{
    //vec3 fsin = vec3(-fsin_0.x, fsin_0.y, fsin_0.z);
    OutputColor = texture(samplerCube(CubeTexture, CubeSampler), fsin_0);
}";
    }
}
