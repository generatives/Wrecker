#version 450

layout(set = 0, binding = 0, rgba32f) uniform image3D LightTexture;
layout(set = 0, binding = 1, rgba32f) uniform image3D SolidityTexture;

float getLightValue(ivec3 texIndex)
{
    if(texIndex.x > 0 && texIndex.x < 128 && texIndex.y > 0 && texIndex.y < 128 && texIndex.z > 0 && texIndex.z < 128)
    {
        return imageLoad(LightTexture, texIndex).r;
    }
    else
    {
        return 0;
    }
}

float collectValue(float currentValue, float otherValue)
{
    if(otherValue > currentValue)
    {
        return otherValue - 0.25f;
    }
    else
    {
        return currentValue;
    }
}

float collectIndexValue(float currentValue, ivec3 otherIndex)
{
    return collectValue(currentValue, getLightValue(otherIndex));
}

void main()
{
    ivec3 texIndex = ivec3(gl_GlobalInvocationID.xyz);

    float solidity = imageLoad(SolidityTexture, texIndex).r;

    if(solidity == 1.0)
    {
        imageStore(LightTexture, texIndex, vec4(0, 0, 0, 0));
        return;
    }
    
    if(texIndex.y == 127)
    {
        imageStore(LightTexture, texIndex, vec4(1.0, 0, 0, 0));
        return;
    }
    
    float aboveValue = getLightValue(texIndex + ivec3(0, 1, 0));
    if(aboveValue == 1.0f)
    {
        imageStore(LightTexture, texIndex, vec4(1.0, 0, 0, 0));
        return;
    }
    
    float lightValue = imageLoad(LightTexture, texIndex).r;
    lightValue = collectIndexValue(lightValue, texIndex + ivec3(0, 1, 0));
    lightValue = collectIndexValue(lightValue, texIndex + ivec3(0, -1, 0));
    lightValue = collectIndexValue(lightValue, texIndex + ivec3(1, 0, 0));
    lightValue = collectIndexValue(lightValue, texIndex + ivec3(-1, 0, 0));
    lightValue = collectIndexValue(lightValue, texIndex + ivec3(0, 0, 1));
    lightValue = collectIndexValue(lightValue, texIndex + ivec3(0, 0, -1));
    imageStore(LightTexture, texIndex, vec4(lightValue, 0, 0, 0));
    
}