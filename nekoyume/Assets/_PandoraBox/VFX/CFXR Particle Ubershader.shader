//--------------------------------------------------------------------------------------------------------------------------------
// Cartoon FX
// (c) 2012-2020 Jean Moreno
//--------------------------------------------------------------------------------------------------------------------------------

Shader "Cartoon FX/Remaster/Particle Ubershader"
{
	Properties
	{
	//# Blending
	//#
		[Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Blend Source", Float) = 5
		[Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Blend Destination", Float) = 10
		[KeywordEnumNoPrefix(Alpha Blending, _ALPHABLEND_ON, Alpha Blending Premultiplied, _ALPHAPREMULTIPLY_ON, Multiplicative, _ALPHAMODULATE_ON, Additive, _CFXR_ADDITIVE)] _BlendingType ("Blending Type", Float) = 0

	//# 
		[ToggleNoKeyword] _ZWrite ("Depth Write", Float) = 0
		[Toggle(_ALPHATEST_ON)] _UseAlphaClip ("Alpha Clipping (Cutout)", Float) = 0
	//# IF_KEYWORD _ALPHATEST_ON
		_Cutoff ("Cutoff Threshold", Range(0.001,1)) = 0.1
	//# END_IF
	
	//# --------------------------------------------------------
	
		[Toggle(_FADING_ON)] _UseSP ("Soft Particles", Float) = 0
	//# IF_KEYWORD _FADING_ON
		_SoftParticlesFadeDistanceNear ("Near Fade", Float) = 0
		_SoftParticlesFadeDistanceFar ("Far Fade", Float) = 1
	//# END_IF

	//# 

		[Toggle(_CFXR_EDGE_FADING)] _UseEF ("Edge Fade", Float) = 0
	//# IF_KEYWORD _CFXR_EDGE_FADING
		_EdgeFadePow ("Edge Fade Power", Float) = 1
	//# END_IF

	//# 

	//# ========================================================

	//# Effects
	//#

		[Toggle(_CFXR_DISSOLVE)] _UseDissolve ("Enable Dissolve", Float) = 0
	//# IF_KEYWORD _CFXR_DISSOLVE
		[NoScaleOffset] _DissolveTex ("Dissolve Texture", 2D) = "gray" {}
		_DissolveSmooth ("Dissolve Smoothing", Range(0.0001,0.5)) = 0.1
		[ToggleNoKeyword] _InvertDissolveTex ("Invert Dissolve Texture", Float) = 0
	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_CFXR_UV_DISTORTION)] _UseUVDistortion ("Enable UV Distortion", Float) = 0
	//# IF_KEYWORD _CFXR_UV_DISTORTION
		
		[NoScaleOffset] _DistortTex ("Distortion Texture", 2D) = "gray" {}
		_DistortScrolling ("Scroll (XY) Tile (ZW)", Vector) = (0,0,1,1)
		[Toggle(_CFXR_UV2_DISTORTION)] _UseUV2Distortion ("Use UV2", Float) = 0
		_Distort ("Distortion Strength", Range(0,2.0)) = 0.1
		[ToggleNoKeyword] _FadeAlongU ("Fade along Y", Float) = 0
		[Toggle(_CFXR_UV_DISTORTION_ADD)] _UVDistortionAdd ("Add to base UV", Float) = 0
	//# END_IF

	//# ========================================================

	//# Colors
	//#

		[NoScaleOffset] _MainTex ("Texture", 2D) = "white" {}
		[Toggle(_CFXR_SINGLE_CHANNEL)] _SingleChannel ("Single Channel Texture", Float) = 0

	//# --------------------------------------------------------

		[KeywordEnum(Off,1x,2x)] _CFXR_OVERLAYTEX ("Enable Overlay Texture", Float) = 0
	//# IF_KEYWORD _CFXR_OVERLAYTEX_1X || _CFXR_OVERLAYTEX_2X
		[KeywordEnum(RGBA,RGB,A)] _CFXR_OVERLAYBLEND ("Overlay Blend Channels", Float) = 0
		[NoScaleOffset] _OverlayTex ("Overlay Texture", 2D) = "white" {}
		_OverlayTex_Scroll ("Overlay Scrolling / Scale", Vector) = (0.1,0.1,1,1)
	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_FLIPBOOK_BLENDING)] _UseFB ("Flipbook Blending", Float) = 0

	//# --------------------------------------------------------

		[Toggle(_CFXR_SECONDCOLOR_LERP)] _UseSecondColor ("Secondary Vertex Color (TEXCOORD2)", Float) = 0
	//# IF_KEYWORD _CFXR_SECONDCOLOR_LERP
		[NoScaleOffset] _SecondColorTex ("Second Color Map", 2D) = "black" {}
		_SecondColorSmooth ("Second Color Smoothing", Range(0.0001,0.5)) = 0.2
	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_CFXR_FONT_COLORS)] _UseFontColor ("Use Font Colors", Float) = 0

