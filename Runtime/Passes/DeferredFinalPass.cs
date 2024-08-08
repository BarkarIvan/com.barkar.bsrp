using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DeferredFinalPassData
    {
        public TextureHandle GBuffer3;
        public Material DeffereFinalPassMaterial;
        public MaterialPropertyBlock MPB;
    }
    
    
    public class DeferredFinalPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Deferred Final Pass");
        private BaseRenderFunc<DeferredFinalPassData, RenderGraphContext> _renderFunc;
        
        
        public DeferredFinalPass()
        {
            _renderFunc = RenderFunction;
        }
        
        public void DrawDeferredFinalPass(RenderGraph renderGraph,
            in RenderDestinationTextures input,Material DeferredFinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DeferredFinalPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            builder.AllowPassCulling(false);
            data.MPB = new MaterialPropertyBlock();
            data.GBuffer3 = builder.ReadTexture(input.ColorAttachment3);
            data.DeffereFinalPassMaterial = DeferredFinalPassMaterial;
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DeferredFinalPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
           
            data.MPB.SetTexture(BSRPResources.GBuffer3ID, data.GBuffer3 );
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.DrawProcedural(Matrix4x4.identity, data.DeffereFinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1, data.MPB);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
    }
}
