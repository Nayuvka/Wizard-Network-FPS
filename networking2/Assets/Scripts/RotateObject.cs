using UnityEngine;

public class RotateObject : MonoBehaviour
{
    public float xRotation;
    public float yRotation;
    public float zRotation;


    void Update()
    {
        transform.Rotate(xRotation * Time.deltaTime, yRotation * Time.deltaTime, zRotation * Time.deltaTime);
    }
}
