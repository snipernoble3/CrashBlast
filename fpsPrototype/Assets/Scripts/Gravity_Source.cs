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
	
	public void AttractObject(Transform attractedObject, float blend, bool rotateToGravitySoruce)
	{		
		// Find the direction of gravity
		Vector3 gravityVector = GetGravityVector(attractedObject);
		
		// Add force to the attracted object to simulate gravity toward the gravity source.
		attractedObject.gameObject.GetComponent<Rigidbody>().AddForce(gravityVector, ForceMode.Acceleration);
		
		if (rotateToGravitySoruce) // Rotate the attracted object so that its downward direction faces the gravity source
		{
			bool useTestSpheres = true;
			useTestSpheres = false;
			
			// Start by getting the distance from the attracted object to the CENTER of the gravity source (this will be longer than the distnace to the SURFACE, but it gives us a range to check).
			float distanceToSurface = Vector3.Distance(attractedObject.position, transform.position);
			
			RaycastHit[] surfaceHits; 
			
			// Use a RaycastAll in case somthing is in the way between the attracted object and the gravity source.
			surfaceHits = Physics.RaycastAll(attractedObject.position, gravityVector, distanceToSurface);
			
			for (int i = 0; i < surfaceHits.Length; i++)
			{
				// Check if the object hit was this gravity source.
				if (surfaceHits[i].transform == transform)
				{
					// Update the distanceToSurface to be the distance from the attracted object to the hit point of the raycast.
					distanceToSurface = Vector3.Distance(attractedObject.position, surfaceHits[i].point);
					
					// Test sphere
					if (useTestSpheres)
					{
						GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
						sphere.transform.position = surfaceHits[i].point;
						sphere.transform.SetParent(transform);
						Destroy(sphere.GetComponent<SphereCollider>());
						Renderer sphereRend = sphere.GetComponent<Renderer>();
						sphereRend.material = new Material(Shader.Find("Standard"));
						sphereRend.material.color = Color.magenta;
						Destroy(sphere, 5.0f);
					}
					
					break; // Once we've found the ray that hit the gravity source's collider, stop checking through the loop
				} 
			}
			
			
			float rotationStep = Mathf.InverseLerp(0.0f, 30.0f, distanceToSurface); // Convert the distance to a 0-1 mapping.
			
			
			// float blend = 0.025f;
			
			// Calculate how far the object has to rotate.
			//float rotationDegrees = Vector3.Angle(-attractedObject.up, gravityVector.normalized);
			
			//float rotationStep = Mathf.InverseLerp(0.0f, 180.0f, rotationDegrees); // Convert the angles 0-180 to a 0-1 mapping.
			
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
		}
	}
}
