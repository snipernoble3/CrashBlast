using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class Gravity_AttractedObject : MonoBehaviour
{
	public Gravity_Source gravitySource;
	public float blendToNewSource = 1.0f;
	private float blendSpeed = 0.025f;
	
    void Awake()
    {
		// If we are sourcing gravity from a plant object, then don't apply Unity's default gravity.
		if (gravitySource != null) GetComponent<Rigidbody>().useGravity = false;
		else GetComponent<Rigidbody>().useGravity = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
		if (blendToNewSource != 1.0f) blendToNewSource = Mathf.Clamp(blendToNewSource + (blendSpeed * Time.fixedDeltaTime), 0.0f, 1.0f);
        if (gravitySource != null) gravitySource.AttractObject(transform, blendToNewSource);
    }
	
	public void SetGravitySource(Gravity_Source gravitySource)
	{
		this.gravitySource = gravitySource;
		GetComponent<Rigidbody>().useGravity = false;
	}
}
