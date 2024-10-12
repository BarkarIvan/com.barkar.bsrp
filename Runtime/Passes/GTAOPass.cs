using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using Barkar.BSRP.Settings.GTAO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
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
        private readonly BaseRenderFunc<GTAOPassData, RenderGraphContext> _renderFunc;
        private Vector2Int _attachmentSize;

        private Camera _camera;
        private Vector4 _GTAOParams;
        public GTAOPass()
        {
            _renderFunc = RenderFunction;
        }

        public void ExecutePass(RenderGraph renderGraph, in ContextContainer input, GTAOSettings settings, Camera camera, Material material)
        {
            using var builder =
                renderGraph.AddRenderPass<GTAOPassData>(_profilingSampler.name, out var data, _profilingSampler);

            var destinationTextures = input.Get<RenderDestinationTextures>();
            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment0);
            _attachmentSize = new Vector2Int(info.width, info.height);
            TextureDesc textureDescriptor =
                new TextureDesc(_attachmentSize.x, _attachmentSize.y);

            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            textureDescriptor.name = "GTAO";
            builder.UseColorBuffer(builder.CreateTransientTexture(textureDescriptor), 0);
            builder.ReadTexture(destinationTextures.ColorAttachment2);
            builder.ReadTexture(destinationTextures.DepthAttachmentCopy);

            data.GTAOMaterial = material;
            
            _camera = camera;
           _GTAOParams = new Vector4(
               settings.Intensity,
               settings.Radius,
               settings.SampleCount,
               0.0f);
            
           builder.SetRenderFunc(_renderFunc);
            
        }

        private void RenderFunction(GTAOPassData data, RenderGraphContext context)
        {
            var cmd = context.cmd;
            var mpb = context.renderGraphPool.GetTempMaterialPropertyBlock();
            mpb.SetVector("_GTAOParams", _GTAOParams);
            
            float fovRad = _camera.fieldOfView * Mathf.Deg2Rad;
            float invHalfTanFov = 1 / Mathf.Tan(fovRad * 0.5f);
            Vector2 focalLen = new Vector2(invHalfTanFov * ((float)_attachmentSize.y / (float)_attachmentSize.x), invHalfTanFov);
            Vector2 invFocalLen = new Vector2(1 / focalLen.x, 1 / focalLen.y);
            mpb.SetVector("_AOUVToViewCoef", new Vector4(2 * invFocalLen.x, 2 * invFocalLen.y, -1 * invFocalLen.x, -1 * invFocalLen.y));
            cmd.ClearRenderTarget(RTClearFlags.All, Color.clear);
            cmd.DrawProcedural(Matrix4x4.identity, data.GTAOMaterial, 3, MeshTopology.Triangles, 3,1, mpb);
        }
    }
}