using System.Collections;
using System.Collections.Generic;
using Palmmedia.ReportGenerator.Core;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadow 
{
    const string bufferName = "Shadows";

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName,
    };

    ScriptableRenderContext context;
    CullingResults cullingResults;
    ShadowSettings shadowSettings;
    const int maxShadowDirectionalLightCount = 1;
    int ShadowedDirectionalLightCount;
    struct ShadowDirectionalLight
    {
        public int visibleLightIndex;
    }
    ShadowDirectionalLight[] ShadowDirectionalLights = new ShadowDirectionalLight[maxShadowDirectionalLightCount];

    static int dirShadowAtlasId = Shader.PropertyToID("_DirectionalShaodwAtlas");

    public void Setup(
        ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings shadowSettings
    )
    {
        this.context = context;
        this.cullingResults = cullingResults;
        this.shadowSettings = shadowSettings;
        ShadowedDirectionalLightCount = 0;
    }

    public void Render()
    {
        if (ShadowedDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            buffer.GetTemporaryRT(dirShadowAtlasId, 1, 1,
				32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        }
    }

    void RenderDirectionalShadows()
    {
        int atlasSize = (int)shadowSettings.directional.atlasSize;
        buffer.GetTemporaryRT(dirShadowAtlasId, atlasSize, atlasSize, 32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        buffer.SetRenderTarget(dirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.ClearRenderTarget(true, false, Color.clear);

        buffer.BeginSample(bufferName);
        ExecuteBuffer();

        for (int i = 0; i < ShadowedDirectionalLightCount; i ++)
        {
            RenderDirectionalShadows(i, atlasSize);
        }
        buffer.EndSample(bufferName);
        ExecuteBuffer();
    }

    void RenderDirectionalShadows (int index, int tileSize) 
    {
        ShadowDirectionalLight light = ShadowDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(cullingResults, light.visibleLightIndex, BatchCullingProjectionType.Orthographic);

        cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.visibleLightIndex, 0, 1, Vector3.zero, tileSize, 0f,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix,
			out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        ExecuteBuffer();
        context.DrawShadows(ref shadowSettings);
    }

    public void Cleanup()
    {
        buffer.ReleaseTemporaryRT(dirShadowAtlasId);
        ExecuteBuffer();
    }

    public void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    public void ReserverDirectionalShadow(Light light, int visibleLightIndex)
    {
        if (ShadowedDirectionalLightCount < maxShadowDirectionalLightCount && 
            light.shadows != LightShadows.None && 
            light.shadowStrength > 0f &&
            cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowDirectionalLights[ShadowedDirectionalLightCount ++] = new ShadowDirectionalLight{
                visibleLightIndex = visibleLightIndex
            };
        }
    }
}
