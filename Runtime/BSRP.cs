using System.Collections.Generic;
using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class BSRP : RenderPipeline
{
    //TODO: rreefactor
    private bool _hdr;
    private float _renderScale;
    
    private readonly RenderGraph RenderGraph = new RenderGraph("BSRP Render Graph");
    
    private static readonly ShaderTagId[] _commonShaderTags = { new ShaderTagId("BSRPLightMode") };


    public BSRP(bool HDR, float renderScale)
    {
        //to settings structs
        _hdr = HDR;
        _renderScale = renderScale;
    }


    protected override void Render(ScriptableRenderContext context, List<Camera> cameras)
    {
        BeginContextRendering(context, cameras);
        Vector2Int textureSize = default;
       
        foreach (Camera camera in cameras)
        {
            BeginCameraRendering(context, camera);
            
            //destination buffer size
            textureSize.x = (int)(camera.pixelWidth * _renderScale);
            textureSize.y = (int)(camera.pixelHeight * _renderScale);
            
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
               RenderDestinationTextures destinationTextures = SetupPass.Record(RenderGraph, textureSize, camera, _hdr);
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