using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

public class ScreenSpaceShadowPassData
{
    public TextureHandle TargetGBuffer;
    public TextureHandle CameraDepth;
    public TextureHandle DepthAttachment;
    public Material ScreenSpaceShadowPassMaterial;
}