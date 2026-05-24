using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;

    private static readonly Queue<Action>
        executionQueue = new();

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        lock (executionQueue)
        {
            while (executionQueue.Count > 0)
            {
                executionQueue
                    .Dequeue()
                    ?.Invoke();
            }
        }
    }

    public static void ExecuteOnMainThread(
        Action action)
    {
        if (action == null)
            return;

        lock (executionQueue)
        {
            executionQueue.Enqueue(action);
        }
    }

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null)
            return;

        GameObject dispatcherObject =
            new GameObject(
                "MainThreadDispatcher");

        dispatcherObject
            .AddComponent<UnityMainThreadDispatcher>();
    }
}