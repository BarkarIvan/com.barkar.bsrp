
using UnityEngine.Rendering.RenderGraphModule;

namespace Barkar.BSRP.Passes
{
    public class PointLightTileCullingPassData
    {
        public BufferHandle TileLightCountBuffer;
        public BufferHandle TileLightIndicesBuffer;
        public TextureHandle CameraDepthTexture;
    }
}
