// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Logo"
{
	Properties
	{
		_ParallexRainbow("Parallex Rainbow", Float) = -1.78
		_Parallax("Parallax", Float) = -1.79
		_TilingandoffsetShiny("Tiling and offset Shiny", Vector) = (0.1,0.76,-1.055,0)
		_Float1("Float 1", Float) = 0.5
		_PowerRainbowMask("PowerRainbowMask", Float) = 9
		_TilingandoffsetRainbow("Tiling and offset Rainbow", Vector) = (0.22,0.99,0.33,0)
		_GlowStrengtrh("Glow Strengtrh", Float) = 1
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_Smoothness("Smoothness", 2D) = "white" {}
		_Metallic("Metallic", 2D) = "white" {}
		_SmoothPower("SmoothPower", Float) = 1
		_Metallicpower("Metallic power", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGINCLUDE
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float3 worldNormal;
			INTERNAL_DATA
			float3 worldPos;
			float2 uv_texcoord;
			float3 viewDir;
		};

		uniform float4 _TilingandoffsetShiny;
		uniform float _Parallax;
		uniform float _Float1;
		uniform float _PowerRainbowMask;
		uniform float _GlowStrengtrh;
		uniform float4 _TilingandoffsetRainbow;
		uniform float _ParallexRainbow;
		uniform sampler2D _TextureSample0;
		uniform float4 _TextureSample0_ST;
		uniform sampler2D _Metallic;
		uniform float4 _Metallic_ST;
		uniform float _Metallicpower;
		uniform sampler2D _Smoothness;
		uniform float4 _Smoothness_ST;
		uniform float _SmoothPower;


		struct Gradient
		{
			int type;
			int colorsLength;
			int alphasLength;
			float4 colors[8];
			float2 alphas[8];
		};


		Gradient NewGradient(int type, int colorsLength, int alphasLength, 
		float4 colors0, float4 colors1, float4 colors2, float4 colors3, float4 colors4, float4 colors5, float4 colors6, float4 colors7,
		float2 alphas0, float2 alphas1, float2 alphas2, float2 alphas3, float2 alphas4, float2 alphas5, float2 alphas6, float2 alphas7)
		{
			Gradient g;
			g.type = type;
			g.colorsLength = colorsLength;
			g.alphasLength = alphasLength;
			g.colors[ 0 ] = colors0;
			g.colors[ 1 ] = colors1;
			g.colors[ 2 ] = colors2;
			g.colors[ 3 ] = colors3;
			g.colors[ 4 ] = colors4;
			g.colors[ 5 ] = colors5;
			g.colors[ 6 ] = colors6;
			g.colors[ 7 ] = colors7;
			g.alphas[ 0 ] = alphas0;
			g.alphas[ 1 ] = alphas1;
			g.alphas[ 2 ] = alphas2;
			g.alphas[ 3 ] = alphas3;
			g.alphas[ 4 ] = alphas4;
			g.alphas[ 5 ] = alphas5;
			g.alphas[ 6 ] = alphas6;
			g.alphas[ 7 ] = alphas7;
			return g;
		}


		float4 SampleGradient( Gradient gradient, float time )
		{
			float3 color = gradient.colors[0].rgb;
			UNITY_UNROLL
			for (int c = 1; c < 8; c++)
			{
			float colorPos = saturate((time - gradient.colors[c-1].w) / ( 0.00001 + (gradient.colors[c].w - gradient.colors[c-1].w)) * step(c, (float)gradient.colorsLength-1));
			color = lerp(color, gradient.colors[c].rgb, lerp(colorPos, step(0.01, colorPos), gradient.type));
			}
			#ifndef UNITY_COLORSPACE_GAMMA
			color = half3(GammaToLinearSpaceExact(color.r), GammaToLinearSpaceExact(color.g), GammaToLinearSpaceExact(color.b));
			#endif
			float alpha = gradient.alphas[0].x;
			UNITY_UNROLL
			for (int a = 1; a < 8; a++)
			{
			float alphaPos = saturate((time - gradient.alphas[a-1].y) / ( 0.00001 + (gradient.alphas[a].y - gradient.alphas[a-1].y)) * step(a, (float)gradient.alphasLength-1));
			alpha = lerp(alpha, gradient.alphas[a].x, lerp(alphaPos, step(0.01, alphaPos), gradient.type));
			}
			return float4(color, alpha);
		}


		void surf( Input i , inout SurfaceOutputStandard o )
		{
			o.Normal = float3(0,0,1);
			float4 appendResult7 = (float4(_TilingandoffsetShiny.x , _TilingandoffsetShiny.y , 0.0 , 0.0));
			float4 appendResult4 = (float4(_TilingandoffsetShiny.z , _TilingandoffsetShiny.w , 0.0 , 0.0));
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = Unity_SafeNormalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float3 ase_worldTangent = WorldNormalVector( i, float3( 1, 0, 0 ) );
			float3 ase_worldBitangent = WorldNormalVector( i, float3( 0, 1, 0 ) );
			float3x3 ase_worldToTangent = float3x3( ase_worldTangent, ase_worldBitangent, ase_worldNormal );
			float3 ase_tanViewDir = mul( ase_worldToTangent, ase_worldViewDir );
			float2 uv_TexCoord13 = i.uv_texcoord * appendResult7.xy + ( appendResult4 + float4( ( _Parallax * ase_tanViewDir ) , 0.0 ) ).xy;
			float temp_output_17_0 = ( uv_TexCoord13.x * _Float1 );
			Gradient gradient20 = NewGradient( 0, 6, 2, float4( 1, 0, 0.06209469, 0 ), float4( 1, 0.3683609, 0, 0.1588312 ), float4( 1, 0.8970642, 0, 0.3558862 ), float4( 0, 1, 0.01689792, 0.5411765 ), float4( 0, 0.3510606, 1, 0.7617609 ), float4( 0.8665185, 0, 1, 1 ), 0, 0, float2( 1, 0 ), float2( 1, 1 ), 0, 0, 0, 0, 0, 0 );
			float4 appendResult15 = (float4(_TilingandoffsetRainbow.x , _TilingandoffsetRainbow.y , 0.0 , 0.0));
			float4 appendResult12 = (float4(_TilingandoffsetRainbow.z , _TilingandoffsetRainbow.w , 0.0 , 0.0));
			float2 uv_TexCoord18 = i.uv_texcoord * appendResult15.xy + ( appendResult12 + float4( ( _ParallexRainbow * i.viewDir ) , 0.0 ) ).xy;
			float2 uv_TextureSample0 = i.uv_texcoord * _TextureSample0_ST.xy + _TextureSample0_ST.zw;
			float4 lerpResult32 = lerp( float4( 0,0,0,0 ) , ( pow( saturate( temp_output_17_0 ) , _PowerRainbowMask ) * ( _GlowStrengtrh * SampleGradient( gradient20, uv_TexCoord18.x ) ) ) , tex2D( _TextureSample0, uv_TextureSample0 ));
			o.Emission = lerpResult32.rgb;
			float2 uv_Metallic = i.uv_texcoord * _Metallic_ST.xy + _Metallic_ST.zw;
			o.Metallic = ( tex2D( _Metallic, uv_Metallic ) * _Metallicpower ).r;
			float2 uv_Smoothness = i.uv_texcoord * _Smoothness_ST.xy + _Smoothness_ST.zw;
			o.Smoothness = ( tex2D( _Smoothness, uv_Smoothness ) * _SmoothPower ).r;
			o.Alpha = 1;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.viewDir = IN.tSpace0.xyz * worldViewDir.x + IN.tSpace1.xyz * worldViewDir.y + IN.tSpace2.xyz * worldViewDir.z;
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18900
0;172;1748;525;2697.238;441.0093;1.410866;True;True
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;1;-2286.251,114.5788;Inherit;True;Tangent;True;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.Vector4Node;2;-2310.619,-297.9869;Inherit;False;Property;_TilingandoffsetShiny;Tiling and offset Shiny;2;0;Create;True;0;0;0;False;0;False;0.1,0.76,-1.055,0;-0.22,1,-1.06,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;3;-2289.251,35.57888;Inherit;False;Property;_Parallax;Parallax;1;0;Create;True;0;0;0;False;0;False;-1.79;-1.79;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.DynamicAppendNode;4;-2016.25,-157.4212;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-2020.25,39.57888;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.Vector4Node;8;-1851.94,402.4604;Inherit;False;Property;_TilingandoffsetRainbow;Tiling and offset Rainbow;5;0;Create;True;0;0;0;False;0;False;0.22,0.99,0.33,0;0.61,0.99,0.33,0;0;5;FLOAT4;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ViewDirInputsCoordNode;10;-1852.94,730.4601;Inherit;True;Tangent;False;0;4;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3
Node;AmplifyShaderEditor.RangedFloatNode;9;-1775.94,627.4601;Inherit;False;Property;_ParallexRainbow;Parallex Rainbow;0;0;Create;True;0;0;0;False;0;False;-1.78;-1.78;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleAddOpNode;6;-1807.25,-124.4211;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;7;-2052.25,-371.4212;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;11;-1421.94,667.4601;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT3;0,0,0;False;1;FLOAT3;0
Node;AmplifyShaderEditor.DynamicAppendNode;12;-1519.94,493.4604;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.SimpleAddOpNode;16;-1276.94,505.4604;Inherit;False;2;2;0;FLOAT4;0,0,0,0;False;1;FLOAT3;0,0,0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.DynamicAppendNode;15;-1520.94,345.4604;Inherit;False;FLOAT4;4;0;FLOAT;0;False;1;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;1;FLOAT4;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;13;-1635.25,-300.4211;Inherit;True;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;14;-1570.25,-9.421125;Inherit;False;Property;_Float1;Float 1;3;0;Create;True;0;0;0;False;0;False;0.5;0.5;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GradientNode;20;-1127.645,238.7233;Inherit;False;0;6;2;1,0,0.06209469,0;1,0.3683609,0,0.1588312;1,0.8970642,0,0.3558862;0,1,0.01689792,0.5411765;0,0.3510606,1,0.7617609;0.8665185,0,1,1;1,0;1,1;0;1;OBJECT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;18;-1132.94,401.4604;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;-1399.25,-262.4212;Inherit;True;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;24;-756.3684,198.5947;Inherit;False;Property;_GlowStrengtrh;Glow Strengtrh;6;0;Create;True;0;0;0;False;0;False;1;8.63;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SaturateNode;21;-912.8389,-242.4212;Inherit;True;1;0;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;22;-965.2499,-16.42113;Inherit;False;Property;_PowerRainbowMask;PowerRainbowMask;4;0;Create;True;0;0;0;False;0;False;9;1.38;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.GradientSampleNode;23;-832.8525,348.0023;Inherit;True;2;0;OBJECT;;False;1;FLOAT;0;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-523.3683,268.5947;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PowerNode;26;-741.2499,-208.4212;Inherit;False;False;2;0;FLOAT;0;False;1;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;29;-273.7988,323.9555;Inherit;True;Property;_TextureSample0;Texture Sample 0;7;0;Create;True;0;0;0;False;0;False;-1;7d0550e2463c8f642918c5399c710187;7d0550e2463c8f642918c5399c710187;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;35;157.0476,572.7627;Inherit;False;Property;_SmoothPower;SmoothPower;10;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;36;18.04761,653.7627;Inherit;True;Property;_Metallic;Metallic;9;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;27;-407.4166,67.15279;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;39;147.0476,864.7627;Inherit;False;Property;_Metallicpower;Metallic power;11;0;Create;True;0;0;0;False;0;False;1;1;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;33;18.04761,382.7627;Inherit;True;Property;_Smoothness;Smoothness;8;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;38;336.0476,721.7627;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;34;333.0476,509.7627;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;32;29.91602,102.8369;Inherit;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FunctionNode;19;-1125.25,-271.4212;Inherit;True;Triangle Wave;-1;;1;51ec3c8d117f3ec4fa3742c3e00d535b;0;1;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;589.5228,278.5638;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;Logo;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;True;0;False;Opaque;;Geometry;All;14;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;4;0;2;3
WireConnection;4;1;2;4
WireConnection;5;0;3;0
WireConnection;5;1;1;0
WireConnection;6;0;4;0
WireConnection;6;1;5;0
WireConnection;7;0;2;1
WireConnection;7;1;2;2
WireConnection;11;0;9;0
WireConnection;11;1;10;0
WireConnection;12;0;8;3
WireConnection;12;1;8;4
WireConnection;16;0;12;0
WireConnection;16;1;11;0
WireConnection;15;0;8;1
WireConnection;15;1;8;2
WireConnection;13;0;7;0
WireConnection;13;1;6;0
WireConnection;18;0;15;0
WireConnection;18;1;16;0
WireConnection;17;0;13;1
WireConnection;17;1;14;0
WireConnection;21;0;17;0
WireConnection;23;0;20;0
WireConnection;23;1;18;1
WireConnection;25;0;24;0
WireConnection;25;1;23;0
WireConnection;26;0;21;0
WireConnection;26;1;22;0
WireConnection;27;0;26;0
WireConnection;27;1;25;0
WireConnection;38;0;36;0
WireConnection;38;1;39;0
WireConnection;34;0;33;0
WireConnection;34;1;35;0
WireConnection;32;1;27;0
WireConnection;32;2;29;0
WireConnection;19;1;17;0
WireConnection;0;2;32;0
WireConnection;0;3;38;0
WireConnection;0;4;34;0
ASEEND*/
//CHKSM=54813D955D342561BE015CC4DC98E085DF7C5827