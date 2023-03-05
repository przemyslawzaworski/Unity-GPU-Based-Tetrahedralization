using UnityEngine;
using UnityEngine.Rendering;
using System.Runtime.InteropServices;

public class Tetrahedralization : MonoBehaviour
{
	[SerializeField] ComputeShader _ComputeShader;
	[SerializeField] Shader _TetrahedralizationShader;
	[SerializeField] Shader _PolyhedronizationShader;
	[SerializeField] Shader _VoxelizationShader;
	[SerializeField] [Range(32, 128)] int _Resolution = 128;
	[SerializeField] [Range(4, 10000)] int _SeedCount = 10000;
	[SerializeField] [Range( 0.0f, 0.4f)] float _Thickness = 0.4f;
	[SerializeField] [Range(10.0f, 100f)] float _LightIntensity = 35f;
	[SerializeField] [Range(10.0f, 100f)] float _Scale = 50f;
	[SerializeField] [Range(-0.51f, 0.51f)] float _SliceMin = -0.5f, _SliceMax = 0.5f;
	[SerializeField] bool _Animation = true;
	[SerializeField] bool _VoronoiEdges = true;
	[SerializeField] bool _DelaunayEdges = true;
	ComputeBuffer _Seeds, _Voxels, _Vertices, _Triangles, _IndirectBuffer;
	Material _PolyhedronizationMaterial, _TetrahedralizationMaterial, _VoxelizationMaterial;
	RenderTexture[] _RenderTextures = new RenderTexture[2];
	int _CVID, _BVID, _JFID, _PKID, _TKID;
	bool _Swap = true;

	struct Seed
	{
		public Vector3 Location;
		public Vector3 Color;
	};

	struct Triangle
	{
		public Vector3 A;
		public Vector3 B;
		public Vector3 C;
	}

	void CreateMaterials()
	{
		_VoxelizationMaterial = new Material(_VoxelizationShader);
		_PolyhedronizationMaterial = new Material(_PolyhedronizationShader);
		_TetrahedralizationMaterial = new Material(_TetrahedralizationShader);
	}

	void CreateRenderTextures()
	{
		RenderTextureDescriptor descriptor = new RenderTextureDescriptor(_Resolution, _Resolution, RenderTextureFormat.ARGBFloat);
		descriptor.dimension = TextureDimension.Tex3D;
		descriptor.volumeDepth = _Resolution;
		for (int i = 0; i < 2; i++)
		{
			_RenderTextures[i] = new RenderTexture(descriptor);
			_RenderTextures[i].enableRandomWrite = true;
			_RenderTextures[i].Create();
			_RenderTextures[i].filterMode = FilterMode.Point;
		}
	}

	void CreateComputeBuffers()
	{
		Seed[] seeds = new Seed[_SeedCount];
		for (int i = 0; i < seeds.Length; i++)
		{
			int x = Random.Range(0, _Resolution);
			int y = Random.Range(0, _Resolution);
			int z = Random.Range(0, _Resolution);
			float r = Random.Range(0f, 1f);
			float g = Random.Range(0f, 1f);
			float b = Random.Range(0f, 1f);
			seeds[i] = new Seed{Location = new Vector3(x, y, z), Color = new Vector3(r, g, b)};
		}
		_Seeds = new ComputeBuffer(seeds.Length, Marshal.SizeOf(typeof(Seed)), ComputeBufferType.Default);
		_Seeds.SetData(seeds);
		_Voxels = new ComputeBuffer(_Resolution * _Resolution * _Resolution, sizeof(float) * 4, ComputeBufferType.Default);
		_Vertices = new ComputeBuffer(seeds.Length * 2048, Marshal.SizeOf(typeof(Vector3)), ComputeBufferType.Append);
		_Triangles = new ComputeBuffer(seeds.Length * 2048, Marshal.SizeOf(typeof(Triangle)), ComputeBufferType.Append);
		_IndirectBuffer = new ComputeBuffer (4, sizeof(int), ComputeBufferType.IndirectArguments);
	}

	void CreateKernels()
	{
		_CVID = _ComputeShader.FindKernel("ClearVoxelsKernel");
		_BVID = _ComputeShader.FindKernel("BuildVoxelsKernel");
		_JFID = _ComputeShader.FindKernel("JumpFloodingKernel");
		_PKID = _ComputeShader.FindKernel("PolyhedronizationKernel");
		_TKID = _ComputeShader.FindKernel("TetrahedralizationKernel");
	}

