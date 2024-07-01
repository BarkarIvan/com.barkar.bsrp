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
            Camera camera, bool HDR)
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
            DefaultFormat format = HDR ? DefaultFormat.HDR : DefaultFormat.LDR;
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(format);
            textureDescriptor.name = "BSRP_Color_Attachment";

            setupPassData.ColorAttachment = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));

            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.name = "BSRP_Depth_Attachment";
            setupPassData.DepthAttachment = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
                
            builder.SetRenderFunc(_renderFunc);

            return new RenderDestinationTextures(setupPassData.ColorAttachment, setupPassData.DepthAttachment);
        }

        private void RenderFunction(SetupPassData setupPassData, RenderGraphContext context)
        {
            context.renderContext.SetupCameraProperties(_camera);
            CommandBuffer cmd = context.cmd;
            cmd.SetRenderTarget(setupPassData.ColorAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, 
                setupPassData.DepthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            bool isClearDepth = _camera.clearFlags <= CameraClearFlags.Nothing;
            bool isClearColor = _camera.clearFlags <= CameraClearFlags.Color;
            var clearColor = _camera.clearFlags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear;
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}