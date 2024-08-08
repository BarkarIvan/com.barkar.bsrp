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
        public MaterialPropertyBlock MPB;
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

        public void ExecutePointLightPass(RenderGraph renderGraph, in RenderDestinationTextures input,
            in PointLightsCullingData cullingData, Material testLightMaterial)
        {
            using var builder =
                renderGraph.AddRenderPass<PointLightsPassData>(_profilingSampler.name, out var data, _profilingSampler);
            
            var info = renderGraph.GetRenderTargetInfo(input.DepthAttachment);

            _textureParams = new Vector4(info.width, info.height, 0, 0);

            data.TileLightCountBuffer = builder.ReadBuffer(cullingData.TileLightCountBuffer);
            data.TileLightIndicesBuffer = builder.ReadBuffer(cullingData.TileLightIndicesBuffer);

            data.DepthAttachment = builder.UseDepthBuffer(input.DepthAttachment, DepthAccess.Read);
            data.CameraDepth = builder.ReadTexture(input.DepthAttachmentCopy);

            data.AlbedoSmoothnessTexture = builder.ReadTexture(input.ColorAttachment0);
            data.RadianceMetallicTexture = builder.ReadTexture(input.ColorAttachment1);
            data.NormalTexture = builder.ReadTexture(input.ColorAttachment2);
            data.LightAccumTexture = builder.UseColorBuffer(input.ColorAttachment3, 0);
            
            builder.AllowPassCulling(false);
            data._testPLMaterial = testLightMaterial;
            data.MPB = new MaterialPropertyBlock();

            builder.SetRenderFunc(_renderFunc);
        }


        private void RenderFunction(PointLightsPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
           
            data.MPB.SetTexture(BSRPResources.GBuffer0ID, data.AlbedoSmoothnessTexture);
            data.MPB.SetTexture(BSRPResources.GBuffer1ID, data.RadianceMetallicTexture);
            data.MPB.SetTexture(BSRPResources.GBuffer2ID, data.NormalTexture);
            data.MPB.SetTexture(BSRPResources.CameraDepthID, data.CameraDepth);
            data.MPB.SetVector(TextureParams, _textureParams);
            data.MPB.SetBuffer(TileLightCountBuffer, data.TileLightCountBuffer);
            data.MPB.SetBuffer(TileLightIndicesBuffer, data.TileLightIndicesBuffer);
            cmd.DrawProcedural(Matrix4x4.identity, data._testPLMaterial, 1, MeshTopology.Triangles, 3, 1, data.MPB);

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}