	void RunComputeShader()
	{
		_ComputeShader.SetInt("_Resolution", _Resolution);
		_ComputeShader.SetInt("_Animation", System.Convert.ToInt32(_Animation));
		_ComputeShader.SetInt("_VoronoiEdges", System.Convert.ToInt32(_VoronoiEdges));
		_ComputeShader.SetFloat("_MaxSteps", Mathf.Log((float)_Resolution, 2.0f));
		_ComputeShader.SetFloat("_Time", Time.time);
		_ComputeShader.SetFloat("_Scale", _Scale);
		_ComputeShader.SetBuffer(_CVID, "_Voxels", _Voxels);
		_ComputeShader.Dispatch(_CVID, _Resolution / 8, _Resolution / 8, _Resolution / 8);
		_ComputeShader.SetBuffer(_BVID, "_Seeds", _Seeds);
		_ComputeShader.SetBuffer(_BVID, "_Voxels", _Voxels);
		_ComputeShader.Dispatch(_BVID, (_Seeds.count + 8) / 8, 1, 1);
		int frameCount = 0;
		for (int i = 0; i < _Resolution; i++)
		{
			_ComputeShader.SetInt("_Frame", frameCount);
			int r = System.Convert.ToInt32(!_Swap);
			int w = System.Convert.ToInt32(_Swap);
			_ComputeShader.SetTexture(_JFID, "_Texture3D", _RenderTextures[r]);
			_ComputeShader.SetTexture(_JFID, "_RWTexture3D", _RenderTextures[w]);
			_ComputeShader.SetBuffer(_JFID, "_Voxels", _Voxels);
			_ComputeShader.Dispatch(_JFID, _Resolution / 8, _Resolution / 8, _Resolution / 8);
			_Swap = !_Swap;
			frameCount++;
		}
		_Vertices.SetCounterValue(0);
		_ComputeShader.SetBuffer(_PKID, "_Vertices", _Vertices);
		_ComputeShader.SetTexture(_PKID,"_Texture3D", _RenderTextures[System.Convert.ToInt32(!_Swap)]);
		_ComputeShader.Dispatch(_PKID, _Resolution / 8, _Resolution / 8, _Resolution / 8);
		_Triangles.SetCounterValue(0);
		_ComputeShader.SetBuffer(_TKID, "_Seeds", _Seeds);
		_ComputeShader.SetBuffer(_TKID, "_Triangles", _Triangles);
		_ComputeShader.SetTexture(_TKID,"_Texture3D", _RenderTextures[System.Convert.ToInt32(!_Swap)]);
		_ComputeShader.Dispatch(_TKID, _Resolution / 8, _Resolution / 8, _Resolution / 8);
	}

	void VoxelizationDrawCall()
	{
		_VoxelizationMaterial.SetInt("_Resolution", _Resolution);
		_VoxelizationMaterial.SetFloat("_Scale", _Scale);
		_VoxelizationMaterial.SetFloat("_SliceMin", _SliceMin);
		_VoxelizationMaterial.SetFloat("_SliceMax", _SliceMax);
		_VoxelizationMaterial.SetBuffer("_Seeds", _Seeds);
		_VoxelizationMaterial.SetTexture("_Texture3D", _RenderTextures[System.Convert.ToInt32(!_Swap)]);
		_VoxelizationMaterial.SetPass(0);
		Graphics.DrawProceduralNow(MeshTopology.Triangles, 36, _Resolution * _Resolution * _Resolution);
	}

	void PolyhedronizationDrawCall()
	{
		int[] args = new int[] { 0, 0, 0, 0 };
		ComputeBuffer.CopyCount(_Vertices, _IndirectBuffer, 0);
		_IndirectBuffer.GetData(args);
		int instanceCount = args[0];
		_PolyhedronizationMaterial.SetFloat("_Thickness", _Thickness);
		_PolyhedronizationMaterial.SetBuffer("_StructuredBuffer", _Vertices);
		_PolyhedronizationMaterial.SetPass(0);
		Graphics.DrawProceduralNow(MeshTopology.Triangles, 36, instanceCount);
	}

	void TetrahedralizationDrawCall()
	{
		int[] args = new int[] { 0, 0, 0, 0 };
		ComputeBuffer.CopyCount(_Triangles, _IndirectBuffer, 0);
		_IndirectBuffer.GetData(args);
		int vertexCount = args[0] * 12;
		_TetrahedralizationMaterial.SetInt("_DelaunayEdges", System.Convert.ToInt32(_DelaunayEdges));
		_TetrahedralizationMaterial.SetFloat("_LightIntensity", _LightIntensity);
		_TetrahedralizationMaterial.SetBuffer("_TriangleBuffer", _Triangles);
		_TetrahedralizationMaterial.SetPass(0);
		Graphics.DrawProceduralNow(MeshTopology.Triangles, vertexCount, 1);
	}

	void Start()
	{
		CreateMaterials();
		CreateRenderTextures();
		CreateComputeBuffers();
		CreateKernels();
	}

	void OnRenderObject()
	{
		RunComputeShader();
		VoxelizationDrawCall();
		PolyhedronizationDrawCall();
		TetrahedralizationDrawCall();
	}

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.Space))
		{
			CameraController cameraController = this.gameObject.GetComponent<CameraController>();
			if (cameraController)
			{
				cameraController.enabled = !cameraController.enabled;
			}
		}
	}

	void OnDestroy()
	{
		if (_VoxelizationMaterial != null) Destroy(_VoxelizationMaterial);
		if (_PolyhedronizationMaterial != null) Destroy(_PolyhedronizationMaterial);
		if (_TetrahedralizationMaterial != null) Destroy(_TetrahedralizationMaterial);
		if (_Seeds != null) _Seeds.Release();
		if (_Voxels != null) _Voxels.Release();
		if (_Vertices != null) _Vertices.Release();
		if (_Triangles != null) _Triangles.Release();
		if (_IndirectBuffer != null) _IndirectBuffer.Release();
		for (int i = 0; i < 2; i++) if (_RenderTextures[i] != null) _RenderTextures[i].Release();
	}
}