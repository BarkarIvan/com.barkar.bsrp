using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;

public class DirectionalLightPassData
{
    public TextureHandle Gbuffer0;
    public TextureHandle Gbuffer1;
    public TextureHandle Gbuffer2;
    public TextureHandle Gbuffer3;
    public TextureHandle DepthAttachment;
    public TextureHandle CameraDepth;
    public Material TestFinalMaterial;
    public MaterialPropertyBlock PropertyBlock;
}