//	//# --------------------------------------------------------
//
//	[Toggle(_CFXR_GRADIENTMAP)] _UseGradientMap ("Gradient Map", Float) = 0
//	//# IF_KEYWORD _CFXR_GRADIENTMAP
//		[NoScaleOffset] _GradientMap ("Gradient Map", 2D) = "black" {}
//	//# END_IF

	//# --------------------------------------------------------

		[Toggle(_CFXR_HDR_BOOST)] _HdrBoost ("Enable HDR Multiplier", Float) = 0
	//# IF_KEYWORD _CFXR_HDR_BOOST
		 _HdrMultiply ("HDR Multiplier", Float) = 2
	//# END_IF

	//# --------------------------------------------------------
	
	//# Lighting
	//#

		[KeywordEnumNoPrefix(Off, _, Direct, _CFXR_LIGHTING_DIRECT, Indirect, _CFXR_LIGHTING_INDIRECT, Both, _CFXR_LIGHTING_ALL)] _UseLighting ("Mode", Float) = 0
	//# IF_KEYWORD _CFXR_LIGHTING_DIRECT || _CFXR_LIGHTING_ALL
		_DirectLightingRamp ("Direct Lighting Ramp", Range(0,1)) = 1.0
	//# END_IF
	//# 
	//# IF_KEYWORD _CFXR_LIGHTING_DIRECT || _CFXR_LIGHTING_INDIRECT || _CFXR_LIGHTING_ALL
		[Toggle(_NORMALMAP)] _UseNormalMap ("Enable Normal Map", Float) = 0
	//# IF_KEYWORD _NORMALMAP
		[NoScaleOffset] _BumpMap ("Normal Map", 2D) = "bump" {}
		_BumpScale ("Normal Scale", Range(-1, 1)) = 1.0
	//# END_IF
	//# 
		[Toggle(_EMISSION)] _UseEmission ("Enable Emission (TEXCOORD2)", Float) = 0
	//# 
		[Toggle(_CFXR_LIGHTING_WPOS_OFFSET)] _UseLightingWorldPosOffset ("Enable World Pos. Offset", Float) = 0
	//# IF_KEYWORD _CFXR_LIGHTING_WPOS_OFFSET
		_LightingWorldPosStrength ("Offset Strength", Range(0,1)) = 0.2
	//# END_IF
	//# 
		[Toggle(_CFXR_LIGHTING_BACK)] _UseBackLighting ("Enable Backlighting", Float) = 0
	//# IF_KEYWORD _CFXR_LIGHTING_BACK
		_DirLightScreenAtten ("Dir. Light Screen Attenuation", Range(0, 5)) = 1.0
		_BacklightTransmittance ("Backlight Transmittance", Range(0, 2)) = 1.0
	//# END_IF
	//# 
	//# IF_KEYWORD _CFXR_LIGHTING_INDIRECT || _CFXR_LIGHTING_ALL
		_IndirectLightingMix ("Indirect Lighting Mix", Range(0,1)) = 0.5
	//# END_IF
		_ShadowColor ("Shadow Color", Color) = (0,0,0,1)
	//# 
	//# END_IF

	//# ========================================================
	//# Shadows
	//#

		[KeywordEnum(Off,On,CustomTexture)] _CFXR_DITHERED_SHADOWS ("Dithered Shadows", Float) = 0
	//# IF_KEYWORD _CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
		_ShadowStrength		("Shadows Strength Max", Range(0,1)) = 1.0
		//#	IF_KEYWORD _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
		_DitherCustom		("Dithering 3D Texture", 3D) = "black" {}
		//#	END_IF
	//# END_IF

