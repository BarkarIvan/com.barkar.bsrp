using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using Barkar.BSRP.Settings.GTAO;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class GTAOPassData
    {
        public Material GTAOMaterial;
    }
    
    public class GTAOPass
    {
        private readonly ProfilingSampler _profilingSampler = new("GTAO Pass");
        private readonly BaseRenderFunc<GTAOPassData, RasterGraphContext> _renderFunc;

        public GTAOPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input, GTAOSettings settings)
        {
            using var builder =
                renderGraph.AddRasterRenderPass<GTAOPassData>(_profilingSampler.name, out var data, _profilingSampler);

            var destinationTextures = input.Get<RenderDestinationTextures>();
            builder.SetRenderAttachment(destinationTextures.ColorAttachment1, 0, AccessFlags.WriteAll);

            Vector4 GTAOParams = new Vector4(
               settings.Intensity,
               settings.Radius,
               settings.SampleCount,
               0.0f);
            
            
        }

        private void RenderFunction(GTAOPassData data, RasterGraphContext context)
        {
            
        }
    }
}