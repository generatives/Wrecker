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
layout(set = 4, binding = 0) uniform LightInputs
{
    mat4 LightProjMatrix;
};
layout(set = 4, binding = 1) uniform LightInputss
{
    mat4 LightViewMatrix;
};
layout(set = 4, binding = 2) uniform texture2D LightDepthTexture;
layout(set = 4, binding = 3) uniform sampler LightDepthSampler;

layout(set = 5, binding = 0, rgba32f) uniform image3D LightTexture;

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in vec3 Normal;
layout(location = 3) in float Light;

layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out vec3 fsin_normal;
layout(location = 3) out float fsin_OpacityScale;
layout(location = 4) out vec4 fsin_FragPosLightSpace;
layout(location = 5) out float fsin_light;

float lightFromGrid(ivec3 position)
{
    ivec3 texIndex = position + ivec3(64, 64, 64);
    if(texIndex.x > 0 && texIndex.x < 128 && texIndex.y > 0 && texIndex.y < 128 && texIndex.z > 0 && texIndex.z < 128)
    {
        return imageLoad(LightTexture, texIndex).r;
    }
    else
    {
        return 0;
    }
}

float avgLightFromGrid(ivec3 position)
{
	float light = 0.0;
    for(int x = -1; x <= 1; ++x)
	{
		for(int y = -1; y <= 1; ++y)
		{
            light += lightFromGrid(position);
		}    
	}

    return light / 27;
}

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;

    fsin_texCoords = TexCoords;
    fsin_normal = Normal;
    
    vec4 worldLightProbePos = World * vec4(Normal, 1);
    fsin_light = lightFromGrid(ivec3(floor(worldLightProbePos.xyz)));

    float cameraDistance = length(worldPosition.xyz - CameraPosition);
    float blurAmount = (ViewDistance - cameraDistance) / BlurLength;
    fsin_OpacityScale = clamp(blurAmount, 0, 1);
    
    vec4 fragPosLightSpace = LightProjMatrix * LightViewMatrix * worldPosition;
    fsin_FragPosLightSpace = fragPosLightSpace;
}