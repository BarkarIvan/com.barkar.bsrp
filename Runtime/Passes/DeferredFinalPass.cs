using Barkar.BSRP.Settings;
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
        private TonemappingSettings _tonemapSettings;
        static readonly GlobalKeyword[] _tonemapKeyword =
        {
            GlobalKeyword.Create("_ACESTONEMAP"),
            GlobalKeyword.Create("_GTTONEMAP"),
        };

        public DeferredFinalPass()
        {
            _renderFunc = RenderFunction;
        }
        
        private void SetKeywords(GlobalKeyword[] keywords, int enabledIndex, RasterCommandBuffer cmd)
        {
            for (int i = 0; i < keywords.Length; i++)
            {
                cmd.SetKeyword(keywords[i], i == enabledIndex);
            }
        }

        public void ExecutePass(RenderGraph renderGraph,TonemappingSettings tonemapSettings, Material deferredFinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRasterRenderPass<DeferredFinalPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            _tonemapSettings = tonemapSettings;

            data.DeferredFinalPassMaterial = deferredFinalPassMaterial;
            builder.SetRenderAttachment(renderGraph.ImportBackbuffer(BuiltinRenderTextureType.CameraTarget), 0,
                AccessFlags.WriteAll);
            builder.AllowPassCulling(false);
            builder.AllowGlobalStateModification(true);
            builder.SetRenderFunc(_renderFunc);

        }

        private void RenderFunction(DeferredFinalPassData data, RasterGraphContext context)
        {
            var cmd = context.cmd;
            SetKeywords(_tonemapKeyword, (int)_tonemapSettings.Tonemap - 1, cmd);

            cmd.DrawProcedural(Matrix4x4.identity, data.DeferredFinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1);
        }
    }
}