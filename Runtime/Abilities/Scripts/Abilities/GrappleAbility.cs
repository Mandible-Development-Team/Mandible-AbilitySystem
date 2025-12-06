using UnityEngine;
using System.Collections;

using Mandible.PlayerController;

[CreateAssetMenu(menuName = "Abilities/Movement/Grapple")]
public class GrappleAbility : MovementAbility
{
    [Header("Grapple Settings")]
    public float maxDistance = 30f;
    public float pullForce = 50f;
    public float stopDistance = 1.5f;
    public LayerMask grappleLayerMask;

    public override IEnumerator Activate(IAgent agent, RuntimeState data)
    {
        #if MANDIBLE_PLAYER_CONTROLLER
        PlayerController player = agent as PlayerController;

        if (player == null)
        {
            Debug.LogWarning("GrappleAbility: Agent is not a PlayerController!");
            yield break;
        }

        Vector3 origin = player.camera.transform.position;
        Vector3 direction = player.camera.transform.forward;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, grappleLayerMask))
        {
            Vector3 targetPoint = hit.point;

            while ((player.transform.position - targetPoint).sqrMagnitude > stopDistance * stopDistance)
            {
                Vector3 pullDir = (targetPoint - player.transform.position).normalized;
                Vector3 impulse = pullDir * pullForce * Time.deltaTime;

                player.ApplyImpulse(impulse);

                yield return null;
            }
        }
        else
        {
            Debug.Log("Grapple: No target hit!");
        }
        #endif

        yield return null;
    }
}
