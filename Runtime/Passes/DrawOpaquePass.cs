using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

public class DrawOpaquePass
{
    private readonly ProfilingSampler _profilingSampler = new("Draw Opaque Pass");

    private RendererListDesc _rendererListDesc;
    private BaseRenderFunc<DrawOpaqueGeometryPassData, RenderGraphContext> _renderFunction;

    public DrawOpaquePass()
    {
        _renderFunction = RenderFunction;
    }

    public void DrawOpaqueGeometry(RenderGraph renderGraph,
        ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, in ContextContainer input,
        int renderingLayerMask)
    {
        using var builder = renderGraph.AddRenderPass<DrawOpaqueGeometryPassData>(_profilingSampler.name,
            out var data,
            _profilingSampler);

        //TODO refactor
        StencilState stencil = StencilState.defaultValue;
        stencil.SetCompareFunction(CompareFunction.Always);
        stencil.SetPassOperation(StencilOp.Replace);
        stencil.SetFailOperation(StencilOp.Keep);
        stencil.enabled = true;


        RenderStateBlock renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil);
        renderStateBlock.stencilState = stencil;
        renderStateBlock.stencilReference = 8; //?

        _rendererListDesc =
            new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.opaque,
                sortingCriteria = SortingCriteria.CommonOpaque,
                renderingLayerMask = (uint)renderingLayerMask,
                rendererConfiguration = PerObjectData.ReflectionProbes |
                                        PerObjectData.Lightmaps |
                                        PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe |
                                        PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume,

                stateBlock = renderStateBlock
            };

        data.RendererList =
            builder.UseRendererList(renderGraph.CreateRendererList(_rendererListDesc));

        RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();
        data.ColorAttachment0 =
            builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment0), 0);
        data.ColorAttachment1 =
            builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment1), 1);
        data.ColorAttachment2 =
            builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment2), 2);
        data.ColorAttachment3 =
            builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment3), 3);
        data.DepthAttachment =
            builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.ReadWrite);

        builder.AllowPassCulling(false);

        builder.SetRenderFunc(_renderFunction);
    }

    private void RenderFunction(DrawOpaqueGeometryPassData drawOpaqueGeometryPassData, RenderGraphContext context)
    {
        context.cmd.DrawRendererList(drawOpaqueGeometryPassData.RendererList);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }
}