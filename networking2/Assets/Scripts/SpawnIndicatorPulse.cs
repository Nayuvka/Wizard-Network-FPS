using UnityEngine;

public class SpawnIndicatorPulse : MonoBehaviour
{
    Light spotLight;

    void Start()
    {
        spotLight = GetComponentInChildren<Light>();
    }

    void Update()
    {
        float pulse = Mathf.PingPong(Time.time * 5f, 10f);
        spotLight.intensity = 15 + pulse;
    }
}