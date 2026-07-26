using System.Collections.Generic;
using UnityEngine;

public class WellTeleporter : MonoBehaviour
{
    [Header("Teleport Destination")]
    [SerializeField] private Transform destination;

    [Header("Settings")]
    [SerializeField] private float teleportCooldown = 1f;
    [SerializeField] private bool resetVelocity = true;

    // Prevents the player from immediately teleporting back.
    private static readonly Dictionary<GameObject, float> LastTeleportTimes = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || destination == null)
            return;

        GameObject player = other.transform.root.gameObject;

        if (LastTeleportTimes.TryGetValue(player, out float lastTeleportTime))
        {
            if (Time.time < lastTeleportTime + teleportCooldown)
                return;
        }

        LastTeleportTimes[player] = Time.time;

        Rigidbody playerRigidbody = player.GetComponent<Rigidbody>();

        if (playerRigidbody != null)
        {
            playerRigidbody.position = destination.position;
            playerRigidbody.rotation = destination.rotation;

            if (resetVelocity)
            {
                playerRigidbody.linearVelocity = Vector3.zero;
                playerRigidbody.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            player.transform.SetPositionAndRotation(
                destination.position,
                destination.rotation
            );
        }
    }
}