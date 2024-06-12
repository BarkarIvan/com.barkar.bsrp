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
        
        public static RenderDestinationTextures SetupDestinationTextures(RenderGraph renderGraph,
            Vector2Int attachmetSize,
            Camera camera, bool HDR)
        {
            _camera = camera;
            using var builder = renderGraph.AddRenderPass<SetupPassData>(_profilingSampler.name, out var setupPassData, _profilingSampler);
            setupPassData.AttachmentSize = attachmetSize;
            setupPassData.CameraClearFlags = camera.clearFlags; //send only flags

            //texture descriptor
            TextureDesc textureDescriptor =
                new TextureDesc(setupPassData.AttachmentSize.x, setupPassData.AttachmentSize.y);
            DefaultFormat format = HDR ? DefaultFormat.HDR : DefaultFormat.LDR;
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(format);
            textureDescriptor.name = "BSRP_Color_Attachment";

            setupPassData.ColorAttachment = builder.UseColorBuffer(renderGraph.CreateTexture(textureDescriptor), 0);
            //copy?

            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.name = "BSRP_Depth_Attachment";
            setupPassData.DepthAttachment = builder.UseDepthBuffer(renderGraph.CreateTexture(textureDescriptor),
                DepthAccess.ReadWrite);
            //copy?
           

            builder.AllowPassCulling(false); //never cull this pass

            builder.SetRenderFunc((SetupPassData setupPassData, RenderGraphContext context) =>
            {
                CommandBuffer cmd = context.cmd;
                //set
                context.renderContext.SetupCameraProperties(_camera);
                cmd.SetRenderTarget(setupPassData.ColorAttachment, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store, setupPassData.DepthAttachment, RenderBufferLoadAction.DontCare,
                    RenderBufferStoreAction.Store);
                //clear
                bool isClearDepth = setupPassData.CameraClearFlags != CameraClearFlags.Nothing;
                bool isClearColor = setupPassData.CameraClearFlags == CameraClearFlags.Color;
                var clearColor = isClearColor ? camera.backgroundColor.linear : Color.clear;
                cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);

                //set size here?

                context.renderContext.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            });

            return new RenderDestinationTextures(setupPassData.ColorAttachment, setupPassData.DepthAttachment);
        }
    }
}