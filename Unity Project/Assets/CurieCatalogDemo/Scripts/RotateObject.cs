using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public Vector3 rotationPerSec;

    public bool Rotate = true;

    void Update()
    {
        if(Rotate)
            transform.Rotate(rotationPerSec * Time.deltaTime);
    }
}
