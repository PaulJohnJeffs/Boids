using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : MonoBehaviour
{
	[SerializeField]
	private float _speed;
	[SerializeField]
	private Vector3 _direction;

	private void Awake()
	{
		if (_direction.sqrMagnitude == 0)
			_direction = Vector3.forward;

		_direction = _direction.normalized;
	}

	private void Update()
	{
		transform.position += _speed * Time.deltaTime * _direction;
	}
}
