Shader "Polyhedronization"
{
	Subshader
	{	
		Pass
		{
			Cull Off
			CGPROGRAM
			#pragma vertex VSMain
			#pragma fragment PSMain
			#pragma target 5.0

			static const float3 _Vertices[36] = // vertices of single cube, in local space
			{
				{ 0.5, -0.5,  0.5}, { 0.5,  0.5,  0.5}, {-0.5,  0.5,  0.5},
				{ 0.5, -0.5,  0.5}, {-0.5,  0.5,  0.5}, {-0.5, -0.5,  0.5},
				{ 0.5,  0.5,  0.5}, { 0.5,  0.5, -0.5}, {-0.5,  0.5, -0.5},
				{ 0.5,  0.5,  0.5}, {-0.5,  0.5, -0.5}, {-0.5,  0.5,  0.5},
				{ 0.5,  0.5, -0.5}, { 0.5, -0.5, -0.5}, {-0.5, -0.5, -0.5},
				{ 0.5,  0.5, -0.5}, {-0.5, -0.5, -0.5}, {-0.5,  0.5, -0.5},
				{ 0.5, -0.5, -0.5}, { 0.5, -0.5,  0.5}, {-0.5, -0.5,  0.5},
				{ 0.5, -0.5, -0.5}, {-0.5, -0.5,  0.5}, {-0.5, -0.5, -0.5},
				{-0.5, -0.5,  0.5}, {-0.5,  0.5,  0.5}, {-0.5,  0.5, -0.5},
				{-0.5, -0.5,  0.5}, {-0.5,  0.5, -0.5}, {-0.5, -0.5, -0.5},
				{ 0.5, -0.5, -0.5}, { 0.5,  0.5, -0.5}, { 0.5,  0.5,  0.5},
				{ 0.5, -0.5, -0.5}, { 0.5,  0.5,  0.5}, { 0.5, -0.5,  0.5},
			};

			float _Thickness;
			StructuredBuffer<float3> _StructuredBuffer;

			float Sphere (float3 p, float3 c, float r)
			{
				return length(p - c) - r;
			}

			float4 Raymarching (float3 ro, float3 rd, float3 center)
			{
				float3 worldPos = ro;
				for (int i = 0; i < 64; i++)
				{
					float t = Sphere(ro, center, 0.5 * _Thickness);
					if (t < 0.01) return float4(max(dot(normalize(_WorldSpaceLightPos0.xyz), normalize(worldPos - center)), 0.5).xxx, 1.0);
					ro += t * rd;
				}
				return (float4) 0;
			}

			float4 VSMain (uint id : SV_VertexID, uint instance : SV_InstanceID, out float3 worldPos : WORLDPOS, out float3 offset : OFFSET) : SV_POSITION
			{
				offset = _StructuredBuffer[instance];
				worldPos = _Vertices[id] * _Thickness + offset;
				return UnityObjectToClipPos(float4(worldPos, 1.0));
			}

			float4 PSMain (float4 vertex : SV_POSITION, float3 worldPos : WORLDPOS, float3 offset : OFFSET) : SV_Target
			{
				float3 worldPosition = worldPos;
				float3 viewDirection = normalize(worldPos.xyz - _WorldSpaceCameraPos);
				float4 color = Raymarching (worldPosition, viewDirection, offset);
				if (color.a < 0.001) discard;
				return color;
			}
			ENDCG
		}
	}
}