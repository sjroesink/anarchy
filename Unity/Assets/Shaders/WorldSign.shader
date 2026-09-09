Shader "Reborn/WorldSign"
{
    Properties { _MainTex ("Font atlas", 2D) = "white" {} }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Back
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            struct Input { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Output { float4 vertex:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            Output vert(Input input)
            {
                Output output;output.vertex=UnityObjectToClipPos(input.vertex);
                output.uv=input.uv;output.color=input.color;return output;
            }
            fixed4 frag(Output input):SV_Target
            {
                return fixed4(input.color.rgb,input.color.a*tex2D(_MainTex,input.uv).a);
            }
            ENDCG
        }
    }
}
