using UnityEngine;

public class CursorLockOnStart : MonoBehaviour
{
    public bool lockCursorOnStart = true;
    void Start()
    {
        if (lockCursorOnStart)
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
    }
}
