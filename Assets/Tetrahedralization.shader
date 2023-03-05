Shader "Tetrahedralization"
{
	SubShader
	{
		Cull Off
		Pass
		{
			CGPROGRAM
			#pragma vertex VSMain
			#pragma fragment PSMain
			#pragma target 5.0

			struct Triangle
			{
				float3 Vertices[3];
			};

			int _DelaunayEdges;
			float _LightIntensity;
			StructuredBuffer<Triangle> _TriangleBuffer;

			float4 VSMain (float4 vertex : POSITION, uint id : SV_VertexID, out float2 barycentric : BARYCENTRIC, out float3 worldPos : TEXCOORD0) : SV_Position
			{
				uint index = id % 3u;
				worldPos = float3(_TriangleBuffer[id / 3u].Vertices[index]);
				barycentric = float2(fmod(index, 2.0), step(2.0, index));
				return UnityObjectToClipPos(float4(worldPos, 1.0));
			}

			float4 PSMain (float4 vertex : SV_POSITION, float2 barycentric : BARYCENTRIC, float3 worldPos : TEXCOORD0) : SV_Target
			{
				float3 coords = float3(barycentric, 1.0 - barycentric.x - barycentric.y);
				float3 df = fwidth(coords);
				float3 wireframe = smoothstep(df * 0.1, df * 0.1 + df, coords);
				if ((1.0 - min(wireframe.x, min(wireframe.y, wireframe.z))) < 0.01)
				{
					if (_DelaunayEdges == 1) discard;
					return (float4) 1.0;
				}
				float r = distance(worldPos, float3(cos(_Time.g) * 5.0 + sin(_Time.g) * 6.0, 0.0, -sin(_Time.g) * 7.0 + cos(_Time.g) * 8.0)) / _LightIntensity;
				float g = distance(worldPos, float3(0.0, cos(_Time.g) * 9.0 + sin(_Time.g) * 10.0, -sin(_Time.g) * 11.0 + cos(_Time.g) * 12.0)) / _LightIntensity;
				float b = distance(worldPos, float3(cos(_Time.g) * 13.0 + sin(_Time.g) * 14.0, -sin(_Time.g) * 15.0 + cos(_Time.g) * 16.0, 0.0)) / _LightIntensity;
				float3 rgb = 1.0 - float3(r, g, b);
				return float4(rgb, 1.0);
			}
			ENDCG
		}
	}
}