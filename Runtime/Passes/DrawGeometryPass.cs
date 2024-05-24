using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
public class DrawGeometryPass
{
    private static readonly ProfilingSampler OpaqueSampler = new ProfilingSampler("Opaque Profiling Sample");
    private static readonly ProfilingSampler TransparentSampler = new ProfilingSampler("Transparent Profiling Sample");

    private RendererListHandle rendererListHandle;

    private void Render(RenderGraphContext context)
    {
        context.cmd.DrawRendererList(rendererListHandle);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }

    public static void Record(RenderGraph renderGraph, ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, RenderDestinationTextures destinationTextures, int renderingLayerMask, bool isOpaque)
    {
        var profilingSampler = GetProfilingSampler(isOpaque);
        var drawGeometryPass = new DrawGeometryPass();
        
        using RenderGraphBuilder builder = renderGraph.AddRenderPass(isOpaque ? "Draw Opaque Pass" : "Draw Transparent Pass", out drawGeometryPass, profilingSampler);

        //create rederer list
        //render config: lightmaps, light probes, etc. here
        RendererListDesc renderListDescriptor =
            new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                renderingLayerMask = (uint)renderingLayerMask
            };
       
        drawGeometryPass.rendererListHandle =
            builder.UseRendererList(renderGraph.CreateRendererList(renderListDescriptor));

        builder.ReadWriteTexture(destinationTextures.ColorAttachment);
        builder.ReadWriteTexture(destinationTextures.DepthAttachment);
        //read shdow resources
        //etc
      
        builder.SetRenderFunc<DrawGeometryPass>((drawGeometryPass, context) => drawGeometryPass.Render(context));
    }
    
    private static ProfilingSampler GetProfilingSampler(bool isOpaque)
    {
        return isOpaque ? OpaqueSampler : TransparentSampler;
    }


}