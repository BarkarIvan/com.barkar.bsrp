using System.Data;
using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using Barkar.BSRP.Settings.GTAO;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.UI;

namespace Barkar.BSRP.Passes
{
    public class GTAOTexturesItem : ContextItem
    {
        public TextureHandle GTAOTExture;

        public override void Reset()
        {
            GTAOTExture = default;
        }
    }
    
    public class GTAOPassData
    {
        public Material GTAOMaterial;
        public TextureHandle GTAOTexture;
        public TextureHandle GTAOPassTransientTexture;
        public TextureHandle GTAOPassTransientTexture2;
    }
    
    public class GTAOPass
    {
        private readonly ProfilingSampler _profilingSampler = new("GTAO Pass");
        private readonly BaseRenderFunc<GTAOPassData, RenderGraphContext> _renderFunc;
        private Vector2Int _attachmentSize;
        private GTAOSettings _settings;
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

            _settings = settings;
            var destinationTextures = input.Get<RenderDestinationTextures>();
            var info = renderGraph.GetRenderTargetInfo(destinationTextures.ColorAttachment0);
            _attachmentSize = new Vector2Int(info.width, info.height);
            TextureDesc textureDescriptor =
                new TextureDesc(_attachmentSize.x, _attachmentSize.y);

            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);


            textureDescriptor.name = "GTAOTransient1";
            data.GTAOPassTransientTexture = builder.CreateTransientTexture(textureDescriptor);
            textureDescriptor.name = "GTAOTransient2";
            data.GTAOPassTransientTexture2 = builder.CreateTransientTexture(textureDescriptor);
            
            textureDescriptor.name = "GTAOResultTexture";
            data.GTAOTexture = renderGraph.CreateTexture(textureDescriptor);
            builder.ReadWriteTexture(data.GTAOTexture);
       
            builder.ReadTexture(destinationTextures.ColorAttachment2);
            builder.ReadTexture(destinationTextures.DepthAttachmentCopy);

            data.GTAOMaterial = material;
            
            _camera = camera;
           _GTAOParams = new Vector4(
               settings.Pow,
               settings.Radius,
               settings.SampleCount,
               settings.Thickness);
           
           builder.SetRenderFunc(_renderFunc);

           var GtaoItem = input.GetOrCreate<GTAOTexturesItem>();
           GtaoItem.GTAOTExture = data.GTAOTexture;

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
            float projScale = (float)_attachmentSize.y / (Mathf.Tan(fovRad * 0.5f) * 2) * 0.5f;
            mpb.SetFloat("_AO_HalfProjScale", projScale);
            
            cmd.SetRenderTarget(data.GTAOPassTransientTexture,  RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear);
            cmd.DrawProcedural(Matrix4x4.identity, data.GTAOMaterial, 0, MeshTopology.Triangles, 3,1, mpb);
         
            
            mpb.SetTexture("_GTAOTexture", data.GTAOPassTransientTexture);
            cmd.SetRenderTarget(data.GTAOPassTransientTexture2, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear);
            cmd.DrawProcedural(Matrix4x4.identity, data.GTAOMaterial, 1, MeshTopology.Triangles, 3,1, mpb);
           //
            mpb.SetTexture("_GTAOTexture", data.GTAOPassTransientTexture2);
            mpb.SetFloat("_GTAO_Sharpness", _settings.Sharpness);
            mpb.SetFloat("_GTAO_Intencity", _settings.Intencity);
            cmd.SetRenderTarget(data.GTAOTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(RTClearFlags.Color, Color.clear);
            cmd.DrawProcedural(Matrix4x4.identity, data.GTAOMaterial, 2, MeshTopology.Triangles, 3,1, mpb);

            cmd.SetGlobalTexture(BSRPShaderIDs.GTAOBeentNormalTexture, data.GTAOTexture);

        }
    }
}