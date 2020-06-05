using System;
using System.Collections.Generic;
using System.Text;

namespace Clunker.Graphics.Materials
{
    public class PartileSystem
    {
        public const string VertexCode = @"
#version 450
#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

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
layout(set = 2, binding = 0) uniform ParticleSystemParamBuffer
{
    int StartTime;
    float Speed;
};

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in vec3 Normal;

layout(location = 3) in int ParticleStartTime;
layout(location = 4) in vec3 ParticleDirection;

layout(location = 0) out vec3 fsin_Position_WorldSpace;
layout(location = 1) out vec3 fsin_Normal;
layout(location = 2) out vec3 fsin_TexCoord;

void main()
{
    float cosX = cos(InstanceRotation.x);
    float sinX = sin(InstanceRotation.x);
    mat3 instanceRotX = mat3(
        1, 0, 0,
        0, cosX, -sinX,
        0, sinX, cosX);

    float cosY = cos(InstanceRotation.y + LocalRotation);
    float sinY = sin(InstanceRotation.y + LocalRotation);
    mat3 instanceRotY = mat3(
        cosY, 0, sinY,
        0, 1, 0,
        -sinY, 0, cosY);

    float cosZ = cos(InstanceRotation.z);
    float sinZ = sin(InstanceRotation.z);
    mat3 instanceRotZ =mat3(
        cosZ, -sinZ, 0,
        sinZ, cosZ, 0,
        0, 0, 1);

    mat3 instanceRotFull = instanceRotZ * instanceRotY * instanceRotZ;
    mat3 scalingMat = mat3(InstanceScale.x, 0, 0, 0, InstanceScale.y, 0, 0, 0, InstanceScale.z);

    float globalCos = cos(-GlobalRotation);
    float globalSin = sin(-GlobalRotation);

    mat3 globalRotMat = mat3(
        globalCos, 0, globalSin,
        0, 1, 0,
        -globalSin, 0, globalCos);

    vec3 transformedPos = (scalingMat * instanceRotFull * Position) + InstancePosition;
    transformedPos = globalRotMat * transformedPos;
    vec4 pos = vec4(transformedPos, 1);
    fsin_Position_WorldSpace = transformedPos;
    gl_Position = Proj * View * pos;
    fsin_Normal = normalize(globalRotMat * instanceRotFull * Normal);
    fsin_TexCoord = vec3(TexCoord, InstanceTexArrayIndex);
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
layout(set = 1, binding = 3) uniform ObjectProperties
{
    vec4 Colour;
};
void main()
{
    vec4 objectColour = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords) * WireframeColour * Colour;

    vec3 norm = normalize(fsin_normal);
    float diff = max(dot(norm, DiffuseLightDirection), 0.0) + 0.4;
    vec4 diffuse = diff * DiffuseLightColour;
    vec4 litColour = diffuse * objectColour;
    fsout_color = vec4(litColour.xyz, objectColour.w);
}";

        public const string UnlitFragmentCode = @"
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
layout(set = 1, binding = 3) uniform ObjectProperties
{
    vec4 Colour;
};
void main()
{
    vec4 objectColour = texture(sampler2D(SurfaceTexture, SurfaceSampler), fsin_texCoords) * WireframeColour * Colour;

    fsout_color = objectColour;
}";
    }
}
