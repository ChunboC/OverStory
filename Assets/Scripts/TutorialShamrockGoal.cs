using UnityEngine;

public class TutorialShamrockGoal : MonoBehaviour
{
    [SerializeField] private MovementTrainerNPC movementTrainer;

    private bool hasNotifiedTrainer;

    private void OnTriggerEnter(Collider other)
    {
        if (hasNotifiedTrainer)
        {
            return;
        }

        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null)
        {
            return;
        }

        hasNotifiedTrainer = true;

        if (movementTrainer != null)
        {
            movementTrainer.NotifyFountainShamrockCollected();
        }
    }
}