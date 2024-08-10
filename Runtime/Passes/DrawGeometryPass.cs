using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

public class DrawGeometryPass
{
    private  readonly ProfilingSampler OpaqueSampler = new ("Opaque Profiling Sample");
    private  readonly ProfilingSampler TransparentSampler = new ("Transparent Profiling Sample");

    private  RendererListDesc _rendererListDesc;
    private  BaseRenderFunc<DrawGeometryPassData, RenderGraphContext> _renderFunction;

    public DrawGeometryPass()
    {
        _renderFunction = RenderFunction;
    }
    
    
    

    public  void DrawGeometry(RenderGraph renderGraph,
        ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, in ContextContainer input,
        int renderingLayerMask, bool isOpaque)
    {
        var profilingSampler = GetProfilingSampler(isOpaque);

        using var builder = renderGraph.AddRenderPass<DrawGeometryPassData>(
            isOpaque ? "Draw Opaque Pass" : "Draw Transparent Pass",
            out var drawGeometryPassData,
            profilingSampler);

        //TODO refactor
        StencilState stencil = StencilState.defaultValue;
        stencil.SetCompareFunction(CompareFunction.Always);
        stencil.SetPassOperation(StencilOp.Replace);
        stencil.SetFailOperation(StencilOp.Keep);
        stencil.enabled = true;
        
        
        RenderStateBlock renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil);
        renderStateBlock.stencilState = stencil;
        renderStateBlock.stencilReference = 8;
    
        _rendererListDesc =
            new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = isOpaque ? RenderQueueRange.opaque : RenderQueueRange.transparent,
                sortingCriteria = isOpaque ? SortingCriteria.CommonOpaque : SortingCriteria.CommonTransparent,
                renderingLayerMask = (uint)renderingLayerMask,
                rendererConfiguration = PerObjectData.ReflectionProbes |
                                        PerObjectData.Lightmaps |
                                        PerObjectData.ShadowMask |
                                        PerObjectData.LightProbe |
                                        PerObjectData.OcclusionProbe |
                                        PerObjectData.LightProbeProxyVolume |
                                        PerObjectData.OcclusionProbeProxyVolume,
                
                //PerObjectData.LightData|
                //  PerObjectData.LightIndices
                stateBlock = renderStateBlock
            };

        drawGeometryPassData.RendererListHandle =
            builder.UseRendererList(renderGraph.CreateRendererList(_rendererListDesc));

        RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();
        drawGeometryPassData.ColorAttachment0 = builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment0),0);
        drawGeometryPassData.ColorAttachment1 = builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment1),1);
        drawGeometryPassData.ColorAttachment2 = builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment2),2);
        drawGeometryPassData.ColorAttachment3 = builder.UseColorBuffer(builder.WriteTexture(destinationTextures.ColorAttachment3),3);
        drawGeometryPassData.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.ReadWrite);
        
        builder.AllowPassCulling(false);
        
        builder.SetRenderFunc(_renderFunction);
        
    }

    private  void RenderFunction(DrawGeometryPassData drawGeometryPassData, RenderGraphContext context)
    {
        context.cmd.DrawRendererList(drawGeometryPassData.RendererListHandle);
        context.renderContext.ExecuteCommandBuffer(context.cmd);
        context.cmd.Clear();
    }
    private  ProfilingSampler GetProfilingSampler(bool isOpaque)
    {
        return isOpaque ? OpaqueSampler : TransparentSampler;
    }


}