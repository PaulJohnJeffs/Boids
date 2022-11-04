using UnityEngine;

public class ToroidalField : VectorField
{
	[SerializeField]
	private Vector3 _localAxis; // Field points along axis in the centre of the ring
	[SerializeField, Min(0)]
	private float _radius;

	public override Vector3 Evaluate(Vector3 worldPos)
	{
		Vector3 localPos = transform.InverseTransformPoint(worldPos);

		if (localPos.x == 0 && localPos.z == 0)
			return _localAxis.normalized;

		Vector3 nearestPointOnRing = new Vector3(localPos.x, 0, localPos.z);
		nearestPointOnRing = nearestPointOnRing.normalized * _radius;

		Vector3 ringToPos = localPos - nearestPointOnRing;
		Vector3 clockwise = Vector3.Cross(nearestPointOnRing.normalized, _localAxis);
		Vector3 fieldDirection = Vector3.Cross(ringToPos, clockwise);

		return transform.TransformVector(fieldDirection).normalized;
	}

	[SerializeField, Min(2)]
	private int _debugResolution;
	[SerializeField, Min(0)]
	private float _debugScale;

	public void OnDrawGizmosSelected()
	{
		float step = _debugScale / (_debugResolution - 1);
		for (int i = 0; i < _debugResolution; i++)
		{
			float x = (i * step) - _debugScale / 2f;
			for (int j = 0; j < _debugResolution; j++)
			{
				float y = (j * step) - _debugScale / 2f;
				for (int k = 0; k < _debugResolution; k++)
				{
					float z = (k * step) - _debugScale / 2f;
					Vector3 pos = new Vector3(x, y, z);
					Vector3 dir = Evaluate(transform.TransformPoint(pos));
					Gizmos.color = Color.magenta;
					Gizmos.DrawRay(transform.TransformPoint(pos), dir);
				}
			}
		}
	}
}
