using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes.Setup
{
    public class SetupPass
    {
        private readonly ProfilingSampler _profilingSampler = new ("Setup Pass");
        private Camera _camera;
        private Vector2Int _attachmentSize;

        private BloomSettings _bloomSettings;

        private BaseRenderFunc<SetupPassData, RenderGraphContext> _renderFunc;
        

        public  RenderDestinationTextures SetupDestinationTextures(RenderGraph renderGraph,
            Vector2Int attachmetSize,
            Camera camera)
        {
            
            _renderFunc = RenderFunction;
            _camera = camera;
            _attachmentSize = attachmetSize;


            using var builder =
                renderGraph.AddRenderPass<SetupPassData>(_profilingSampler.name, out var setupPassData,
                    _profilingSampler);
            builder.AllowPassCulling(false);

            //texture descriptor
            TextureDesc textureDescriptor =
                new TextureDesc(_attachmentSize.x, _attachmentSize.y);
        
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            textureDescriptor.name = "BSRP_Albedo_Smoothness";
            setupPassData.ColorAttachment0 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.name = "BSRP_Radiance_Metallic";
            setupPassData.ColorAttachment1 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.name = "BSRP_NormalMap";
            textureDescriptor.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
            setupPassData.ColorAttachment2 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.name = "BSRP_Light_Accumulate";
            //textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
           textureDescriptor.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
            setupPassData.ColorAttachment3 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
          
           // 
           textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.name = "BSRP_Depth_Stencil";
            setupPassData.DepthAttachment = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
           
            builder.SetRenderFunc(_renderFunc);

            return new RenderDestinationTextures(setupPassData.ColorAttachment0, setupPassData.ColorAttachment1, setupPassData.ColorAttachment2, setupPassData.ColorAttachment3, setupPassData.DepthAttachment);
        }

        private void RenderFunction(SetupPassData setupPassData, RenderGraphContext context)
        {
            context.renderContext.SetupCameraProperties(_camera);

            CommandBuffer cmd = context.cmd;
            cmd.SetRenderTarget(setupPassData.ColorAttachment0, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, 
                setupPassData.DepthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            bool isClearDepth = _camera.clearFlags <= CameraClearFlags.Nothing;
            bool isClearColor = _camera.clearFlags <= CameraClearFlags.Color;
            var clearColor = _camera.clearFlags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear;
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            
            cmd.SetRenderTarget(setupPassData.ColorAttachment1, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
           
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            
            cmd.SetRenderTarget(setupPassData.ColorAttachment2, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            
            cmd.SetRenderTarget(setupPassData.ColorAttachment3, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}