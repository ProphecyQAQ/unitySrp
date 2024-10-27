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
    Shadow shadows = new Shadow();

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
        shadows.ReserverDirectionalShadow(visibleLight.light, index);
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

    public void Cleanup () {
		shadows.Cleanup();
	}

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings)
    {
        this.cullingResults = cullingResults;

        buffer.BeginSample(bufferName);
        shadows.Setup(context, cullingResults, shadowSettings);  
        SetupLights();
        shadows.Render();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }
}
