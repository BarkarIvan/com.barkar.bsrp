using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
public class DrawGeometryPass
{
    private static readonly ProfilingSampler OpaqueSampler = new ProfilingSampler("Opaque Profiling Sample");
    private static readonly ProfilingSampler TransparentSampler = new ProfilingSampler("Transparent Profiling Sample");
    private static readonly ShaderTagId[] ShaderTags = { new ShaderTagId("BSRPLightMode") };

    private RendererListHandle rendererListHandle;

    private void Render(RenderGraphContext context)
    {
        context.cmd.DrawRendererList(rendererListHandle);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }

    public static void Record(RenderGraph renderGraph, Camera camera, CullingResults cullingResults, int renderingLayerMask, bool isOpaque)
    {
        var profilingSampler = isOpaque ? OpaqueSampler : TransparentSampler;
        var drawGeometryPass = new DrawGeometryPass();

        using RenderGraphBuilder builder = renderGraph.AddRenderPass(isOpaque ? "Draw Opaque Pass" : "Draw Transparent Pass", out drawGeometryPass, profilingSampler);
        
        drawGeometryPass.rendererListHandle = builder.UseRendererList(renderGraph.CreateRendererList(new RendererListDesc(ShaderTags, cullingResults, camera)
        {
            renderQueueRange = RenderQueueRange.all,
            sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
            renderingLayerMask = (uint)renderingLayerMask
        }));

        builder.SetRenderFunc<DrawGeometryPass>((drawGeometryPass, context) => drawGeometryPass.Render(context));
    }
}