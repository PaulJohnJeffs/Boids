using UnityEngine;

public abstract class VectorField : MonoBehaviour
{
	public abstract Vector3 Evaluate(Vector3 pos);
}
