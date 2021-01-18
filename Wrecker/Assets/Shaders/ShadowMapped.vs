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

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TexCoords;
layout(location = 2) in vec3 Normal;

layout(location = 0) out vec2 fsin_texCoords;
layout(location = 1) out vec3 fsin_normal;
layout(location = 3) out float fsin_OpacityScale;
layout(location = 4) out vec4 fsin_FragPosLightSpace;
layout(location = 5) out float fsin_FragShadow;

float shadowTest(vec4 fragPosLightSpace, float surfaceLightDiff)
{
    vec3 projCoords = fragPosLightSpace.xyz / fragPosLightSpace.w;

    if(projCoords.z > 1.0)
        return 0.0;

    projCoords.x = projCoords.x * 0.5 + 0.5;
    projCoords.y = -projCoords.y * 0.5 + 0.5; 
    float closestDepth = texture(sampler2D(LightDepthTexture, LightDepthSampler), projCoords.xy).r;
    float currentDepth = projCoords.z;
     
    float bias = max(0.05 * (1.0 - surfaceLightDiff), 0.005);

    float shadow = 0.0;
	vec2 texelSize = 1.0 / vec2(1 * 1024, 1 * 1024);
	for(int x = -1; x <= 1; ++x)
	{
		for(int y = -1; y <= 1; ++y)
		{
			float pcfDepth = texture(sampler2D(LightDepthTexture, LightDepthSampler), projCoords.xy + vec2(x, y) * texelSize).r; 
			shadow += currentDepth - bias > pcfDepth ? 1.0 : 0.0;        
		}    
	}

	shadow = shadow > 2 ? 1 : 0;

    return shadow;
}

void main()
{
    vec4 worldPosition = World * vec4(Position, 1);
    vec4 viewPosition = View * worldPosition;
    vec4 clipPosition = Projection * viewPosition;
    gl_Position = clipPosition;

    fsin_texCoords = TexCoords;
    fsin_normal = Normal;

    float cameraDistance = length(worldPosition.xyz - CameraPosition);
    float blurAmount = (ViewDistance - cameraDistance) / BlurLength;
    fsin_OpacityScale = clamp(blurAmount, 0, 1);
    
    vec3 norm = normalize(fsin_normal);
    float diff = max(dot(norm, vec3(-1, 8, 4)), 0);
    vec4 fragPosLightSpace = LightProjMatrix * LightViewMatrix * worldPosition;
    fsin_FragPosLightSpace = fragPosLightSpace;
    float shadow = shadowTest(fragPosLightSpace, diff);
    fsin_FragShadow = shadow;
}