Shader "Voxelization"
{
	Subshader
	{	
		Pass
		{
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

			struct Seed
			{
				float3 Location;
				float3 Color;
			};

			uint _Resolution;
			float _SliceMin, _SliceMax, _Scale;
			StructuredBuffer<Seed> _Seeds;
			Texture3D _Texture3D;

			float4 VSMain (uint id : SV_VertexID, uint instance : SV_InstanceID, out float3 color : TEXCOORD0) : SV_POSITION
			{
				float3 uvw = float3(instance % _Resolution, (instance / _Resolution) % _Resolution, instance / (_Resolution * _Resolution));
				float4 voxel = _Texture3D.Load(int4(uvw, 0));
				color = _Seeds[int(floor(voxel.a))].Color;
				float scale = _Scale / float(_Resolution);
				float4 worldPos = float4(_Vertices[id] * scale + (uvw - 0.5 * _Resolution + 0.5) * scale, 1.0);
				bool culling = ((worldPos.y / _Scale) < _SliceMin) || ((worldPos.y / _Scale) > _SliceMax);
				return culling ? asfloat(0x7fc00000) : UnityObjectToClipPos(worldPos);
			}

			float4 PSMain (float4 vertex : SV_POSITION, float3 color : TEXCOORD0) : SV_Target
			{
				return float4(color, 1.0);
			}
			ENDCG
		}
	}
}