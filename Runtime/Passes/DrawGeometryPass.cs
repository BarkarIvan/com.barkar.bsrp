using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
public class DrawGeometryPass
{
    private static readonly ProfilingSampler OpaqueSampler = new ProfilingSampler("Opaque Profiling Sample");
    private static readonly ProfilingSampler TransparentSampler = new ProfilingSampler("Transparent Profiling Sample");
    
    
    public static void DrawGeometry(RenderGraph renderGraph, ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, RenderDestinationTextures input, int renderingLayerMask, bool isOpaque)
    {
        var profilingSampler = GetProfilingSampler(isOpaque);
        
        using RenderGraphBuilder builder = renderGraph.AddRenderPass<DrawGeometryPassData>(isOpaque ? "Draw Opaque Pass" : "Draw Transparent Pass", out var drawGeometryPassData, profilingSampler);

        //create rederer list
        //render config: lightmaps, light probes, etc. here
        RendererListDesc renderListDescriptor =
            new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                renderingLayerMask = (uint)renderingLayerMask
            };
       
        drawGeometryPassData.RendererListHandle =
            builder.UseRendererList(renderGraph.CreateRendererList(renderListDescriptor));

        drawGeometryPassData.ColorAttachment = builder.ReadWriteTexture(input.ColorAttachment);
        drawGeometryPassData.DepthAttachment = builder.ReadWriteTexture(input.DepthAttachment);
        //TODO: red shadow res
      
        builder.SetRenderFunc((DrawGeometryPassData drawGeometryPassData, RenderGraphContext context) =>
        {
            context.cmd.DrawRendererList(drawGeometryPassData.RendererListHandle);
            context.renderContext.ExecuteCommandBuffer(context.cmd);
            context.cmd.Clear();
        });
    }
    
    private static ProfilingSampler GetProfilingSampler(bool isOpaque)
    {
        return isOpaque ? OpaqueSampler : TransparentSampler;
    }


}