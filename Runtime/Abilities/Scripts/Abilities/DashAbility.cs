using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Mandible.AbilitySystem;
using Mandible.Core;

[CreateAssetMenu(menuName = "Abilities/Movement/Dash")]
public class DashAbility : MovementAbility
{
    public float force = 10f;

    public override IEnumerator Activate(IAgent agent, RuntimeState data)
    {
        agent.CancelGravity();
        agent.ApplyImpulse(agent.GetLookDirection() * force);

        yield return null;
    }
}
