using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartLevel : MonoBehaviour
{
    void Update()
    {
        if (Input.GetButtonDown("Restart")) SceneManager.LoadScene( SceneManager.GetActiveScene().buildIndex );
    }
}
