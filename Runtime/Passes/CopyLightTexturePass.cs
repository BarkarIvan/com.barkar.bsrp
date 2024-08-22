using Barkar.BSRP.CameraRenderer;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    
    public class CopyLightTexturePass
    {
        private readonly ProfilingSampler _profilingSampler = new("Copy Light Texture Pass");
        private readonly BaseRenderFunc<CopyLightTexturePassData, RenderGraphContext> _renderFunc;

        public CopyLightTexturePass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecuteCopyLightTexturePass(RenderGraph renderGraph, in ContextContainer input)
        {
            using var builder =
                renderGraph.AddRenderPass<CopyLightTexturePassData>(_profilingSampler.name, out var data, _profilingSampler);

            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            data.ColorTextureCopy = builder.WriteTexture(destinationTextures.LightTextureCopy);
            data.OriginalColor = builder.ReadTexture(destinationTextures.ColorAttachment3);
            builder.AllowPassCulling(false);

            builder.SetRenderFunc(_renderFunc);

        }

        private void RenderFunction(CopyLightTexturePassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            cmd.CopyTexture(data.OriginalColor, data.ColorTextureCopy);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}