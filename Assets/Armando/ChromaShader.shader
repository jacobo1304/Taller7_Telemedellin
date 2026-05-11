Shader "Custom/ChromaKey_Refined" {
    Properties {
        _MainTex ("Base (RGB)", 2D) = "white" {}
        _ColorToReplace ("Color to Replace", Color) = (0,1,0,1)
        _Threshold ("Threshold", Range(0, 1)) = 0.4
        _Smoothing ("Smoothing", Range(0, 1)) = 0.1
        _WhiteBoost ("White Protection", Range(0, 1)) = 0.5
    }
    SubShader {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _ColorToReplace;
            float _Threshold;
            float _Smoothing;
            float _WhiteBoost;

            v2f vert (appdata v) {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 col = tex2D(_MainTex, i.uv);
                
                // Calculamos qué tan cerca está el color del verde
                float diff = distance(col.rgb, _ColorToReplace.rgb);
                
                // Calculamos la luminosidad del píxel (qué tan blanco es)
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                
                // Si el píxel es muy brillante (blanco), reducimos la fuerza del recorte
                float finalThreshold = _Threshold * (1.0 - (luminance * _WhiteBoost));
                
                // Aplicamos un recorte suave para que los bordes no sean dentados
                float alpha = smoothstep(finalThreshold, finalThreshold + _Smoothing, diff);
                
                return fixed4(col.rgb, alpha);
            }
            ENDCG
        }
    }
}