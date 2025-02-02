Shader "Custom/PoCustomShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        // _Rotation: uv���� ���ο��� ȸ���� ���� (�� ����)
        _Rotation("Rotation", Range(0, 360)) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            // _MainTex_ST: (offset.x, offset.y, scale.x, scale.y)
            float4 _MainTex_ST;
            float _Rotation;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // ������ uv (��κ� 0~1) ����
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // uv ���� ���ο��� ȸ�� ���� (�߽��� 0.5, 0.5)
                if (_Rotation != 0)
                {
                    float rad = radians(_Rotation);
                    float cosA = cos(rad);
                    float sinA = sin(rad);
                    // �߽� �������� �̵� �� ȸ���ϰ� �ٽ� ����
                    uv = uv - 0.5;
                    uv = float2(uv.x * cosA - uv.y * sinA, uv.x * sinA + uv.y * cosA);
                    uv += 0.5;
                }

                // uv�� mainTextureOffset�� mainTextureScale ����:
                // ��, uv ����(0~1)�� ���� �ؽ�ó�� �κ� �������� ����
                uv = uv * _MainTex_ST.zw + _MainTex_ST.xy;

                return tex2D(_MainTex, uv);
            }
            ENDCG
        }
    }
}