using Barkar.BSRP.CameraRenderer;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;

public class SetupPass
{
    private static readonly ProfilingSampler profilingSampler = new("Setup Pass");

    private TextureHandle _colorAttachment;
    private TextureHandle _depthAttachment;
    private CameraClearFlags _cameraClearFlags;
    private Vector2Int _attachmentSize;

    private Camera camera;
    
    //use hdr?
    public static RenderDestinationTextures Record(RenderGraph graph, Vector2Int attachmetSize,
        Camera camera, bool HDR)
    {
        SetupPass setupPass;
        using RenderGraphBuilder builder = graph.AddRenderPass(profilingSampler.name, out setupPass, profilingSampler);
        setupPass._attachmentSize = attachmetSize;
        setupPass.camera = camera;
        setupPass._cameraClearFlags = camera.clearFlags;

        TextureHandle colorAttachment;
        TextureHandle depthAttachment;

        //texture descriptor
        TextureDesc textureDescriptor = new TextureDesc(attachmetSize.x, attachmetSize.y);
        DefaultFormat format = HDR ? DefaultFormat.HDR : DefaultFormat.LDR;
        textureDescriptor.colorFormat = SystemInfo.GetGraphicsFormat(format);
        textureDescriptor.name = "BSRP_Color_Attachment";
        
        colorAttachment = builder.UseColorBuffer(graph.CreateTexture(textureDescriptor), 0);
        //copy?

        textureDescriptor.depthBufferBits = DepthBits.Depth32;
        textureDescriptor.name = "BSRP_Depth_Attachment";
        depthAttachment = builder.UseDepthBuffer(graph.CreateTexture(textureDescriptor), DepthAccess.ReadWrite);
        //copy?
        
        builder.AllowPassCulling(false); //not cull this pass
        builder.SetRenderFunc<SetupPass>(static(pass, context) => pass.Render(context));

        return new RenderDestinationTextures(colorAttachment, depthAttachment);

    }

    private void Render(RenderGraphContext context)
    {
        CommandBuffer cmd = context.cmd;
        //set
        cmd.SetRenderTarget(_colorAttachment, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store, _depthAttachment, RenderBufferLoadAction.DontCare,RenderBufferStoreAction.Store );
       //clear
        bool isClearDepth = _cameraClearFlags != CameraClearFlags.Nothing;
        bool isClearColor = camera.clearFlags == CameraClearFlags.Color;
        var clearColor = isClearColor ? camera.backgroundColor.linear : Color.clear;
        cmd.ClearRenderTarget(isClearDepth, isClearColor, clearColor);
       
        //set size here?
        
        context.renderContext.ExecuteCommandBuffer(cmd);
        cmd.Clear();
    }



}