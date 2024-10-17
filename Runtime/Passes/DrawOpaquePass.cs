using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DrawOpaquePass
    {
        private readonly ProfilingSampler _profilingSampler = new("Draw Opaque Pass");

        private RendererListDesc _rendererListDesc;
        private readonly BaseRenderFunc<DrawOpaqueGeometryPassData, RasterGraphContext> _renderFunction;

        public DrawOpaquePass()
        {
            _renderFunction = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph,
            ShaderTagId[] shaderTags, Camera camera, CullingResults cullingResults, in ContextContainer input,
            int renderingLayerMask)
        {
            using var builder = renderGraph.AddRasterRenderPass<DrawOpaqueGeometryPassData>(_profilingSampler.name,
                out var data,
                _profilingSampler);

            
            //TODO refactor
            var stencil = StencilState.defaultValue;
            stencil.SetCompareFunction(CompareFunction.Always);
            stencil.SetPassOperation(StencilOp.Replace);
            stencil.SetFailOperation(StencilOp.Keep);
            stencil.enabled = true;


            var renderStateBlock = new RenderStateBlock(RenderStateMask.Stencil);
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
        
            data.RendererList = renderGraph.CreateRendererList(_rendererListDesc);
            builder.UseRendererList(data.RendererList);

            //TODO to array
            var destinationTextures = input.Get<RenderDestinationTextures>();

            builder.SetRenderAttachment(destinationTextures.ColorAttachment0, 0);
            builder.SetRenderAttachment(destinationTextures.ColorAttachment1, 1);
            builder.SetRenderAttachment(destinationTextures.ColorAttachment2, 2);
            builder.SetRenderAttachment(destinationTextures.ColorAttachment3, 3);
            builder.SetRenderAttachmentDepth(destinationTextures.DepthAttachment);


            builder.SetGlobalTextureAfterPass(destinationTextures.ColorAttachment0, BSRPShaderIDs.GBuffer0ID);
            builder.SetGlobalTextureAfterPass(destinationTextures.ColorAttachment1, BSRPShaderIDs.GBuffer1ID);
            builder.SetGlobalTextureAfterPass(destinationTextures.ColorAttachment2, BSRPShaderIDs.GBuffer2ID);
            builder.SetGlobalTextureAfterPass(destinationTextures.ColorAttachment3, BSRPShaderIDs.GBuffer3ID);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(_renderFunction);
        }

        private void RenderFunction(DrawOpaqueGeometryPassData data, RasterGraphContext context)
        {
            context.cmd.DrawRendererList(data.RendererList);
        }
    }
}