Shader "Custom/ProximityGrid"
{
    Properties
    {
        _GridColor ("Grid Color", Color) = (0.5, 0.5, 0.5, 1)
        _CursorPosition ("Cursor Position", Vector) = (0, 0, 0, 0)
        _RevealRadius ("Reveal Radius", Float) = 0.5
        _FalloffDistance ("Falloff Distance", Float) = 0.2
        _MinOpacity ("Min Opacity", Float) = 0.1
        _MaxOpacity ("Max Opacity", Float) = 0.8
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            float4 _GridColor;
            float3 _CursorPosition;
            float _RevealRadius;
            float _FalloffDistance;
            float _MinOpacity;
            float _MaxOpacity;
            
            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.color = v.color;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
                
                // Calculate distance from cursor
                float dist = distance(i.worldPos, _CursorPosition);
                
                // Calculate alpha based on distance
                // Inside radius = MaxOpacity, outside = MinOpacity
                float alpha = lerp(_MaxOpacity, _MinOpacity, 
                    smoothstep(_RevealRadius, _RevealRadius + _FalloffDistance, dist));
                
                return float4(_GridColor.rgb, alpha);
            }
            ENDCG
        }
    }
}