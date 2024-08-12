using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Bloom;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DeferredFinalPassData
    {
        public TextureHandle GBuffer3;
        public Material DeferreFinalPassMaterial;
    }
    
    
    public class DeferredFinalPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Deferred Final Pass");
        private BaseRenderFunc<DeferredFinalPassData, RenderGraphContext> _renderFunc;

        private BloomResourses _bloomResourses;
        
        public DeferredFinalPass()
        {
            _renderFunc = RenderFunction;
        }
        
        public void DrawDeferredFinalPass(RenderGraph renderGraph, ContextContainer container,Material DeferredFinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DeferredFinalPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            builder.AllowPassCulling(false);
            RenderDestinationTextures destinationTextures = container.Get<RenderDestinationTextures>();

            data.GBuffer3 = builder.ReadTexture(destinationTextures.ColorAttachment3);
            data.DeferreFinalPassMaterial = DeferredFinalPassMaterial;
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DeferredFinalPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            mpb.SetTexture(BSRPResources.GBuffer3ID, data.GBuffer3 );
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, data.DeferreFinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1, mpb);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
    }
}
