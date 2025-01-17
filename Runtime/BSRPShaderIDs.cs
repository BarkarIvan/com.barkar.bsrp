
using UnityEngine;

namespace Barkar.BSRP
{
    public static class BSRPShaderIDs
    {
    
        public static readonly int CameraDepthID = Shader.PropertyToID("_CameraDepth");
        public static readonly int GBuffer0ID = Shader.PropertyToID("_GBuffer0");
        public static readonly int GBuffer1ID = Shader.PropertyToID("_GBuffer1");
        public static readonly int GBuffer2ID = Shader.PropertyToID("_GBuffer2");
        public static readonly int GBuffer3ID = Shader.PropertyToID("_GBuffer3");
        
        public static readonly int DualFilterOffsetID = Shader.PropertyToID("_DualFilterOffset");
        public static readonly int FilterID = Shader.PropertyToID("_Filter");
        
        public static readonly int MainLightShadowMapID = Shader.PropertyToID("_MainLightShadowMap");

        public static readonly int GTAOBeentNormalTexture = Shader.PropertyToID("_GTAOBentNormalTexture");

       
        public static readonly int UnityMatrixIvpID = Shader.PropertyToID("unity_MatrixIVP");
        public static readonly int SourceTextureID = Shader.PropertyToID("_SourceTexture");
        public static readonly int RenderSizeParamsID = Shader.PropertyToID("_RenderSizeParams");
       
        public static readonly int BloomTextureID =  Shader.PropertyToID("_BloomTexture");
        public static readonly int _customBloomLensDirtTextureID = Shader.PropertyToID("_CustomBloomLensDirtTexture");
        public static readonly int _customBloomParamsID = Shader.PropertyToID("_CustomBloomParams");
        
     
    }
}
