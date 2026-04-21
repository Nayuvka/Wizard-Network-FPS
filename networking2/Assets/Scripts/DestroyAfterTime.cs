using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float Time = 0.1f;
    void Start()
    {
        Destroy(gameObject, Time);
    }
}
