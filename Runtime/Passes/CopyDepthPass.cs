using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP
{
    //TODO exclude

    public class CopyDepthPassData
    {
        public TextureHandle OriginalDepth;
        public TextureHandle DepthTextureCopy;
    }

    public class CopyDepthPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Copy Depth Pass");
        private readonly BaseRenderFunc<CopyDepthPassData, RenderGraphContext> _renderFunc;

        public CopyDepthPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecuteCopyDepthPass(RenderGraph renderGraph, in RenderDestinationTextures input)
        {
            using var builder =
                renderGraph.AddRenderPass<CopyDepthPassData>(_profilingSampler.name, out var data, _profilingSampler);

            data.DepthTextureCopy = builder.WriteTexture(input.DepthAttachmentCopy);
            data.OriginalDepth = builder.ReadTexture(input.DepthAttachment);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(_renderFunc);

        }

        private void RenderFunction(CopyDepthPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.CopyTexture(data.OriginalDepth, data.DepthTextureCopy);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}