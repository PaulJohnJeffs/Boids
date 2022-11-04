using UnityEngine;

public struct BoidData
{
	public Vector3 Position;
	public Vector3 Forward;
}

public class BoidSpawner : MonoBehaviour
{
	public const int BOID_DATA_SIZE = (sizeof(float) * 6);

	[SerializeField]
	private ComputeShader _computeShader;
	[SerializeField]
	private int _numBoids;
	[SerializeField]
	private float _range;

	[SerializeField]
	private float _sightRange;
	[SerializeField]
	private float _flockSpacing;
	[SerializeField]
	private float _evadeSpacing;

	[SerializeField]
	private float _speed;
	[SerializeField, Range(0, 100)]
	private float _evadeWeight = 1;
	[SerializeField, Range(0, 100)]
	private float _alignWeight = 1;
	[SerializeField, Range(0, 100)]
	private float _flockWeight = 1;

	[SerializeField]
	private Mesh _boidMesh;
	[SerializeField]
	private Material _boidMat;
	private RenderParams _renderParams;
	private ComputeBuffer _boidsCB;
	private ComputeBuffer _sortedBoidsCB;
	private ComputeBuffer _boidPartitionIndicesCB;
	private ComputeBuffer _boidsPerPartitionCB;
	private ComputeBuffer _prefixSumLastCB;
	private ComputeBuffer _prefixSumCurrentCB;

	private int _updateBoidsIdx;
	private int _partitionBoidsIdx;
	private int _sumPartitionsIdx;
	private int _sortBoidsIdx;
	private int _clearSumsIdx;

	private int _numPartitionsPerDimension;
	private int _numPartitions;

	private void Start()
	{
		Vector3[] verts = new Vector3[]
		{
			new Vector3(0.25f, 0f, 0f),
			new Vector3(-0.25f, 0f, 0f),
			new Vector3(0f, 1f, 0f),
		};

		int[] tris = new int[]
		{
			0, 1, 2,
		};

		_boidMesh = new Mesh()
		{
			vertices = verts,
			triangles = tris,
		};

		// Change the bounding area so that it is a multiple of the sight range.
		_numPartitionsPerDimension = Mathf.CeilToInt(_range / _sightRange);
		_numPartitions = (int)Mathf.Pow(_numPartitionsPerDimension, 3);
		_range = _numPartitionsPerDimension * _sightRange;

		// Create an array of boids with random positions and orientations
		BoidData[] data = new BoidData[_numBoids];
		for (int i = 0; i < _numBoids; i++)
		{
			Vector3 pos = new Vector3(Random.value, Random.value, Random.value) * _range;
			Vector3 look = Random.onUnitSphere;
			data[i] = new BoidData()
			{
				Position = pos,
				Forward = look,
			};
		}

		_boidsCB = new ComputeBuffer(_numBoids, BOID_DATA_SIZE);
		_boidsCB.SetData(data);
		_sortedBoidsCB = new ComputeBuffer(_numBoids, BOID_DATA_SIZE);
		_boidPartitionIndicesCB = new ComputeBuffer(_numBoids, 2 * sizeof(uint));
		_boidsPerPartitionCB = new ComputeBuffer(_numPartitions, sizeof(uint));
		_prefixSumLastCB = new ComputeBuffer(_numPartitions, sizeof(uint));
		_prefixSumCurrentCB = new ComputeBuffer(_numPartitions, sizeof(uint));

		_updateBoidsIdx = _computeShader.FindKernel("UpdateBoids");
		_computeShader.SetBuffer(_updateBoidsIdx, "Boids", _boidsCB);
		_computeShader.SetBuffer(_updateBoidsIdx, "SortedBoids", _sortedBoidsCB);
		_computeShader.SetBuffer(_updateBoidsIdx, "BoidPartitionIndices", _boidPartitionIndicesCB);
		_computeShader.SetBuffer(_updateBoidsIdx, "BoidsPerPartition", _boidsPerPartitionCB);
		_computeShader.SetBuffer(_updateBoidsIdx, "PrefixSumCurrent", _prefixSumCurrentCB);

		_partitionBoidsIdx = _computeShader.FindKernel("PartitionBoids");
		_computeShader.SetBuffer(_partitionBoidsIdx, "Boids", _boidsCB);
		_computeShader.SetBuffer(_partitionBoidsIdx, "BoidPartitionIndices", _boidPartitionIndicesCB);
		_computeShader.SetBuffer(_partitionBoidsIdx, "BoidsPerPartition", _boidsPerPartitionCB);

		_sumPartitionsIdx = _computeShader.FindKernel("SumPartitionCounts");

		_sortBoidsIdx = _computeShader.FindKernel("Sort");
		_computeShader.SetBuffer(_sortBoidsIdx, "Boids", _boidsCB);
		_computeShader.SetBuffer(_sortBoidsIdx, "SortedBoids", _sortedBoidsCB);
		_computeShader.SetBuffer(_sortBoidsIdx, "BoidPartitionIndices", _boidPartitionIndicesCB);
		_computeShader.SetBuffer(_sortBoidsIdx, "BoidsPerPartition", _boidsPerPartitionCB);
		_computeShader.SetBuffer(_sortBoidsIdx, "PrefixSumCurrent", _prefixSumCurrentCB);

		_clearSumsIdx = _computeShader.FindKernel("ClearSums");
		_computeShader.SetBuffer(_clearSumsIdx, "BoidsPerPartition", _boidsPerPartitionCB);
		_computeShader.SetBuffer(_clearSumsIdx, "PrefixSumCurrent", _prefixSumCurrentCB);
		_computeShader.SetBuffer(_clearSumsIdx, "PrefixSumLast", _prefixSumLastCB);

		_computeShader.SetFloat("SightRange", _sightRange);
		//_computeShader.SetFloat("SightDotMin", Mathf.Cos(_sightAngle));
		_computeShader.SetFloat("Bounds", _range);
		_computeShader.SetInt("NumBoids", _numBoids);
		_computeShader.SetInt("NumPartitionsPerDimension", _numPartitionsPerDimension);

		_renderParams = new RenderParams(_boidMat);
		_renderParams.matProps = new MaterialPropertyBlock();
		_renderParams.matProps.SetBuffer("_Boids", _boidsCB);
		_renderParams.matProps.SetBuffer("_BoidPartitions", _boidPartitionIndicesCB);
		_renderParams.worldBounds = new Bounds(Vector3.zero, Vector3.one * _range * 2f);
	}

