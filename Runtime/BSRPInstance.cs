using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class BSRPInstance : RenderPipeline
{
    private BSRPAsset _bsrpAsset;
    private ShaderTagId[] _shaderTags = { new ShaderTagId("BSRPLightMode") };
   
    private CommandBuffer skyboxCmd;
    private CommandBuffer _clearRenderTargetCmd;

    private readonly RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    public BSRPInstance(BSRPAsset asset)
    {
        _bsrpAsset = asset;
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);

        _clearRenderTargetCmd = new CommandBuffer();
        _clearRenderTargetCmd.name = "Clear target Commands";
        skyboxCmd = new CommandBuffer();
        skyboxCmd.name = "Render Skybox Commands";

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
        //    _clearRenderTargetCmd.ClearRenderTarget(clearDepth, clearColor, Color.black);
         //   context.ExecuteCommandBuffer(_clearRenderTargetCmd);
           // _clearRenderTargetCmd.Clear();

           RenderGraphParameters renderGraphParameters = new RenderGraphParameters
           {
               commandBuffer = CommandBufferPool.Get(),
               currentFrameIndex = Time.frameCount,
               executionName = "Camera rg", //sampler prof
               rendererListCulling =  true,
               scriptableRenderContext = context
           };

           using (RenderGraph.RecordAndExecute(renderGraphParameters))
           {
               DrawGeometryPass.Record(RenderGraph, camera, cullingResults, -1, true);//-1????s
           }
            
       //     if (drawSkyBox)
           // {
            //    RenderSkybox(context, camera);
           // }
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            renderGraphParameters.commandBuffer.Clear();
            context.Submit();
        }

        //release cmd's
        _clearRenderTargetCmd.Release();
    }
    

    protected override void Dispose(bool disposing)
    {
        RenderGraph.Cleanup();
    }

    //old version
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
    }
}