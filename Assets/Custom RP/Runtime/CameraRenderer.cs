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
		context.DrawSkybox(camera);
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