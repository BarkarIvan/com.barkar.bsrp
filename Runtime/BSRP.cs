using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class BSRP : RenderPipeline
{
    private BSRPSettings _bsrpSettings;
    
    private readonly RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");
    
    private static readonly ShaderTagId[] _commonShaderTags = { new ShaderTagId("BSRPLightMode") };


    public BSRP(BSRPSettings settings)
    {
        _bsrpSettings = settings;
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);
        Vector2Int textureSize = default;
       
        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);
            
            //destination buffer size
            textureSize.x = (int)(camera.pixelWidth * _bsrpSettings.RenderScale);
            textureSize.y = (int)(camera.pixelHeight * _bsrpSettings.RenderScale);
            
            ScriptableCullingParameters cullingParameters;
            if (!camera.TryGetCullingParameters(out cullingParameters)) continue;
            CullingResults cullingResults = context.Cull(ref cullingParameters);
            context.SetupCameraProperties(camera);
            
            
           RenderGraphParameters renderGraphParameters = new RenderGraphParameters
           {
               commandBuffer = CommandBufferPool.Get(),
               currentFrameIndex = Time.frameCount,
               executionName = "Camera Render Graph", 
               rendererListCulling =  true,
               scriptableRenderContext = context
           };
           
           using (RenderGraph.RecordAndExecute(renderGraphParameters))
           {
               using var _ = new RenderGraphProfilingScope(RenderGraph, new ProfilingSampler("Camera_" + camera.name));
               
               //directional shadows

               //setup destinations
               RenderDestinationTextures destinationTextures = SetupPass.Record(RenderGraph, textureSize, camera, _bsrpSettings.HDR);
               //draw opaque
               DrawGeometryPass.Record(RenderGraph, _commonShaderTags, camera, cullingResults, destinationTextures, camera.cullingMask, true);
              
               //skybox
               
               //draw transparent
               
               //...
               //final pass
              
           }
           context.ExecuteCommandBuffer(renderGraphParameters.commandBuffer);
           context.Submit();
           CommandBufferPool.Release(renderGraphParameters.commandBuffer);
        }
        RenderGraph.EndFrame();
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