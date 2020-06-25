using System;
using System.Collections.Generic;
using System.Text;
using Veldrid;

namespace Clunker.Graphics
{
    public static class LightMeshMaterial
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
layout(set = 3, binding = 0) uniform CameraInputs
{
    vec3 CameraPosition;
    float ViewDistance;
    float BlurLength;
    vec3 Spacing;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in vec3 Normal;
layout(location = 3) in float Light;

layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out vec3 fsin_normal;
layout(location = 2) out float fsin_light;
layout(location = 3) out float fsin_OpacityScale;

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;
    fsin_texCoords = TexCoords;
    fsin_normal = Normal;
    fsin_light = Light;
    float cameraDistance = length(worldPosition.xyz - CameraPosition);
    float blurAmount = (ViewDistance - cameraDistance) / BlurLength;
    fsin_OpacityScale = clamp(blurAmount, 0, 1);
}";

        public const string FragmentCode = @"
#version 450
layout(location = 0) in vec2 fsin_texCoords;
layout(location = 1) in vec3 fsin_normal;
layout(location = 2) in float fsin_light;
layout(location = 3) in float fsin_OpacityScale;

layout(location = 0) out vec4 fsout_color;
layout(set = 0, binding = 2) uniform SceneLighting
{
    vec4 DiffuseLightColour;
    vec3 DiffuseLightDirection;
    vec4 AmbientLightColour;
    float AmbientLightStrength;
};
layout(set = 2, binding = 0) uniform texture2D SurfaceTexture;
layout(set = 2, binding = 1) uniform sampler SurfaceSampler;
layout(set = 2, binding = 2) uniform TextureColour
{
    vec4 Colour;
};

void main()
{
    vec4 objectColour = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords) * Colour;

    vec3 norm = normalize(fsin_normal);
    float diff = dot(norm, DiffuseLightDirection) * 0.5 + 0.5;
    vec4 diffuse = diff * DiffuseLightColour;
    vec4 litColour = fsin_light * diffuse * objectColour;
    fsout_color = vec4(litColour.xyz, objectColour.w * fsin_OpacityScale);
}";

        public static Material Build(GraphicsDevice device, MaterialInputLayouts registry) =>
            new Material(device, VertexCode, FragmentCode, new string[] { "Model", "Lighting" }, new string[] { "SceneInputs", "WorldTransform", "Texture", "CameraInputs" }, registry);
    }
}
