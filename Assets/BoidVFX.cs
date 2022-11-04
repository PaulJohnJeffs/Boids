using UnityEngine;

public class BoidVFX : MonoBehaviour
{
	[SerializeField]
	private Renderer _renderer;
	private Material _material;

	private void Awake()
	{
		_renderer = GetComponentInChildren<Renderer>();
		_material = _renderer.material;
	}

	private void Update()
	{
		_material.SetVector("_Forward", transform.forward);
	}
}
