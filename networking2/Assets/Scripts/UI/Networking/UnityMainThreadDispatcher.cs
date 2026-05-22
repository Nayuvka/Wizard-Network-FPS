using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> executionQueue = new Queue<Action>();
    private void Update() { lock (executionQueue) { while (executionQueue.Count > 0) executionQueue.Dequeue()(); } }
    public static void ExecuteOnMainThread(Action action) { lock (executionQueue) { executionQueue.Enqueue(action); } }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize() { new GameObject("MainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>(); }
}
