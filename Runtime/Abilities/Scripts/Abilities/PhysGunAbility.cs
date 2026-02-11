using UnityEngine;
using System.Collections;

using Mandible.AbilitySystem;
using Mandible.Core;

[CreateAssetMenu(menuName = "Abilities/Combat/PhysGun")]
public class PhysGunAbility : CombatAbility
{
    class Data
    {
        public Rigidbody held;
        public float distance = 4f;
    }

    [SerializeField] float range = 40f;
    [SerializeField] float pullSpeed = 12f;
    [SerializeField] float launchForce = 45f;
    [SerializeField] float minDist = 2f;
    [SerializeField] float maxDist = 10f;
    [SerializeField] float scrollSensitivity = 2f;
    [SerializeField] LayerMask mask = ~0;

    public override IEnumerator Activate(IAgent agent, RuntimeState rs)
    {
        var d = GetState<Data>(rs);

        yield return WaitForPress(agent, "Fire");

        Grab(agent, d);
        
        if (d.held == null) yield break;

        yield return WaitUntil(() =>
        {
            UpdateDistance(agent, d);
            ApplyPull(agent, d);

            return agent.Input.ConsumePressed("Fire");
        });

        Launch(agent, d);

        yield return Exit();
    }

    public override void OnAbilityCanceled(IAgent agent, RuntimeState rs)
    {
        var s = GetState<Data>(rs);
        if (s.held != null)
            s.held.useGravity = true;
    }

    void Grab(IAgent agent, Data d)
    {
        Vector3 origin = agent.transform.position;
        Vector3 dir = agent.GetLookDirection();

        if (Physics.Raycast(origin, dir, out var hit, range, mask) && hit.rigidbody)
        {
            d.held = hit.rigidbody;
            d.held.useGravity = false;
            d.held.linearVelocity = Vector3.zero;
        }
    }

    void UpdateDistance(IAgent agent, Data d)
    {
        float scroll = agent.Input.GetContext<float>("Scroll");
        if (scroll == 0f) return;

        d.distance += scroll * scrollSensitivity * Time.deltaTime;
        d.distance = Mathf.Clamp(d.distance, minDist, maxDist);
    }

    void ApplyPull(IAgent agent, Data d)
    {
        Vector3 target = agent.transform.position + agent.GetLookDirection() * d.distance;
        Vector3 toTarget = Vector3.zero;

        if(d.held == null) return;
        
        toTarget = target - d.held.position;

        float dist = toTarget.magnitude;
        float springStrength = pullSpeed * d.held.mass;
        float damping = 8f;

        Vector3 spring = toTarget.normalized * springStrength * dist;
        Vector3 damp = -d.held.linearVelocity * damping;

        d.held.AddForce(spring + damp, ForceMode.Force);
    }

    void Launch(IAgent agent, Data d)
    {
        if(d.held == null) return;

        d.held.useGravity = true;
        d.held.AddForce(agent.GetLookDirection() * launchForce, ForceMode.Impulse);
    }
}
