using UnityEngine;

public class TutorialStatueInteractable : MonoBehaviour, IInteractable
{
    [SerializeField]
    private int staffTypeIndex;

    public string promptMessage => "Use Statue";

    public void Interact(NetworkPlayerController player)
    {
        NetworkShoot shoot = player.GetComponent<NetworkShoot>();

        if (shoot == null)
            return;

        shoot.CycleProjectile(1);
    }
}