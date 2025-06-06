Shader "Custom/WhiteSilhouetteSprite"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Brightness ("Brightness", Range(0, 2)) = 1
        _Contrast ("Contrast", Range(0, 3)) = 1
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [HideInInspector] _AlphaTex ("External Alpha", 2D) = "white" {}
        [HideInInspector] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex SpriteVert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _Brightness;
            float _Contrast;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color * _Color;
                
                // Convert to grayscale using luminance formula
                float grayscale = dot(c.rgb, float3(0.299, 0.587, 0.114));
                
                // Apply contrast: (value - 0.5) * contrast + 0.5
                grayscale = (grayscale - 0.5) * _Contrast + 0.5;
                
                // Apply brightness
                grayscale *= _Brightness;
                
                // Clamp to 0-1 range
                grayscale = saturate(grayscale);
                
                // Invert the grayscale so dark becomes light
                float inverted = 1.0 - grayscale;
                
                // Use the inverted grayscale as alpha (dark pixels become opaque)
                float alpha = inverted * c.a;
                
                // Output tint color with calculated alpha
                c.rgb = _Color.rgb;
                c.a = alpha;
                
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}