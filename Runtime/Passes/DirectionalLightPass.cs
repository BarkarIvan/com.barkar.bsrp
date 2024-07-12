using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Data;
using Barkar.BSRP.Passes.Bloom;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{

    public class DirectionalLightPassData
    {
        public TextureHandle Gbuffer0;
        public TextureHandle Gbuffer1;
        public TextureHandle Gbuffer2;
        public TextureHandle Gbuffer3;
        public TextureHandle CameraDepth;
        public Material TestFinalMaterial;
    }
    
    public class DirectionalLightPass
    {
        private readonly ProfilingSampler _profilingSampler = new("Directional lighting");
        private BaseRenderFunc<DirectionalLightPassData, RenderGraphContext> _renderFunc;
        
        private Camera _camera;

        
        private readonly int _GBuffer0ID = Shader.PropertyToID("_GBuffer0");
        private readonly int _GBuffer1ID = Shader.PropertyToID("_GBuffer1");
        private readonly int _GBuffer2ID = Shader.PropertyToID("_GBuffer2");
        private readonly int _GBuffer3ID = Shader.PropertyToID("_GBuffer3");
        private readonly int _CameraDepthID = Shader.PropertyToID("_CameraDepth");
       
         public DirectionalLightPass()
        {
            _renderFunc = RenderFunction;
        }
        
        public  void DrawDirectinalLight( RenderGraph renderGraph,
            in RenderDestinationTextures input, Camera camera, Material testfinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DirectionalLightPassData>(_profilingSampler.name, out var bloomPassData,
                    _profilingSampler);

           builder.AllowPassCulling(false);
           
           bloomPassData.Gbuffer0 = builder.ReadTexture(input.ColorAttachment0);
           bloomPassData.Gbuffer1 = builder.ReadTexture(input.ColorAttachment1);
           bloomPassData.Gbuffer2 = builder.ReadTexture(input.ColorAttachment2);
           bloomPassData.Gbuffer3 = builder.UseColorBuffer(input.ColorAttachment3,0);
           bloomPassData.CameraDepth = builder.UseDepthBuffer(input.DepthAttachment, DepthAccess.ReadWrite);
           bloomPassData.TestFinalMaterial = testfinalPassMaterial;
           _camera = camera;
           

            builder.SetRenderFunc(_renderFunc);
            
        }

        private  void RenderFunction(DirectionalLightPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            //prefilter
           
            mpb.SetTexture(_GBuffer0ID, data.Gbuffer0 );
            mpb.SetTexture(_GBuffer1ID, data.Gbuffer1 );
            mpb.SetTexture(_GBuffer2ID, data.Gbuffer2 );
           // mpb.SetTexture(_GBuffer3ID, data.Gbuffer3 );
            mpb.SetTexture(_CameraDepthID, data.CameraDepth);

            
            //cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget, RenderBufferLoadAction.DontCare,
              //  RenderBufferStoreAction.Store);
            cmd.SetViewport(_camera.pixelRect);
            cmd.DrawProcedural(Matrix4x4.identity, data.TestFinalMaterial, 0, MeshTopology.Triangles,
                3, 1, mpb);
         //   cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}