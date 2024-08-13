using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class PointLightsPassData
    {
        public BufferHandle TileLightCountBuffer;
        public BufferHandle TileLightIndicesBuffer;
        public TextureHandle CameraDepth;
        public TextureHandle DepthAttachment;
        public TextureHandle NormalTexture;
        public TextureHandle AlbedoSmoothnessTexture;
        public TextureHandle RadianceMetallicTexture;
        public TextureHandle LightAccumTexture;
        public Material _testPLMaterial;
    }

    public class PointLightsPass
    {
        private readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Point Lights Pass");

        private Vector4 _textureParams;
        private static readonly int TextureParams = Shader.PropertyToID("_TextureParams");
        private static readonly int TileLightCountBuffer = Shader.PropertyToID("_TileLightCountBuffer");
        private static readonly int TileLightIndicesBuffer = Shader.PropertyToID("_TileLightIndicesBuffer");

        private BaseRenderFunc<PointLightsPassData, RenderGraphContext> _renderFunc;

        public PointLightsPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePointLightPass(RenderGraph renderGraph, in ContextContainer input,
            in PointLightsCullingData cullingData, Material testLightMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<PointLightsPassData>(_profilingSampler.name, out var data, _profilingSampler);
            RenderDestinationTextures destinationTextures = input.Get<RenderDestinationTextures>();

            var info = renderGraph.GetRenderTargetInfo(destinationTextures.DepthAttachment);

            _textureParams = new Vector4(info.width, info.height, 0, 0);

            data.TileLightCountBuffer = builder.ReadBuffer(cullingData.TileLightCountBuffer);
            data.TileLightIndicesBuffer = builder.ReadBuffer(cullingData.TileLightIndicesBuffer);

            data.DepthAttachment = builder.UseDepthBuffer(destinationTextures.DepthAttachment, DepthAccess.Read);
            data.CameraDepth = builder.ReadTexture(destinationTextures.DepthAttachmentCopy);

            data.AlbedoSmoothnessTexture = builder.ReadTexture(destinationTextures.ColorAttachment0);
            data.RadianceMetallicTexture = builder.ReadTexture(destinationTextures.ColorAttachment1);
            data.NormalTexture = builder.ReadTexture(destinationTextures.ColorAttachment2);
            data.LightAccumTexture = builder.UseColorBuffer(destinationTextures.ColorAttachment3, 0);

            builder.AllowPassCulling(false);
            data._testPLMaterial = testLightMaterial;

            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(PointLightsPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();

            mpb.SetTexture(BSRPShaderIDs.GBuffer0ID, data.AlbedoSmoothnessTexture);
            mpb.SetTexture(BSRPShaderIDs.GBuffer1ID, data.RadianceMetallicTexture);
            mpb.SetTexture(BSRPShaderIDs.GBuffer2ID, data.NormalTexture);
            mpb.SetTexture(BSRPShaderIDs.CameraDepthID, data.CameraDepth);
            mpb.SetVector(TextureParams, _textureParams);
            mpb.SetBuffer(TileLightCountBuffer, data.TileLightCountBuffer);
            mpb.SetBuffer(TileLightIndicesBuffer, data.TileLightIndicesBuffer);
            cmd.DrawProcedural(Matrix4x4.identity, data._testPLMaterial, 1, MeshTopology.Triangles, 3, 1, mpb);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}