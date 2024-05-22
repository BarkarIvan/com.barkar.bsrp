using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class BSRPInstance : RenderPipeline
{
    private BSRPAsset _bsrpAsset;

    

    private readonly RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");

    public BSRPInstance(BSRPAsset asset)
    {
        _bsrpAsset = asset;
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);

        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);

            ScriptableCullingParameters cullingParameters;
            if (!camera.TryGetCullingParameters(out cullingParameters)) continue;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            context.SetupCameraProperties(camera);
            
            
           RenderGraphParameters renderGraphParameters = new RenderGraphParameters
           {
               commandBuffer = CommandBufferPool.Get(),
               currentFrameIndex = Time.frameCount,
               executionName = "Camera rg", //sampler prof
               rendererListCulling =  true,
               scriptableRenderContext = context
           };

           //chek cam clear flags  TODO: exclude
           bool drawSkyBox = camera.clearFlags == CameraClearFlags.Skybox;
           bool clearDepth = camera.clearFlags != CameraClearFlags.Nothing;
           bool clearColor = camera.clearFlags == CameraClearFlags.Color;
         
         
          // renderGraphParameters.commandBuffer.ClearRenderTarget(clearDepth, clearColor, Color.clear);
       
           using (RenderGraph.RecordAndExecute(renderGraphParameters))
           {
               //shadows
               
               //Setup and Clear?
               
               DrawGeometryPass.Record(RenderGraph, camera, cullingResults, -1, true);//-1????s
              
               //skybox
               
               //GeometryPassTransparent
               
           }
            context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
            context.Submit();
            CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }
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