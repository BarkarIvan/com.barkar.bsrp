Shader "Hidden/GTAO"
{
    SubShader
    {
        ZTest Always
		Cull Off
		ZWrite Off

        Pass
        {
            Name "GTAO"
            HLSLPROGRAM
            #pragma vertex DefaultPassVertex
            #pragma fragment FragGTAO

            #include "Packages/com.barkar.bsrp/ShaderLibrary/Common.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/CameraRendererPasses.hlsl"
            #include "Packages/com.barkar.bsrp/ShaderLibrary/GTAOPasses.hlsl"
            ENDHLSL
        }

            //Spatial X
            //Spatial Y
    }
}