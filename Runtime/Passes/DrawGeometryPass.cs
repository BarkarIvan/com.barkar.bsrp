using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;

using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
public class DrawGeometryPass
{
    private static readonly ProfilingSampler OpaqueSampler = new ProfilingSampler("Opaque Profiling Sample");
    private static readonly ProfilingSampler TransparentSampler = new ProfilingSampler("Transparent Profiling Sample");

    private static RendererListDesc _rendererListDesc;
    private static UnityEngine.Rendering.RenderGraphModule.BaseRenderFunc<DrawGeometryPassData, UnityEngine.Rendering.RenderGraphModule.RenderGraphContext> _renderFunction;

    static DrawGeometryPass()
    {
        _renderFunction = RenderFunction;
    }

    public static void DrawGeometry(UnityEngine.Rendering.RenderGraphModule.RenderGraph renderGraph,
        ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, RenderDestinationTextures input,
        int renderingLayerMask, bool isOpaque, LightingResources lightingResources)
    {
        var profilingSampler = GetProfilingSampler(isOpaque);

        using (var builder = renderGraph.AddRenderPass<DrawGeometryPassData>(
                   isOpaque ? "Draw Opaque Pass" : "Draw Transparent Pass", out var drawGeometryPassData,
                   profilingSampler))
        {


            //render config: lightmaps, light probes, etc. here
            _rendererListDesc =
                new RendererListDesc(shaderTags, cullingResults, camera)
                {
                    renderQueueRange = isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                    sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                    renderingLayerMask = (uint)renderingLayerMask
                };

            drawGeometryPassData.RendererListHandle =
                builder.UseRendererList(renderGraph.CreateRendererList(_rendererListDesc));

            drawGeometryPassData.ColorAttachment = builder.ReadWriteTexture(input.ColorAttachment);
            drawGeometryPassData.DepthAttachment = builder.ReadWriteTexture(input.DepthAttachment);

            builder.ReadTexture(lightingResources.DirectionalShadowMap);
            builder.ReadBuffer(lightingResources.DirectionalLightBuffer);

            builder.AllowPassCulling(false);

            builder.SetRenderFunc(_renderFunction);
        }
    }

    private static void RenderFunction(DrawGeometryPassData drawGeometryPassData, UnityEngine.Rendering.RenderGraphModule.RenderGraphContext context)
    {
        context.cmd.DrawRendererList(drawGeometryPassData.RendererListHandle);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }
    private static ProfilingSampler GetProfilingSampler(bool isOpaque)
    {
        return isOpaque ? OpaqueSampler : TransparentSampler;
    }


}