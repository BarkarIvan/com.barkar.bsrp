using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class BSRPInstance : RenderPipeline
{
    private BSRPAsset _bsrpAsset;
    private ShaderTagId[] _shaderTags = { new ShaderTagId("BSRPLightMode") };
   
    private CommandBuffer skyboxCmd;

    public BSRPInstance(BSRPAsset asset)
    {
        _bsrpAsset = asset;
        //???
        skyboxCmd = new CommandBuffer();
        skyboxCmd.name = "Render Skybox Command buffer";
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);

        var clearCmd = new CommandBuffer();

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);

            ScriptableCullingParameters cullingParameters;
            if (!camera.TryGetCullingParameters(out cullingParameters)) continue;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            context.SetupCameraProperties(camera);

            //chek cam clear flags  TODO: exclude
            bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox;
            bool clearDepth = camera.clearFlags != CameraClearFlags.Nothing;
            bool clearColor = camera.clearFlags == CameraClearFlags.Color;
           
            //clear cam render target
            clearCmd.name = "Clear Color";
            clearCmd.ClearRenderTarget(clearDepth, clearColor, Color.black);
            context.ExecuteCommandBuffer(clearCmd);
            clearCmd.Clear();

            if (drawSkyBox)
            {
                RenderSkybox(context, camera);
            }
            
            context.Submit();
        }

        //release cmd's
        clearCmd.Release();
        skyboxCmd.Release();
    }
    
    //TODO: exclude from here
    public void RenderSkybox(ScriptableRenderContext context, Camera camera)
    {
        RendererList rendererList = context.CreateSkyboxRendererList(camera);
       
        skyboxCmd.DrawRendererList(rendererList);
        context.ExecuteCommandBuffer(skyboxCmd);
        skyboxCmd.Release();
    }

    //old version
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}