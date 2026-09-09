Shader "Reborn/DistrictSky"
{
 Properties { _Top("Zenith",Color)=(.08,.19,.3,1) _Bottom("Horizon",Color)=(.48,.62,.69,1) }
 SubShader { Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" } Cull Off ZWrite Off
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct v2f { float4 p:SV_POSITION; float3 d:TEXCOORD0; };
 float4 _Top,_Bottom;
 v2f vert(float4 v:POSITION) { v2f o;o.p=UnityObjectToClipPos(v);o.d=v.xyz;return o; }
 float hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
 float noise(float2 p) {
 float2 a=floor(p),b=frac(p);b=b*b*(3-2*b);
 return lerp(lerp(hash(a),hash(a+float2(1,0)),b.x),lerp(hash(a+float2(0,1)),hash(a+1),b.x),b.y);
 }
 float terrain(float2 p) {
 float n=0,a=.5;
 for(int k=0;k<5;k++){n+=a*noise(p);p=p*2.03+float2(7.1,13.4);a*=.5;}return n;
 }
 fixed4 frag(v2f i):SV_Target {
 float3 d=normalize(i.d);float t=saturate(d.y);
 float3 c=lerp(_Bottom.rgb,_Top.rgb,pow(t,.45));
 float3 moon=normalize(float3(-.42,.38,.86));float disc=smoothstep(.970,.972,dot(d,moon));
 float3 u=normalize(cross(float3(0,1,0),moon)),v=cross(moon,u);
 float2 uv=float2(dot(d,u),dot(d,v))/.235;
 float relief=terrain(uv*11);
 float sphere=sqrt(saturate(1-dot(uv,uv)));
 float lighting=.35+.65*saturate(sphere*.8-uv.x*.4+uv.y*.25);
 float3 surface=lerp(float3(.42,.49,.55),float3(.82,.86,.85),smoothstep(.2,.8,relief))*lighting;
 c=lerp(c,surface,disc*.78);
 float cloud=terrain(d.xz/max(.16,d.y)*1.35+float2(12.4,5.1));
 float coverage=smoothstep(.50,.74,cloud)*smoothstep(.015,.22,d.y)*.48;
 c=lerp(c,float3(.74,.79,.81),coverage);
 float haze=pow(saturate(1-abs(d.y-.07)),25);c+=float3(.08,.07,.04)*haze;
 return float4(c,1);
 }
 ENDCG }
 }
}
