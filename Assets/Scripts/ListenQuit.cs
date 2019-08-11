using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public class ListenQuit : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        if (Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.X))
        {
            SceneManager.LoadScene("StartMenu");
        }

    }
}
