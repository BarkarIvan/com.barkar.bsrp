using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP
{

    public class DeferredFinalPassData
    {
        public TextureHandle GBuffer3;
        public Material DeffereFinalPassMaterial;
    }
    
    
    public class DeferredFinalPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Deferred Final Pass");
        private BaseRenderFunc<DeferredFinalPassData, RenderGraphContext> _renderFunc;
        
        private Camera _camera;

        
      //  private readonly int _GBuffer0ID = Shader.PropertyToID("_GBuffer0");
       // private readonly int _GBuffer1ID = Shader.PropertyToID("_GBuffer1");
       // private readonly int _GBuffer2ID = Shader.PropertyToID("_GBuffer2");
        private readonly int _GBuffer3ID = Shader.PropertyToID("_GBuffer3");

        public DeferredFinalPass()
        {
            _renderFunc = RenderFunction;
        }


        public void DrawDeferredFinalPass(RenderGraph renderGraph,
            in RenderDestinationTextures input, Camera camera, Material DeferredFinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DeferredFinalPassData>(_profilingSampler.name, out var data,
                    _profilingSampler);

            builder.AllowPassCulling(false);
           
            data.GBuffer3 = builder.ReadTexture(input.ColorAttachment3);
            data.DeffereFinalPassMaterial = DeferredFinalPassMaterial;
            _camera = camera;
            
            builder.SetRenderFunc(_renderFunc);

        }

        private void RenderFunction(DeferredFinalPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
           
            mpb.SetTexture(_GBuffer3ID, data.GBuffer3 );
            cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.SetViewport(_camera.pixelRect);
            cmd.DrawProcedural(Matrix4x4.identity, data.DeffereFinalPassMaterial, 0, MeshTopology.Triangles,
                3, 1, mpb);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
        
    }
}
