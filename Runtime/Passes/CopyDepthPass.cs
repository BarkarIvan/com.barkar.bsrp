using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    
    public class CopyDepthPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Copy Depth Pass");
        private readonly BaseRenderFunc<CopyDepthPassData, RenderGraphContext> _renderFunc;

        public CopyDepthPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input)
        {
            using var builder =
                renderGraph.AddRenderPass<CopyDepthPassData>(_profilingSampler.name, out var data, _profilingSampler);

            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            data.DepthTextureCopy = builder.WriteTexture(destinationTextures.DepthAttachmentCopy);
            data.OriginalDepth = builder.ReadTexture(destinationTextures.DepthAttachment);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(_renderFunc);

        }

        private void RenderFunction(CopyDepthPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.CopyTexture(data.OriginalDepth, data.DepthTextureCopy);
            cmd.SetGlobalTexture(BSRPShaderIDs.CameraDepthID, data.DepthTextureCopy);
        }
    }
}