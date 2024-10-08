using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer 
{
    const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

    ScriptableRenderContext context;
    CullingResults cullingResults;

    Camera camera;

    // Shader Tag Ids
    static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this.camera = camera;
        this.context = context;

        if (!Cull())
        {
            return;
        }

        Setup();
        DrawVisibleGeometry();
        DrawUnsupportedShaders();
        Submit();
    }

    void Setup () 
    {
		context.SetupCameraProperties(camera);

        buffer.ClearRenderTarget(true, true, Color.clear);
        buffer.BeginSample(bufferName);

        ExecuteBuffer();
	}

    void DrawVisibleGeometry () 
    {
        // used to determine whether orthographic or distance-based sorting applie
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        );
        var filteringSettings = new FilteringSettings(
            RenderQueueRange.opaque
        );

        // Render Opaque
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);

        // Render skybox
		context.DrawSkybox(camera);

        // Render transparent
        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

    void DrawUnsupportedShaders()
    {
        var drawingSettings = new DrawingSettings(
            legacyShaderTagIds[0], new SortingSettings(camera)
        );
        for (int i = 1; i < legacyShaderTagIds.Length; i ++)
        {
            drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
        }
        var filteringSettings = FilteringSettings.defaultValue;
        context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
    }

    void Submit () 
    {
        buffer.EndSample(bufferName);
        ExecuteBuffer();
		context.Submit();
	}

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer); // copies the commands from the buffer
        buffer.Clear();
    }

    bool Cull()
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
}