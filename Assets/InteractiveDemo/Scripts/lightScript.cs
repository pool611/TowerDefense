using UnityEngine;
using System.Collections;

public class lightScript : MonoBehaviour 
{
	bool Impact = false;
	public float Sqr;
	// Use this for initialization
	void Start () 
	{
		Impact = true;
		gameObject.light.intensity = 7;
		Sqr = gameObject.light.intensity * gameObject.light.intensity * (( gameObject.light.intensity < 0.0f ) ? -1.0f : 1.0f);
	}
	
	// Update is called once per frame
	void Update () 
	{
		if (Impact)
		{
			gameObject.light.intensity -= (1.0f / Time.deltaTime) * Sqr * .0001f;
			if (gameObject.light.intensity <= 0)
			{
				Destroy (gameObject);
			}
		}
	}
}