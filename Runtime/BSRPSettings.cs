using Barkar.BSRP.Passes.Bloom;
using Barkar.BSRP.Settings;
using Barkar.BSRP.Settings.GTAO;
using Barkar.BSRP.Settings.Shadows;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/BSRPAsset")]
public class BSRPSettings : RenderPipelineAsset<BSRP>
{
    [SerializeField]
    [Range(0.5f, 1)] private float _renderScale = 1f;

    [SerializeField] private GTAOSettings _GTAOSettinhs;
    [SerializeField] private ShadowSettings _shadowSettings;
    [SerializeField] private BloomSettings _bloomSettings;
    [SerializeField] private TonemappingSettings _tonemappingSettings;

    public override string renderPipelineShaderTag => "BSRP";
    
    protected override RenderPipeline CreatePipeline() => new BSRP(_renderScale, _shadowSettings, _bloomSettings, _GTAOSettinhs, _tonemappingSettings);
}
