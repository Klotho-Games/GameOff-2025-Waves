Shader "Sprites/HauntingOutlineURP"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        [Header(Outline Settings)]
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        // CHANGE: Removed the (* 100) multiplier. 1.0 now equals roughly 1 pixel width.
        _OutlineWidth ("Outline Width", Float) = 1.0
        
        [Header(Noise Settings)]
        _NoiseScale ("Noise Scale", Float) = 20.0
        _NoiseSpeed ("Noise Speed", Float) = 2.0
        _NoiseThreshold ("Noise Threshold", Range(0, 1)) = 0.4
        
        [Header(Brightness)]
        _EmissiveBoost ("Brightness", Range(0, 2)) = 1.0
    }

    SubShader
    {
        Tags
        { 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "RenderType"="Transparent" 
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
            "RenderPipeline" = "UniversalPipeline"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "Universal2D" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS   : SV_POSITION;
                float4 color        : COLOR;
                float2 uv           : TEXCOORD0;
                float2 positionWS   : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half4 _OutlineColor;
                float _OutlineWidth;
                float _NoiseScale;
                float _NoiseSpeed;
                float _NoiseThreshold;
                float _EmissiveBoost;
                float4 _MainTex_TexelSize;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float rand(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);

                float a = rand(i);
                float b = rand(i + float2(1.0, 0.0));
                float c = rand(i + float2(0.0, 1.0));
                float d = rand(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = vertexInput.positionCS;
                OUT.positionWS = vertexInput.positionWS.xy; 
                OUT.uv = IN.uv;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // 1. Sample the Original Sprite
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                
                // 2. Neighbor Sampling (The "Donut" Logic)
                // We use exact TexelSize steps. 
                // If _OutlineWidth is 1, we step 1 pixel away.
                float2 unit = _MainTex_TexelSize.xy * _OutlineWidth;
                
                float pixelUp    = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(0, unit.y)).a;
                float pixelDown  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(0, unit.y)).a;
                float pixelLeft  = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv - float2(unit.x, 0)).a;
                float pixelRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv + float2(unit.x, 0)).a;

                // 3. Calculate Outline Mask
                // Add all neighbors together to find the "Expanded Shape"
                float alphaSum = saturate(pixelUp + pixelDown + pixelLeft + pixelRight);
                
                // Subtract the original shape to leave ONLY the rim (The Donut)
                float outlineMask = saturate(alphaSum - c.a);

                // 4. Noise Logic
                float timeVal = _Time.y * _NoiseSpeed;
                float2 pos = IN.positionWS * _NoiseScale;
                
                float n1 = noise(pos + float2(timeVal, timeVal * 0.6));
                float n2 = noise(pos - float2(timeVal * 0.7, timeVal));
                float n = (n1 + n2) * 0.5;
                
                float noiseMask = step(_NoiseThreshold, n);

                // 5. Combine Outline
                half4 finalOutline = _OutlineColor;
                finalOutline.a *= outlineMask * noiseMask; 

                // 6. Final Composition
                // We place the original sprite strictly ON TOP of the outline.
                // This ensures the interior ghost never disappears or gets eroded.
                half4 finalColor = lerp(finalOutline, c * IN.color, c.a);
                
                // Brightness Control
                finalColor.rgb *= _EmissiveBoost;
                
                // Pre-multiply Alpha (Standard Unity 2D Blending)
                finalColor.rgb *= finalColor.a;

                return finalColor;
            }
            ENDHLSL
        }
    }
}
