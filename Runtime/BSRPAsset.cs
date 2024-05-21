
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/BSRPAsset")]
public class BSRPAsset : RenderPipelineAsset
{
    public string ExampleString;

    protected override RenderPipeline CreatePipeline()
    {
        return new BSRPInstance(this);
    }
}
