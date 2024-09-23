using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class DeferredFinalPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Deferred Final Pass");
        private readonly BaseRenderFunc<DeferredFinalPassData, RasterGraphContext> _renderFunc;
        private BloomResourses _bloomResourses;

        public DeferredFinalPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, Material deferredFinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRasterRenderPass<DeferredFinalPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            data.DeferredFinalPassMaterial = deferredFinalPassMaterial;
            builder.SetRenderAttachment(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget), 0,
                AccessFlags.WriteAll);
            builder.AllowPassCulling(false);
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DeferredFinalPassData data, RasterGraphContext context)
        {
            var cmd = context.cmd;
            cmd.DrawProcedural(Matrix4x4.identity, data.DeferredFinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1);
        }
    }
}