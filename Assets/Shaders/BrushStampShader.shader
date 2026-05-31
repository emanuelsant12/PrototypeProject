Shader "Custom/BrushStampShader"
{
    Properties
    {
        _MainTex ("Current Canvas", 2D) = "white" {}
        _BrushTex ("Brush Texture", 2D) = "white" {}
        _BrushColor ("Brush Color", Color) = (0,0,0,1)
        _BrushUV ("Brush UV", Vector) = (0.5,0.5,0,0)
        _BrushSize ("Brush Size", Float) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            TEXTURE2D(_BrushTex);
            SAMPLER(sampler_BrushTex);

            float4 _BrushColor;
            float4 _BrushUV;
            float _BrushSize;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 canvasColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);

                float2 delta = input.uv - _BrushUV.xy;

                float2 brushUV = delta / _BrushSize + 0.5;

                if (brushUV.x < 0.0 || brushUV.x > 1.0 || brushUV.y < 0.0 || brushUV.y > 1.0)
                {
                    return canvasColor;
                }

                half4 brushSample = SAMPLE_TEXTURE2D(_BrushTex, sampler_BrushTex, brushUV);

                float alpha = brushSample.a;

                half4 result = lerp(canvasColor, _BrushColor, alpha);
                result.a = 1.0;

                return result;
            }

            ENDHLSL
        }
    }
}