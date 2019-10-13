using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof (Rigidbody))]
public class Gravity_AttractedObject : MonoBehaviour
{
	public Gravity_Source gravitySource;
	
    void Awake()
    {
		// If we are sourcing gravity from a plant object, then don't apply Unity's default gravity.
		if (gravitySource != null) GetComponent<Rigidbody>().useGravity = false;
		else GetComponent<Rigidbody>().useGravity = true;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (gravitySource != null) gravitySource.AttractObject(transform);
    }
	
	void SetGravitySource(Gravity_Source gravitySource)
	{
		this.gravitySource = gravitySource;
	}
}
