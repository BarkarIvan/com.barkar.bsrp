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
        private readonly ProfilingSampler _profilingSampler = new("Setup Pass");
        private Camera _camera;
        private Vector4 _attachmentSize;

        private BloomSettings _bloomSettings;

        private BaseRenderFunc<SetupPassData, RenderGraphContext> _renderFunc;


        public void ExecutePass(RenderGraph renderGraph,
            Vector2 attachmetSize,
            Camera camera, ContextContainer container)
        {
            _renderFunc = RenderFunction;
            _camera = camera;
            _attachmentSize = new Vector4( attachmetSize.x, attachmetSize.y, 1.0f / attachmetSize.x, 1.0f / attachmetSize.y);


            using var builder =
                renderGraph.AddRenderPass<SetupPassData>(_profilingSampler.name, out var setupPassData,
                    _profilingSampler);
            builder.AllowPassCulling(false);

            //texture descriptor
            TextureDesc textureDescriptor =
                new TextureDesc((int)_attachmentSize.x, (int)_attachmentSize.y);

            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.LDR);
            textureDescriptor.name = "Albedo_Smoothness";
            setupPassData.ColorAttachment0 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.name = "Radiance_Metallic";
            setupPassData.ColorAttachment1 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.name = "Normal_Map";
            textureDescriptor.colorFormat = GraphicsFormat.A2B10G10R10_UNormPack32;
            setupPassData.ColorAttachment2 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.name = "Light_Accumulate";
            textureDescriptor.enableRandomWrite = true; 
            
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            setupPassData.ColorAttachment3 = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
            textureDescriptor.enableRandomWrite = false;
            textureDescriptor.name = "Light_Texture_Copy";
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.HDR);
            TextureHandle lightCopyTexture = renderGraph.CreateTexture(textureDescriptor);
            
            textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            textureDescriptor.depthBufferBits = DepthBits.Depth32;
            textureDescriptor.name = "Depth_Stencil";
            setupPassData.DepthAttachment = builder.WriteTexture(renderGraph.CreateTexture(textureDescriptor));
         
            textureDescriptor.name = "Depth_Copy";
            TextureHandle depthCopyTexture = renderGraph.CreateTexture(textureDescriptor);
            builder.SetRenderFunc(_renderFunc);

            RenderDestinationTextures textures = container.GetOrCreate<RenderDestinationTextures>();
            textures.ColorAttachment0 = setupPassData.ColorAttachment0;
            textures.ColorAttachment1 = setupPassData.ColorAttachment1;
            textures.ColorAttachment2 = setupPassData.ColorAttachment2;
            textures.ColorAttachment3 = setupPassData.ColorAttachment3;
            textures.DepthAttachment = setupPassData.DepthAttachment;
            textures.DepthAttachmentCopy = depthCopyTexture;
            textures.LightTextureCopy = lightCopyTexture;
        }

        private void RenderFunction(SetupPassData setupPassData, RenderGraphContext context)
        {

            CommandBuffer cmd = context.cmd;
            cmd.SetRenderTarget(setupPassData.ColorAttachment0, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store,
                setupPassData.DepthAttachment, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
            bool isClearDepth = _camera.clearFlags <= CameraClearFlags.Nothing;
            bool isClearColor = _camera.clearFlags <= CameraClearFlags.Color;
            var clearColor = _camera.clearFlags == CameraClearFlags.Color
                ? _camera.backgroundColor.linear
                : Color.clear;
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);

            cmd.SetRenderTarget(setupPassData.ColorAttachment1, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);

            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            
            cmd.SetRenderTarget(setupPassData.ColorAttachment2, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);

            cmd.SetRenderTarget(setupPassData.ColorAttachment3, RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store);
            cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
            cmd.SetGlobalVector(BSRPShaderIDs.RenderSizeParamsID, _attachmentSize);
            context.renderContext.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }
    }
}