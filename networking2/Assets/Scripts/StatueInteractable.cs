using UnityEngine;
using Unity.Netcode;

public class StatueInteractable : NetworkBehaviour
{
    private bool interacted = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || interacted) return;

        if (other.CompareTag("Player"))
        {
            interacted = true;

            NetworkShoot shooter = other.GetComponent<NetworkShoot>();
            if (shooter != null)
            {
                int randomType = Random.Range(0, 4); // Assuming 4 staff types
                int damageBoost = SpawnManager.Instance.currentRound.Value * 5;

                shooter.currentStaffTypeIndex = randomType;
                shooter.baseStaffDamage += damageBoost;
            }

            SpawnManager.Instance.OnStatueInteracted();
        }
    }
}