	public void Update()
	{
		_computeShader.SetFloat("DeltaTime", Time.deltaTime);
		_computeShader.SetFloat("AlignWeight", _alignWeight);
		_computeShader.SetFloat("EvadeWeight", _evadeWeight);
		_computeShader.SetFloat("FlockWeight", _flockWeight);
		_computeShader.SetFloat("Speed", _speed);
		_computeShader.SetFloat("FlockSpacing", _flockSpacing);
		_computeShader.SetFloat("EvadeSpacing", _evadeSpacing);

		_computeShader.SetBuffer(_sumPartitionsIdx, "PrefixSumCurrent", _prefixSumCurrentCB);
		_computeShader.SetBuffer(_sumPartitionsIdx, "PrefixSumLast", _boidsPerPartitionCB);

		_computeShader.Dispatch(_clearSumsIdx, Mathf.CeilToInt((float)_numPartitions / 256), 1, 1);
		_computeShader.Dispatch(_partitionBoidsIdx, Mathf.CeilToInt((float)_numBoids / 256), 1, 1);

		bool odd = true;
		for (int n = 1; n < _numPartitions; n <<= 1)
		{
			_computeShader.SetInt("PrefixSumPower", n);
			_computeShader.Dispatch(_sumPartitionsIdx, Mathf.CeilToInt((float)_numPartitions / 256), 1, 1);

			_computeShader.SetBuffer(_sumPartitionsIdx, "PrefixSumLast", odd ? _prefixSumCurrentCB : _prefixSumLastCB);
			_computeShader.SetBuffer(_sumPartitionsIdx, "PrefixSumCurrent", odd ? _prefixSumLastCB : _prefixSumCurrentCB);

			odd = !odd;
		}

		_computeShader.SetBuffer(_sortBoidsIdx, "PrefixSumCurrent", !odd ? _prefixSumCurrentCB : _prefixSumLastCB);
		_computeShader.SetBuffer(_updateBoidsIdx, "PrefixSumCurrent", !odd ? _prefixSumCurrentCB : _prefixSumLastCB);

		_computeShader.Dispatch(_sortBoidsIdx, Mathf.CeilToInt((float)_numBoids / 256), 1, 1);

		_computeShader.Dispatch(_updateBoidsIdx, Mathf.CeilToInt((float)_numBoids / 256), 1, 1);

		Graphics.RenderMeshPrimitives(_renderParams, _boidMesh, 0, _numBoids);
	}

	private void OnDestroy()
	{
		_boidsCB.Release();
		_sortedBoidsCB.Release();
		_boidPartitionIndicesCB.Release();
		_boidsPerPartitionCB.Release();
		_prefixSumCurrentCB.Release();
		_prefixSumLastCB.Release();
	}
}
