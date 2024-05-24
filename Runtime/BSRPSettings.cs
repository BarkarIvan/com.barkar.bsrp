
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/BSRPAsset")]
public class BSRPSettings : RenderPipelineAsset
{
    public bool HDR;
    [Range(0.5f, 1)] public float RenderScale = 1f;

    protected override RenderPipeline CreatePipeline()
    {
        return new BSRP(this);
    }
}
