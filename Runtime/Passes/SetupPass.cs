using Barkar.BSRP.CameraRenderer;
using Barkar.BSRP.Passes.Data;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

namespace Barkar.BSRP.Passes
{
    public class SetupPass
    {
        private static readonly ProfilingSampler _profilingSampler = new ProfilingSampler("Setup Pass");
        private static Camera _camera;
        private static Vector2Int _attachmentSize;

        private static BaseRenderFunc<SetupPassData, RenderGraphContext> _renderFunc;

        static SetupPass()
        {
            _renderFunc = RenderFunction;
        }
        
        public static RenderDestinationTextures SetupDestinationTextures(RenderGraph renderGraph,
            Vector2Int attachmetSize,
            Camera camera, bool HDR)
        {
            _camera = camera;
            _attachmentSize = attachmetSize;

            using var builder = renderGraph.AddRenderPass<SetupPassData>(_profilingSampler.name, out var setupPassData, _profilingSampler);
            builder.AllowPassCulling(false); 

            //texture descriptor
            TextureDesc textureDescriptor =
                new TextureDesc(_attachmentSize.x, _attachmentSize.y);
            DefaultFormat format = HDR ? DefaultFormat.HDR : DefaultFormat.LDR;
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(format);
            textureDescriptor.name = "BSRP_Color_Attachment";

            setupPassData.ColorAttachment = builder.UseColorBuffer(renderGraph.CreateTexture(textureDescriptor), 0);
            //copy?

            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.name = "BSRP_Depth_Attachment";
            setupPassData.DepthAttachment = builder.UseDepthBuffer(renderGraph.CreateTexture(textureDescriptor),
                DepthAccess.ReadWrite);

            builder.SetRenderFunc(_renderFunc);
            
            return new RenderDestinationTextures(setupPassData.ColorAttachment, setupPassData.DepthAttachment);
        }

        private static void RenderFunction(SetupPassData setupPassData, RenderGraphContext context)
        {
            
            context.renderContext.SetupCameraProperties(_camera);
            CommandBuffer cmd = context.cmd;

            cmd.SetRenderTarget(setupPassData.ColorAttachment, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store, setupPassData.DepthAttachment, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
           
            bool isClearDepth = _camera.clearFlags <= CameraClearFlags.Nothing;
            bool isClearColor = _camera.clearFlags <= CameraClearFlags.Color;
            var clearColor = _camera.clearFlags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear;
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);

            //set size here?

            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}