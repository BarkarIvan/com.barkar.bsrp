using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RendererUtils;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DepthNormalsOnlyPassData
    {
        public RendererListHandle RendererList;
    }
    
    public class DepthNormalsOnlyPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler(" Depth Normals Pass");
        private RendererListDesc _rendererListDecriptipn;
        private readonly BaseRenderFunc<DepthNormalsOnlyPassData, RasterGraphContext> _renderFunc;

        public DepthNormalsOnlyPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, ShaderTagId[] shaderTags, Camera camera,
            CullingResults cullingResults, in ContextContainer input, int renderingLayerMask)
        {
            using var builder  = renderGraph.AddRasterRenderPass<DepthNormalsOnlyPassData>(
                _profilingSampler.name, out var data, _profilingSampler);


            _rendererListDecriptipn = new RendererListDesc(shaderTags, cullingResults, camera)
            {
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.RenderQueue,
                renderingLayerMask = (uint)renderingLayerMask
            };

            data.RendererList = renderGraph.CreateRendererList(_rendererListDecriptipn);
            builder.UseRendererList(data.RendererList);

            var destinationTextures = input.Get<RenderDestinationTextures>();

            builder.SetRenderAttachment(destinationTextures.ColorAttachment2,0);
            builder.SetRenderAttachmentDepth(destinationTextures.DepthAttachmentCopy);
            builder.SetGlobalTextureAfterPass(destinationTextures.ColorAttachment2, BSRPShaderIDs.GBuffer2ID);
            builder.SetGlobalTextureAfterPass( destinationTextures.DepthAttachmentCopy, BSRPShaderIDs.CameraDepthID);

            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DepthNormalsOnlyPassData data, RasterGraphContext context)
        {
            context.cmd.ClearRenderTarget(true, true, Color.clear);
            context.cmd.DrawRendererList(data.RendererList);
        }

    }
}