//		_ReceivedShadowsStrength ("Received Shadows Strength", Range(0,1)) = 0.5
	}
	
	Category
	{
		Tags
		{
			"Queue"="Transparent"
			"IgnoreProjector"="True"
			"RenderType"="Transparent"
			"PreviewType"="Plane"
		}

		Blend [_SrcBlend] [_DstBlend], One One
		ZWrite [_ZWrite]
		Cull  Off

		//================================================================================================================================
		// Includes & Code

		CGINCLUDE

			#if defined(UNITY_SAMPLE_FULL_SH_PER_PIXEL)
				#undef UNITY_SAMPLE_FULL_SH_PER_PIXEL
			#endif
			#define UNITY_SAMPLE_FULL_SH_PER_PIXEL defined(_NORMALMAP)

			#include "UnityCG.cginc"
			#include "UnityStandardUtils.cginc"
			#include "AutoLight.cginc"
			
			#if CFXR_URP
				#include "CFXR_URP.cginc"
			#else
				#include "UnityLightingCommon.cginc"
			#endif

			#if defined(_CFXR_LIGHTING_INDIRECT) || defined(_CFXR_LIGHTING_DIRECT) || defined(_CFXR_LIGHTING_ALL)
				#define LIGHTING
			#endif
			#if defined(_CFXR_LIGHTING_DIRECT) || defined(_CFXR_LIGHTING_ALL)
				#define LIGHTING_DIRECT
			#endif
			#if defined(_CFXR_LIGHTING_INDIRECT) || defined(_CFXR_LIGHTING_ALL)
				#define LIGHTING_INDIRECT
			#endif
			#if (defined(_CFXR_LIGHTING_DIRECT) || defined(_CFXR_LIGHTING_ALL)) && defined(_CFXR_LIGHTING_BACK)
				#define LIGHTING_BACK
			#endif

			#include "CFXR_SETTINGS.cginc"

			// --------------------------------

			CBUFFER_START(UnityPerMaterial)

			sampler2D _MainTex;
			sampler2D _OverlayTex;
			float4 _OverlayTex_Scroll;

			sampler2D _BumpMap;
			half _BumpScale;

			float4 _GameObjectWorldPosition;
			half _LightingWorldPosStrength;
			half _IndirectLightingMix;
			half4 _ShadowColor;
			half _DirectLightingRamp;
			half _DirLightScreenAtten;
			half _BacklightTransmittance;

			sampler2D _DissolveTex;
			half _InvertDissolveTex;
			half _DissolveSmooth;

			sampler2D _DistortTex;
			half4 _DistortScrolling;
			half _Distort;
			half _FadeAlongU;

			// sampler2D _GradientMap;

			sampler2D _SecondColorTex;
			half _SecondColorSmooth;

			half _HdrMultiply;

			half _ReceivedShadowsStrength;

			half _Cutoff;

			UNITY_DECLARE_DEPTH_TEXTURE(_CameraDepthTexture);
			half _SoftParticlesFadeDistanceNear;
			half _SoftParticlesFadeDistanceFar;
			half _EdgeFadePow;

			float _ShadowStrength;
			sampler3D _DitherMaskLOD;
			sampler3D _DitherCustom;
			float4 _DitherCustom_TexelSize;

			CBUFFER_END

			// --------------------------------
			// Input/output

			struct appdata
			{
				float4 vertex		: POSITION;
				half4 color			: COLOR;
				float4 texcoord		: TEXCOORD0;	//xy = uv, zw = random
				float4 texcoord1	: TEXCOORD1;	//additional particle data: x = dissolve, y = animFrame
				float4 texcoord2	: TEXCOORD2;	//additional particle data: second color
		#if PASS_SHADOW_CASTER || _CFXR_EDGE_FADING || defined(LIGHTING)
				float3 normal       : NORMAL;
		#endif
		#if defined(LIGHTING) && _NORMALMAP
				float4 tangent : TANGENT;
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			// vertex to fragment
			struct v2f
			{
				float4 pos				: SV_POSITION;
				half4 color				: COLOR;
				float4 uv_random		: TEXCOORD0;	//uv + particle data
				float4 custom1			: TEXCOORD1;	//additional particle data
		#if _CFXR_SECONDCOLOR_LERP || _CFXR_FONT_COLORS || (defined(LIGHTING) && _EMISSION)
				float4 secondColor		: TEXCOORD2;
		#endif
		#if !defined(GLOBAL_DISABLE_SOFT_PARTICLES) && ((defined(SOFTPARTICLES_ON) || defined(CFXR_URP) || defined(SOFT_PARTICLES_ORTHOGRAPHIC)) && defined(_FADING_ON))
				float4 projPos			: TEXCOORD3;
		#endif
		#if defined(LIGHTING_DIRECT) || (defined(LIGHTING_INDIRECT) && _NORMALMAP)
				float3 worldPos : TEXCOORD4;
		#endif
		#if defined(LIGHTING_INDIRECT) && !_NORMALMAP
				float3 shColor : TEXCOORD5;
		#endif
		#if defined(LIGHTING_DIRECT) && defined(LIGHTING_BACK)
				float3 viewDirWS : TEXCOORD6;
		#endif
		#if defined(_CFXR_LIGHTING_WPOS_OFFSET) && (defined(LIGHTING_DIRECT) || defined(LIGHTING_INDIRECT))
				float3 worldPosDiff : TEXCOORD7;
		#endif
		#if !PASS_SHADOW_CASTER
				UNITY_FOG_COORDS(8)		//note: does nothing if fog is not enabled
				// SHADOW_COORDS(8)
		#endif
		#if defined(LIGHTING)
				float3 normalWS : NORMAL;
			#if _NORMALMAP
				float4 tangentWS : TANGENT;
			#endif
		#endif
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// vertex to fragment (shadow caster)
			struct v2f_shadowCaster
			{
				V2F_SHADOW_CASTER_NOPOS
				half4 color				: COLOR;
				float4 uv_random		: TEXCOORD1;	//uv + particle data
				float4 custom1			: TEXCOORD2;	//additional particle data
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};

			// --------------------------------

			#include "CFXR.cginc"

			// --------------------------------
			// Vertex

		#if PASS_SHADOW_CASTER
			void vertex_program (appdata v, out v2f_shadowCaster o, out float4 opos : SV_POSITION)
		#else
			v2f vertex_program (appdata v)
		#endif
			{
		#if !PASS_SHADOW_CASTER
				v2f o;
		#endif

				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_TRANSFER_INSTANCE_ID(v, o);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

		#if !PASS_SHADOW_CASTER
				o.pos = UnityObjectToClipPos(v.vertex);

			#if defined(LIGHTING_DIRECT) || (defined(LIGHTING_INDIRECT) && _NORMALMAP)
				// Particle Systems already output their vertex position in world space
				o.worldPos = v.vertex.xyz;

				#if defined(LIGHTING_BACK)
					o.viewDirWS = normalize(_WorldSpaceCameraPos.xyz - o.worldPos);
					//o.viewDirWS = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));
				#endif
			#endif

			#if defined(LIGHTING) && defined(_CFXR_LIGHTING_WPOS_OFFSET)
				o.worldPosDiff = _GameObjectWorldPosition.xyz - v.vertex.xyz;
			#endif
		#endif

				o.color = v.color;
				o.uv_random = v.texcoord;
				o.custom1 = v.texcoord1;
		#if _CFXR_SECONDCOLOR_LERP || _CFXR_FONT_COLORS || (defined(LIGHTING) && _EMISSION)
				o.secondColor = v.texcoord2;
		#endif
		#if defined(LIGHTING)
				o.normalWS = v.normal.xyz;
			#if _NORMALMAP
				o.tangentWS = v.tangent;
			#endif
		#endif

				// Ambient Lighting / Light Probes, per-vertex if no normal map
		#if defined(LIGHTING_INDIRECT) && !_NORMALMAP
				half3 shColor = ShadeSHPerVertex(v.normal.xyz, half3(0,0,0));
				o.shColor = shColor;
		#endif

				/*
		#if !PASS_SHADOW_CASTER
				// Shadows Receiving
				TRANSFER_SHADOW(o);
		#endif
				*/

		#if PASS_SHADOW_CASTER
				vert(v, o, opos);
		#else
				return vert(v, o);
		#endif
			}

			// --------------------------------
			// Shade4PointLights Custom

			float3 CFXR_Shade4PointLights (
			float4 lightPosX, float4 lightPosY, float4 lightPosZ,
			float3 lightColor0, float3 lightColor1, float3 lightColor2, float3 lightColor3,
			float4 lightAttenSq,
			float3 pos, float3 normal)
			{
				// to light vectors
				float4 toLightX = lightPosX - pos.x;
				float4 toLightY = lightPosY - pos.y;
				float4 toLightZ = lightPosZ - pos.z;
				// squared lengths
				float4 lengthSq = 0;
				lengthSq += toLightX * toLightX;
				lengthSq += toLightY * toLightY;
				lengthSq += toLightZ * toLightZ;
				// don't produce NaNs if some vertex position overlaps with the light
				lengthSq = max(lengthSq, 0.000001);

				// NdotL
				float4 ndotl = 0;
				ndotl += toLightX * normal.x;
				ndotl += toLightY * normal.y;
				ndotl += toLightZ * normal.z;
				ndotl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, ndotl * 0.5 + 0.5);

				// correct NdotL
				float4 corr = rsqrt(lengthSq);
				ndotl = max (float4(0,0,0,0), ndotl * corr);

				// attenuation
				float4 atten = 1.0 / (1.0 + lengthSq * lightAttenSq);
				float4 diff = ndotl * atten;
				// final color
				float3 col = 0;
				col += lightColor0 * diff.x;
				col += lightColor1 * diff.y;
				col += lightColor2 * diff.z;
				col += lightColor3 * diff.w;
				return col;
			}

			// --------------------------------
			// Fragment

		#if PASS_SHADOW_CASTER
			float4 fragment_program (v2f_shadowCaster i, UNITY_VPOS_TYPE vpos : VPOS) : SV_Target
		#else
			half4 fragment_program (v2f i) : SV_Target
		#endif
			{
				UNITY_SETUP_INSTANCE_ID(i);
				UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

				// ================================================================
				// UV Distortion

			#if _CFXR_UV_DISTORTION
				#if _CFXR_UV2_DISTORTION
					float2 uvDistortion = tex2D(_DistortTex, (i.custom1.xy * _DistortScrolling.zw + i.uv_random.zw + frac(_DistortScrolling.xy * _Time.yy))).rg * 2.0 - 1.0;
				#else
					float2 uvDistortion = tex2D(_DistortTex, (i.uv_random.xy * _DistortScrolling.zw + i.uv_random.zw + frac(_DistortScrolling.xy * _Time.yy))).rg * 2.0 - 1.0;
				#endif

				#if _CFXR_UV_DISTORTION_ADD
					uvDistortion = i.uv_random.xy + uvDistortion * _Distort;
				#else
					uvDistortion = lerp(i.uv_random.xy, uvDistortion, _Distort);
				#endif

				if (_FadeAlongU > 0)
				{
					uvDistortion = lerp(i.uv_random.xy, uvDistortion, i.uv_random.y * 0.5);
				}

				#define main_uv uvDistortion
			#else
				#define main_uv i.uv_random
			#endif

				// ================================================================
				// Color & Alpha

			#if _CFXR_SINGLE_CHANNEL
				half4 mainTex = half4(1, 1, 1, tex2D(_MainTex, main_uv.xy).r);
			#else
				half4 mainTex = tex2D(_MainTex, main_uv.xy);
			#endif

			#ifdef _FLIPBOOK_BLENDING
				#if _CFXR_SINGLE_CHANNEL
					half4 mainTex2 = tex2D(_MainTex, i.uv_random.zw).r;
				#else
					half4 mainTex2 = tex2D(_MainTex, i.uv_random.zw);
				#endif
					mainTex = lerp(mainTex, mainTex2, i.custom1.y);
			#endif

			#if _CFXR_OVERLAYTEX_1X
				float2 timeOffset = frac(_Time.yy * _OverlayTex_Scroll.xy);
				float2 overlayUv = ((i.uv_random.xy + i.uv_random.zw) * _OverlayTex_Scroll.zz) + timeOffset;
				half4 overlay = tex2D(_OverlayTex, overlayUv).r;
			#elif _CFXR_OVERLAYTEX_2X
				float2 timeOffset = frac(_Time.yy * _OverlayTex_Scroll.xy);
				float2 overlayUv = ((i.uv_random.xy + i.uv_random.zw) * _OverlayTex_Scroll.zz) + timeOffset;
				half4 overlay = tex2D(_OverlayTex, overlayUv).r;

				overlayUv = ((i.uv_random.xy + i.uv_random.wz) * _OverlayTex_Scroll.ww) + timeOffset;
				half4 overlay2 = tex2D(_OverlayTex, overlayUv).r;
				mainTex.a *= (overlay.r + overlay2.r) / 2.0;
			#endif

			#if _CFXR_OVERLAYTEX_1X || _CFXR_OVERLAYTEX_2X
				#if _CFXR_OVERLAYBLEND_A
				mainTex.a *= overlay.r;
				#elif _CFXR_OVERLAYBLEND_RGB
				mainTex.rgb *= overlay.rgb;
				#else
				mainTex.rgba *= overlay.rgba;
				#endif
			#endif
					
				/*
			#if _CFXR_GRADIENTMAP
				mainTex.rgb = tex2D(_GradientMap, mainTex.a).rgb;
			#endif
				*/

			#if _CFXR_FONT_COLORS
				half3 particleColor = mainTex.b * i.color.rgb + mainTex.g * i.custom1.rgb + mainTex.r * i.secondColor.rgb;
				half particleAlpha = mainTex.a * i.color.a;
			#else
				half3 particleColor = mainTex.rgb * i.color.rgb;
				half particleAlpha = mainTex.a * i.color.a;
			#endif
				
			#if _CFXR_SECONDCOLOR_LERP
				half secondColorMap = tex2D(_SecondColorTex, i.uv_random.xy).r;
				float time = lerp(-_SecondColorSmooth, 1+_SecondColorSmooth, i.secondColor.a);
				secondColorMap = smoothstep(secondColorMap - _SecondColorSmooth, secondColorMap + _SecondColorSmooth, time);
				particleColor.rgb += i.secondColor.rgb * secondColorMap;
			#endif

			#if _CFXR_HDR_BOOST
				#ifdef UNITY_COLORSPACE_GAMMA
					_HdrMultiply = LinearToGammaSpaceApprox(_HdrMultiply);
				#endif
				particleColor.rgb *= _HdrMultiply * GLOBAL_HDR_MULTIPLIER;
			#endif

				/*
			#if !PASS_SHADOW_CASTER
				// Shadows Receiving
				half shadows = SHADOW_ATTENUATION(i);
				particleColor.rgb *= saturate(shadows + (1.0 - _ReceivedShadowsStrength));
			#endif
				*/

				// ================================================================
				// Lighting

		#if !PASS_SHADOW_CASTER
		
			#if defined(LIGHTING)
				half3 particleLighting = half3(0, 0, 0);
				
				#if CFXR_URP
					#define UNPACK_SCALE_NORMAL UnpackNormalScale
					float3 mainLightDir = _MainLightPosition.xyz;
					float3 mainLightColor = _MainLightColor.rgb;
				#else
					#define UNPACK_SCALE_NORMAL UnpackScaleNormal
					float3 mainLightDir = _WorldSpaceLightPos0.xyz;
					float3 mainLightColor = _LightColor0.rgb;
				#endif
			
				#if _NORMALMAP
					half3 normalTS = UnpackScaleNormal_CFXR(tex2D(_BumpMap, main_uv.xy), _BumpScale);
					// tangent space to world space:
					float sgn = i.tangentWS.w;      // should be either +1 or -1
					float3 bitangent = sgn * cross(i.normalWS.xyz, i.tangentWS.xyz);
					float3 normalWS = mul(normalTS, half3x3(i.tangentWS.xyz, bitangent.xyz, i.normalWS.xyz));
				#else
					half3 normalWS = i.normalWS;
				#endif
				#if defined(_CFXR_LIGHTING_WPOS_OFFSET)
					normalWS = normalize(normalWS.xyz - i.worldPosDiff.xyz * _LightingWorldPosStrength);
				#endif
			#endif

				// - Direct
			#if defined(LIGHTING_DIRECT)
				// Main Directional
				half ndl = dot(normalWS, mainLightDir);
				ndl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, ndl * 0.5 + 0.5);
				half mainDirectLighting = max(0, ndl);

				#if defined(LIGHTING_BACK)
					half viewAtten = length(mainLightDir  + i.viewDirWS);
					viewAtten = 1 - smoothstep(0, _DirLightScreenAtten, viewAtten);
					mainDirectLighting += (viewAtten * viewAtten) * _BacklightTransmittance;
				#endif

				// particleColor.rgb = lerp(_ShadowColor.rgb, particleColor.rgb * mainLightColor.rgb, mainDirectLighting);
				particleColor.rgb *= lerp(_ShadowColor.rgb, mainLightColor.rgb, mainDirectLighting);

				// Point Lights
				#if defined(ENABLE_POINT_LIGHTS)
					#if CFXR_URP
						uint additionalLightsCount = GetAdditionalLightsCount();
						for (uint lightIndex = 0u; lightIndex < additionalLightsCount; ++lightIndex)
						{
							Light light = GetAdditionalLight(lightIndex, i.worldPos);
							half ndl = dot(normalWS, light.direction);
							ndl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, ndl * 0.5 + 0.5);
							ndl = max(0, ndl);

						#if defined(LIGHTING_BACK)
							half backNdl = dot(-normalWS, light.direction);
							backNdl = smoothstep(0.5 - _DirectLightingRamp * 0.5, 0.5 + _DirectLightingRamp * 0.5, backNdl * 0.5 + 0.5);
							ndl += max(0, backNdl) * _BacklightTransmittance;
						#endif

							particleColor.rgb += ndl * light.color.rgb * light.distanceAttenuation;
						}
					#else
						half3 pointLights = CFXR_Shade4PointLights(
							unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							unity_4LightAtten0, i.worldPos, normalWS);

						#if defined(LIGHTING_BACK)
							// TODO Tweak CFXR_Shade4PointLights to handle both front & back lights more efficiently?
							pointLights += CFXR_Shade4PointLights(
							unity_4LightPosX0, unity_4LightPosY0, unity_4LightPosZ0,
							unity_LightColor[0].rgb, unity_LightColor[1].rgb, unity_LightColor[2].rgb, unity_LightColor[3].rgb,
							unity_4LightAtten0, i.worldPos, -normalWS) * _BacklightTransmittance;
						#endif

						particleColor.rgb += pointLights;
					#endif
				#endif
			#endif

				// - Indirect
			#if defined(LIGHTING_INDIRECT)
				#if _NORMALMAP
						half3 shColor = ShadeSHPerPixel(normalWS, half3(0,0,0), float3(0,0,0));
						particleColor.rgb += shColor.rgb * _IndirectLightingMix;
				#else
					particleColor.rgb += i.shColor.rgb * _IndirectLightingMix;
				#endif
			#endif

			#if defined(LIGHTING) && _EMISSION
				particleColor.rgb += i.secondColor.rgb;
			#endif

		#endif

				// ================================================================
				// Dissolve

			#if _CFXR_DISSOLVE
				half dissolveTex = tex2D(_DissolveTex, i.uv_random.xy).r;
				dissolveTex = _InvertDissolveTex <= 0 ? 1 - dissolveTex : dissolveTex;
				half dissolveTime = i.custom1.x;
			#else
				half dissolveTex = 0;
				half dissolveTime = 0;
			#endif

				// ================================================================
				//

			#if PASS_SHADOW_CASTER
				return frag(i, vpos, particleColor, particleAlpha, dissolveTex, dissolveTime);
			#else
				return frag(i, particleColor, particleAlpha, dissolveTex, dissolveTime);
			#endif
			}

		ENDCG

		//====================================================================================================================================
		// Universal Rendering Pipeline

		Subshader
		{
			Pass
			{
				Name "BASE_URP"
				Tags { "LightMode"="UniversalForward" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				//#pragma multi_compile_fwdbase
				//#pragma multi_compile SHADOWS_SCREEN

				#pragma multi_compile CFXR_URP
				
				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				// #pragma shader_feature_local _ _CFXR_GRADIENTMAP
				#pragma shader_feature_local _ _CFXR_SECONDCOLOR_LERP _CFXR_FONT_COLORS
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_EDGE_FADING
				#pragma shader_feature_local _ _CFXR_LIGHTING_DIRECT _CFXR_LIGHTING_INDIRECT _CFXR_LIGHTING_ALL
				#pragma shader_feature_local _ _CFXR_LIGHTING_WPOS_OFFSET
				#pragma shader_feature_local _ _CFXR_LIGHTING_BACK

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _NORMALMAP
				#pragma shader_feature_local _ _EMISSION
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE


				ENDCG
			}
			
			// Same as above with 'Universal2D' instead and DISABLE_SOFT_PARTICLES keyword
			Pass
			{
				Name "BASE_URP"
				Tags { "LightMode"="Universal2D" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				//#pragma multi_compile_fwdbase
				//#pragma multi_compile SHADOWS_SCREEN

				#pragma multi_compile CFXR_URP
				#pragma multi_compile DISABLE_SOFT_PARTICLES
				
				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				// #pragma shader_feature_local _ _CFXR_GRADIENTMAP
				#pragma shader_feature_local _ _CFXR_SECONDCOLOR_LERP _CFXR_FONT_COLORS
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_EDGE_FADING
				#pragma shader_feature_local _ _CFXR_LIGHTING_DIRECT _CFXR_LIGHTING_INDIRECT _CFXR_LIGHTING_ALL
				#pragma shader_feature_local _ _CFXR_LIGHTING_WPOS_OFFSET
				#pragma shader_feature_local _ _CFXR_LIGHTING_BACK

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _NORMALMAP
				#pragma shader_feature_local _ _EMISSION
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE


				ENDCG
			}

			//--------------------------------------------------------------------------------------------------------------------------------

			Pass
			{
				Name "ShadowCaster"
				Tags { "LightMode" = "ShadowCaster" }

				BlendOp Add
				Blend One Zero
				ZWrite On
				Cull Off

				CGPROGRAM

				#pragma multi_compile PASS_SHADOW_CASTER

				#pragma vertex vertex_program
				#pragma fragment fragment_program

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING

				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#pragma multi_compile_shadowcaster
				#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE

			#if _CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
				#pragma target 3.0		//needed for VPOS
			#endif

				ENDCG
			}
		}

		//====================================================================================================================================
		// Built-in Rendering Pipeline

		SubShader
		{
			Pass
			{
				Name "BASE"
				Tags { "LightMode"="ForwardBase" }

				CGPROGRAM

				#pragma vertex vertex_program
				#pragma fragment fragment_program
				
				#pragma target 2.0
				
				#pragma multi_compile_particles
				#pragma multi_compile_fog
				//#pragma multi_compile_fwdbase
				//#pragma multi_compile SHADOWS_SCREEN
				
				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				// #pragma shader_feature_local _ _CFXR_GRADIENTMAP
				#pragma shader_feature_local _ _CFXR_SECONDCOLOR_LERP _CFXR_FONT_COLORS
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _CFXR_HDR_BOOST
				#pragma shader_feature_local _ _CFXR_EDGE_FADING
				#pragma shader_feature_local _ _CFXR_LIGHTING_DIRECT _CFXR_LIGHTING_INDIRECT _CFXR_LIGHTING_ALL
				#pragma shader_feature_local _ _CFXR_LIGHTING_WPOS_OFFSET
				#pragma shader_feature_local _ _CFXR_LIGHTING_BACK

				// Using the same keywords as Unity's Standard Particle shader to minimize project-wide keyword usage
				#pragma shader_feature_local _ _NORMALMAP
				#pragma shader_feature_local _ _EMISSION
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING
				#pragma shader_feature_local _ _FADING_ON
				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE


				ENDCG
			}

			//--------------------------------------------------------------------------------------------------------------------------------

			Pass
			{
				Name "ShadowCaster"
				Tags { "LightMode" = "ShadowCaster" }

				BlendOp Add
				Blend One Zero
				ZWrite On
				Cull Off

				CGPROGRAM

				#pragma multi_compile PASS_SHADOW_CASTER

				#pragma vertex vertex_program
				#pragma fragment fragment_program

				#pragma shader_feature_local _ _CFXR_SINGLE_CHANNEL
				#pragma shader_feature_local _ _CFXR_DISSOLVE
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV2_DISTORTION
				#pragma shader_feature_local _ _CFXR_UV_DISTORTION_ADD
				#pragma shader_feature_local _ _CFXR_OVERLAYTEX_1X _CFXR_OVERLAYTEX_2X
				#pragma shader_feature_local _ _CFXR_OVERLAYBLEND_A _CFXR_OVERLAYBLEND_RGB
				#pragma shader_feature_local _ _FLIPBOOK_BLENDING

				#pragma shader_feature_local _ _ALPHATEST_ON
				#pragma shader_feature_local _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON _CFXR_ADDITIVE

				#pragma multi_compile_shadowcaster
				#pragma shader_feature_local _ _CFXR_DITHERED_SHADOWS_ON _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE

			#if _CFXR_DITHERED_SHADOWS_ON || _CFXR_DITHERED_SHADOWS_CUSTOMTEXTURE
				#pragma target 3.0		//needed for VPOS
			#endif

				ENDCG
			}
		}
	}
	
	CustomEditor "CartoonFX.MaterialInspector"
}

