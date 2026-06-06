using System;
using UnityEngine;

public class TestObject : MonoBehaviour, IInteractable
{
    public string promptMessage = "Interact with Test Object";

    public void Interact(NetworkPlayerController player)
    {
        Debug.Log("Interacted with Object");
    }
}
