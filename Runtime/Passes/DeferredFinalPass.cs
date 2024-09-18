using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DeferredFinalPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Deferred Final Pass");
        private BaseRenderFunc<DeferredFinalPassData, RenderGraphContext> _renderFunc;
        private BloomResourses _bloomResourses;
        
        public DeferredFinalPass()
        {
            _renderFunc = RenderFunction;
        }
        
        public void ExecutePass(RenderGraph renderGraph, ContextContainer container,Material deferredFinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DeferredFinalPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);
            RenderDestinationTextures destinationTextures = container.Get<RenderDestinationTextures>();

            data.GBuffer3 = builder.ReadTexture(destinationTextures.ColorAttachment3);
            data.DeferredFinalPassMaterial = deferredFinalPassMaterial;
            data.BackBuffer =
                builder.UseColorBuffer(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget), 0);
            
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DeferredFinalPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            mpb.SetTexture(BSRPShaderIDs.GBuffer3ID, data.GBuffer3 );
            cmd.DrawProcedural(Matrix4x4.identity, data.DeferredFinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1, mpb);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
    }
}
