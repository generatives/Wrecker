using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Materials
{
    public class Mesh3d
    {
        public const string VertexCode = @"
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

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in vec3 Normal;
layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out vec3 fsin_normal;
void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    fsin_texCoords = TexCoords;
    fsin_normal = Normal;
}";

        public const string FragmentCode = @"
#version 450
layout(location = 0) in vec2 fsin_texCoords;
layout(location = 1) in vec3 fsin_normal;
layout(location = 0) out vec4 fsout_color;
layout(set = 0, binding = 2) uniform SceneColours
{
    vec4 WireframeColour;
};
layout(set = 0, binding = 3) uniform SceneLighting
{
    vec4 DiffuseLightColour;
    vec3 DiffuseLightDirection;
    vec4 AmbientLightColour;
    float AmbientLightStrength;
};
layout(set = 1, binding = 1) uniform texture2D SurfaceTexture;
layout(set = 1, binding = 2) uniform sampler SurfaceSampler;
void main()
{
    vec4 objectColour = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords) * WireframeColour;

    vec3 norm = normalize(fsin_normal);
    float diffuse = max(dot(norm, DiffuseLightDirection), 0.0);

    vec3 result = (0.4 + diffuse) * objectColour.xyz;
    //fsout_color = vec4(result, 1.0);
    fsout_color = objectColour;
}";
    }
}
