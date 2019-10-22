using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Gravity_Source : MonoBehaviour
{
	[SerializeField] private bool isRadial = true;
	[SerializeField] private float gravityStrength = -9.81f;
	public Vector3 nonRadialDirection;
	
	private void Awake()
	{
		nonRadialDirection = transform.up;
	}
	
	public Vector3 GetGravityVector(Transform attractedObject)
	{
		if (isRadial) return (attractedObject.position - transform.position).normalized * gravityStrength;
		else return nonRadialDirection * gravityStrength;
	}
	
	public void AttractObject(Transform attractedObject, float blend)
	{		
		// float blend = 0.025f;
		
		// Rotate the attracted object so that its downward direction faces the gravity source
		Vector3 gravityVector = GetGravityVector(attractedObject);
		
		// Calculate how far the object has to rotate.
		float rotationDegrees = Vector3.Angle(-attractedObject.up, gravityVector.normalized);
		
		float rotationStep = Mathf.InverseLerp(0.0f, 180.0f, rotationDegrees); // Convert the angles 0-180 to a 0-1 mapping.
		
		// 0.0f doesn't rotate at all, 1.0f rotates instantly.
		// 0.03f is fast, for things will small angle diferences, 0.0025f is slow for things with large angle differences.
		rotationStep = Mathf.Lerp(0.03f, 0.0025f, rotationStep); // Convert the angles 0-180 to a 0-1 mapping.
		
		/*
		rotationStep = Vector3.Distance(attractedObject.position, transform.position);
		rotationStep = Mathf.InverseLerp(0.0f, 50.0f, rotationStep); // Convert the angles 0-180 to a 0-1 mapping.
		rotationStep = Mathf.Lerp(0.03f, 0.0025f, rotationStep); // Convert the angles 0-180 to a 0-1 mapping.
		*/	
		
		// Set the target rotation (aim at gravity source)
		Quaternion targetRotation = Quaternion.FromToRotation(-attractedObject.up, gravityVector.normalized) * attractedObject.rotation;
		
		// Rotate towards the target.
		attractedObject.rotation = Quaternion.Slerp(attractedObject.rotation, targetRotation, rotationStep);
		
		// Add force to the attracted object to simulate gravity toward the gravity source.
		attractedObject.gameObject.GetComponent<Rigidbody>().AddForce(gravityVector, ForceMode.Acceleration);
	}
}
