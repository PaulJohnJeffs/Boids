using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSCounter : MonoBehaviour
{
	[SerializeField]
	private TMPro.TextMeshProUGUI _fpsText;

    void Update()
    {
		int fps = Mathf.RoundToInt(1f / Time.deltaTime);
		_fpsText.text = fps.ToString();
    }
}
