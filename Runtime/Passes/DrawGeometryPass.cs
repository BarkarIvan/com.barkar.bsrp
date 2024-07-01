using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

public class DrawGeometryPass
{
    private static readonly ProfilingSampler OpaqueSampler = new ("Opaque Profiling Sample");
    private static readonly ProfilingSampler TransparentSampler = new ("Transparent Profiling Sample");

    private  RendererListDesc _rendererListDesc;
    private  BaseRenderFunc<DrawGeometryPassData, RenderGraphContext> _renderFunction;

    public DrawGeometryPass()
    {
        _renderFunction = RenderFunction;
    }

    public  void DrawGeometry(RenderGraph renderGraph,
        ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, RenderDestinationTextures input,
        int renderingLayerMask, bool isOpaque, LightingResources lightingResources)
    {
        var profilingSampler = GetProfilingSampler(isOpaque);

        using var builder = renderGraph.AddRenderPass<DrawGeometryPassData>(
            isOpaque ? "Draw Opaque Pass" : "Draw Transparent Pass", out var drawGeometryPassData,
            profilingSampler);
        
        _rendererListDesc =
            new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                renderingLayerMask = (uint)renderingLayerMask,
                //обязательно иначе глюки в SH и GI 
                rendererConfiguration = PerObjectData.ReflectionProbes |
                                        PerObjectData.Lightmaps |
                                        PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe |
                                        PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume,
                //PerObjectData.LightData|
                //  PerObjectData.LightIndices
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

    private static void RenderFunction(DrawGeometryPassData drawGeometryPassData, RenderGraphContext context)
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