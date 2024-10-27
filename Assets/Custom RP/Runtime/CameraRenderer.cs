using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer 
{
    const string bufferName = "Render Camera";

	CommandBuffer buffer = new CommandBuffer {
		name = bufferName
	};

    ScriptableRenderContext context;
    CullingResults cullingResults;

    Camera camera;
    Lighting lighting = new Lighting();

    // Shader Tag Ids
    static ShaderTagId 
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit");

    public void Render(
        ScriptableRenderContext context, Camera camera,
        bool useDynamicBatching, bool useGPUInstancing,
        ShadowSettings shadowSettings
    )
    {
        this.camera = camera;
        this.context = context;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(shadowSettings.maxDistance))
        {
            return;
        }

        buffer.BeginSample(sampleName);
        ExecuteBuffer();
        lighting.Setup(context, cullingResults, shadowSettings);
        buffer.EndSample(sampleName);

        Setup();
        DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmos();
        lighting.Cleanup();
        Submit();
    }

    void Setup () 
    {
		context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;

        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth, 
            flags <= CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear);
        buffer.BeginSample(sampleName);

        ExecuteBuffer();
	}

    void DrawVisibleGeometry (bool useDynamicBatching, bool useGPUInstancing) 
    {
        // used to determine whether orthographic or distance-based sorting applie
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };

        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings)
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
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

    void Submit () 
    {
        buffer.EndSample(sampleName);
        ExecuteBuffer();
		context.Submit();
	}

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer); // copies the commands from the buffer
        buffer.Clear();
    }

    bool Cull(float maxDistance)
    {
        ScriptableCullingParameters p;
        if (camera.TryGetCullingParameters(out p))
        {
            p.shadowDistance = Mathf.Min(maxDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }
}