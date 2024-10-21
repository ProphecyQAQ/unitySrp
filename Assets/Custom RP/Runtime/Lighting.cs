using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting 
{
    const string bufferName = "Light";

    CommandBuffer buffer = new CommandBuffer { name = bufferName };
    CullingResults cullingResults;

    const int maxDirLightCount = 4;
    static int
        dirLightCountID = Shader.PropertyToID("_DirectionalLightCount"),
		dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
		dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    static Vector4[]
        dirLightColors = new Vector4[maxDirLightCount],
        dirLightDirections = new Vector4[maxDirLightCount];

    void SetupDirectionalLight (int index, ref VisibleLight visibleLight) 
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
    }

    void SetupLights () 
    {
        int dirLightCount = 0;
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        for (int i = 0; i < visibleLights.Length; i++)
        {   
            if (visibleLights[i].lightType == LightType.Directional)
            {
                VisibleLight visibleLight = visibleLights[i];
                SetupDirectionalLight(i, ref visibleLight);
                dirLightCount ++;
                if (dirLightCount > maxDirLightCount)
                    break;
            }
        }

        buffer.SetGlobalInt(dirLightCountID, dirLightCount);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
    }

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
