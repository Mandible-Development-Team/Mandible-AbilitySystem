using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Mandible.AbilitySystem;

[CreateAssetMenu(menuName = "Abilities/Combat/Grenade")]
public class GrenadeAbility : CombatAbility
{
    [Header("Grenade Settings")]
    public GameObject grenadePrefab;
    public float throwForce = 10f;
    public float arcHeight = 0.5f;
    public float spawnOffset = 1f;

    public override IEnumerator Activate(IAgent agent, RuntimeState data)
    {
        if (grenadePrefab == null)
        {
            Debug.LogWarning("Grenade prefab not assigned!");
            yield return null;
        }

        Vector3 spawnPos = agent.transform.position + agent.GetLookDirection() * spawnOffset;

        GameObject grenade = Instantiate(grenadePrefab, spawnPos, Quaternion.identity);

        if (grenade.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 throwDir = (agent.GetLookDirection() + Vector3.up * arcHeight).normalized;
            rb.linearVelocity = throwDir * throwForce;
        }
        else
        {
            Debug.LogWarning("Grenade prefab missing Rigidbody!");
        }

        yield return null;
    }
}
