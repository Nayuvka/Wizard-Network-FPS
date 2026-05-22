using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioListenerDebugger : MonoBehaviour
{
    [Header("Settings")]
    public bool logOnStart = true;
    public bool logRepeatedly = false;
    public float repeatInterval = 2f;

    private float timer;

    void Start()
    {
        if (logOnStart)
        {
            DumpAudioListeners();
        }
    }

    void Update()
    {
        if (!logRepeatedly) return;

        timer += Time.deltaTime;
        if (timer >= repeatInterval)
        {
            timer = 0f;
            DumpAudioListeners();
        }
    }

    [ContextMenu("Dump AudioListeners")]
    public void DumpAudioListeners()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

        Debug.Log($"===== AUDIO LISTENER DEBUG =====");
        Debug.Log($"Total AudioListeners Found: {listeners.Length}");

        for (int i = 0; i < listeners.Length; i++)
        {
            AudioListener al = listeners[i];

            if (al == null) continue;

            GameObject obj = al.gameObject;
            string path = GetHierarchyPath(obj);
            string scene = obj.scene.IsValid() ? obj.scene.name : "Unknown";

            Debug.Log(
                $"[AudioListener {i}] " +
                $"Name: {obj.name} | " +
                $"Scene: {scene} | " +
                $"Active: {obj.activeInHierarchy} | " +
                $"Enabled: {al.enabled} | " +
                $"Path: {path}"
            );
        }

        Debug.Log($"================================");
    }

    private string GetHierarchyPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform;

        while (current.parent != null)
        {
            current = current.parent;
            path = current.name + "/" + path;
        }

        return path;
    }
}