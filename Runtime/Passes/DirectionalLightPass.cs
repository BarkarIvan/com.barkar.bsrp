using Barkar.BSRP.CameraRenderer;
using UnityEngine;
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
        public MaterialPropertyBlock PropertyBlock;
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

        public void DrawDirectinalLight(RenderGraph renderGraph,
            in RenderDestinationTextures input, Camera camera, Material testfinalPassMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<DirectionalLightPassData>(_profilingSampler.name, out var passData,
                    _profilingSampler);

            builder.AllowPassCulling(false);

            passData.Gbuffer0 = builder.ReadTexture(input.ColorAttachment0);
            passData.Gbuffer1 = builder.ReadTexture(input.ColorAttachment1);
            passData.Gbuffer2 = builder.ReadTexture(input.ColorAttachment2);
            passData.Gbuffer3 = builder.UseColorBuffer(input.ColorAttachment3, 0);
            passData.CameraDepth = builder.UseDepthBuffer(input.DepthAttachment, DepthAccess.ReadWrite);
            passData.TestFinalMaterial = testfinalPassMaterial;
            passData.PropertyBlock = new MaterialPropertyBlock();
            _camera = camera;
            
            builder.SetRenderFunc(_renderFunc);
        }

        private void RenderFunction(DirectionalLightPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;

            data.PropertyBlock.SetTexture(_GBuffer0ID, data.Gbuffer0);
            data.PropertyBlock.SetTexture(_GBuffer1ID, data.Gbuffer1);
            data.PropertyBlock.SetTexture(_GBuffer2ID, data.Gbuffer2);

            data.PropertyBlock.SetTexture(_CameraDepthID, data.CameraDepth);

            cmd.SetViewport(_camera.pixelRect);
            cmd.DrawProcedural(Matrix4x4.identity, data.TestFinalMaterial, 0, MeshTopology.Triangles,
                3, 1, data.PropertyBlock);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}