using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/BSRPAsset")]
public class BSRPSettings : RenderPipelineAsset<BSRP>
{
    [SerializeField]
    private bool HDR;
    [SerializeField]
    [Range(0.5f, 1)] private float _renderScale = 1f;

    [SerializeField] private ShadowSettings _shadowSettings;
    [SerializeField] private BloomSettings _bloomSettings;
    

    public override string renderPipelineShaderTag => "BSRP";
    
    protected override RenderPipeline CreatePipeline() => new BSRP(HDR, _renderScale, _shadowSettings);
}
