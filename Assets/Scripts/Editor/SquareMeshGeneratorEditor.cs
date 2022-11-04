using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SquareMeshGenerator))]
public class SquareMeshGeneratorEditor : Editor
{
	private SquareMeshGenerator _meshGenerator;

	private void OnEnable()
	{
		_meshGenerator = (SquareMeshGenerator)target;
	}

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();

		if (GUILayout.Button("Generate Mesh"))
		{
			_meshGenerator.GenerateMesh();
		}

		//if (GUILayout.Button("Apply Perlin Noise CPU"))
		//{
		//	_meshGenerator.ApplyPerlinNoise();
		//}

		if (GUILayout.Button("Apply Static Noise CPU"))
		{
			_meshGenerator.ApplyStaticNoiseCPU();
		}

		if (GUILayout.Button("Apply Static Noise GPU"))
		{
			_meshGenerator.ApplyStaticNoiseGPU();
		}
	}
}
