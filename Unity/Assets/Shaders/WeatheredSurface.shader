Shader "Reborn/WeatheredSurface"
{
 Properties {
  _Color("Base",Color)=(.25,.3,.32,1)
  _Metallic("Metallic",Range(0,1))=0
  _Glossiness("Smoothness",Range(0,1))=.2
  _Weathering("Weathering",Range(0,1))=.4
 }
 SubShader {
  Tags { "RenderType"="Opaque" } LOD 200
  CGPROGRAM
  #pragma surface surf Standard fullforwardshadows
  #pragma target 3.0
  struct Input { float3 worldPos; float3 worldNormal; };
  fixed4 _Color;half _Metallic,_Glossiness,_Weathering;
  float hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
  float noise(float3 p){
   float3 a=floor(p),t=frac(p);t=t*t*(3-2*t);
   return lerp(lerp(lerp(hash(a),hash(a+float3(1,0,0)),t.x),lerp(hash(a+float3(0,1,0)),hash(a+float3(1,1,0)),t.x),t.y),
               lerp(lerp(hash(a+float3(0,0,1)),hash(a+float3(1,0,1)),t.x),lerp(hash(a+float3(0,1,1)),hash(a+1),t.x),t.y),t.z);
  }
  void surf(Input i,inout SurfaceOutputStandard o){
   float broad=noise(i.worldPos*.42),grain=noise(i.worldPos*32);
   float streak=noise(i.worldPos*float3(3,.12,3));
   float variation=lerp(streak,broad,saturate(abs(i.worldNormal.y)));
   float stain=smoothstep(.36,.8,variation)*_Weathering;
   o.Albedo=_Color.rgb*(.90+grain*.2)*(1-stain*.55);
   o.Albedo=lerp(o.Albedo,o.Albedo*float3(1.16,1.05,.88),stain*.5);
   o.Metallic=_Metallic;o.Smoothness=saturate(_Glossiness-stain*.15+(grain-.5)*.08);o.Alpha=1;
  }
  ENDCG
 }
 FallBack "Standard"
}
