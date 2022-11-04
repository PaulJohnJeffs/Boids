using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquareMeshGenerator : MonoBehaviour
{
	[SerializeField, Min(1)]
	private int _iterations;

	[SerializeField]
	private ComputeShader _computeShader;
	[SerializeField]
	private MeshFilter _meshFilter;

	[SerializeField, Min(2)]
	private int _vertsPerSide = 2;
	[SerializeField, Min(1)]
	private float _sideLength = 1;

	[SerializeField, Min(0)]
	private float _noiseAmplitude = 1f;

	private Vector3[] _verts;
	private int[] _tris;

	//public void ApplyPerlinNoise()
	//{
	//	for (int j = 0; j < _iterations; j++)
	//	{
	//		float randXOffset = Random.value * 99999f;
	//		float randYOffset = Random.value * 99999f;

	//		for (int i = 0; i < _verts.Length; i++)
	//		{
	//			Vector3 vector = _verts[i];
	//			float perlin = Mathf.PerlinNoise(vector.x + randXOffset, vector.z + randYOffset) * _noiseAmplitude;

	//			_verts[i] = new Vector3(vector.x, perlin, vector.z);
	//		}
	//	}

	//	UpdateMesh();
	//}

	public void ApplyStaticNoiseCPU()
	{
		if (_vertData == null)
			GenerateMesh();

		for (int j = 0; j < _iterations; j++)
		{
			for (int i = 0; i < _verts.Length; i++)
			{
				Vector3 vector = _verts[i];
				_verts[i] = new Vector3(vector.x, Random.Range(0, _noiseAmplitude), vector.z);
			}
		}

		UpdateMesh();
	}

	public struct Vert
	{
		public Vector3 Position;
	}

	private Vert[] _vertData;

	public void ApplyStaticNoiseGPU()
	{
		if (_vertData == null)
			GenerateMesh();

		ComputeBuffer vertBuffer = new ComputeBuffer(_vertData.Length, sizeof(float) * 3);
		vertBuffer.SetData(_vertData);

		_computeShader.SetBuffer(0, "verts", vertBuffer);
		_computeShader.SetFloat("Seed", Random.value);
		_computeShader.SetInt("VertsPerSide", _vertsPerSide);
		_computeShader.SetInt("Iterations", _iterations);
		_computeShader.Dispatch(0, _vertsPerSide / 1, _vertsPerSide / 1, 1);

		vertBuffer.GetData(_vertData);

		for (int i = 0; i < _verts.Length; i++)
		{
			_verts[i] = _vertData[i].Position;
		}

		vertBuffer.Dispose();
		UpdateMesh();
	}

	public void GenerateMesh()
	{
		int numVerts = _vertsPerSide * _vertsPerSide;
		_verts = new Vector3[numVerts];
		_vertData = new Vert[_vertsPerSide * _vertsPerSide];

		int numBaseVerts = (_vertsPerSide - 1) * (_vertsPerSide - 1);
		int numTris = numBaseVerts * 2 * 3;
		_tris = new int[numTris];

		float a = _sideLength / (_vertsPerSide - 1);

		float xPos;
		float yPos;

		for (int y = 0; y < _vertsPerSide; y++)
		{
			yPos = y * a;

			for (int x = 0; x < _vertsPerSide; x++)
			{
				xPos = x * a;

				_verts[x + (y * _vertsPerSide)] = new Vector3(xPos, 0f, yPos);
				_vertData[x + (y * _vertsPerSide)] = new Vert { Position = _verts[x + (y * _vertsPerSide)] };
			}
		}

		int idx = 0;

		for (int y = 0; y < _vertsPerSide - 1; y++)
		{
			for (int x = 0; x < _vertsPerSide - 1; x++)
			{
				int bottomLeft = x + (y * _vertsPerSide);
				int bottomRight = bottomLeft + 1;
				int topLeft = bottomLeft + _vertsPerSide;
				int topRight = topLeft + 1;

				_tris[idx++] = bottomLeft;
				_tris[idx++] = topRight;
				_tris[idx++] = bottomRight;

				_tris[idx++] = bottomLeft;
				_tris[idx++] = topLeft;
				_tris[idx++] = topRight;
			}
		}

		UpdateMesh();
	}

	private void UpdateMesh()
	{
		Mesh mesh = new Mesh()
		{
			vertices = _verts,
			triangles = _tris,
		};

		mesh.RecalculateNormals();
		_meshFilter.mesh = mesh;
	}

	//private void DrawVerts()
	//{
	//	foreach (Vector3 v in _verts)
	//	{
	//		Gizmos.color = Color.magenta;
	//		Gizmos.DrawSphere(v, 0.01f);
	//	}
	//}

	//private void OnDrawGizmos()
	//{
	//	//GenerateMesh();
	//	//DrawVerts();
	//}
}
