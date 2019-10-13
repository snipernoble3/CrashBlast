using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity_Source : MonoBehaviour
{
	public float gravityStrength = -9.81f;
	
	public Vector3 GetGravityVector(Transform attractedObject)
	{
		return (attractedObject.position - transform.position).normalized * gravityStrength;
	}
	
	public void AttractObject(Transform attractedObject)
	{
		// Rotate the attracted object so that its downward direction faces the gravity source
		Vector3 gravityVector = GetGravityVector(attractedObject);
		attractedObject.rotation = Quaternion.FromToRotation(-attractedObject.up, gravityVector.normalized) * attractedObject.rotation;
		
		// Add force to the attracted object to simulate gravity toward the gravity source.
		attractedObject.gameObject.GetComponent<Rigidbody>().AddForce(gravityVector, ForceMode.Impulse);
	}
}
