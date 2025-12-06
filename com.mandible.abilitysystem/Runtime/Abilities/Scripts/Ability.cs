using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public abstract class Ability : ScriptableObject
{
    [Header("General")]
    public new string name;
    public int id;
    public Sprite icon;
    public string actionName;

    [Header("Ability Settings")]
    [SerializeField] private float duration = 0f;
    [SerializeField] private float cooldown = 2f;
    public float GetCooldown() => cooldown;
    public float GetDuration() => duration;
    public string GetCustomAction() => actionName;

    //Activation
    public abstract IEnumerator Activate(IAgent agent, RuntimeState data);
    protected bool IsActivated(IAgent agent, string actionName)
    {
        var input = agent.Input;
        return input != null && input.WasActivatedThisFrame(actionName);
    }
    
    //Operations
    protected IEnumerator WaitNextFrame()
    {
        yield return null;
    }

    protected IEnumerator WaitForPress(IAgent agent, string action)
    {
        while (!agent.Input.ConsumePressed(action))
            yield return null;
    }

    protected IEnumerator WaitForRelease(IAgent agent, string action)
    {
        while (!agent.Input.ConsumeReleased(action))
            yield return null;
    }

    protected IEnumerator WaitForHold(IAgent agent, string action)
    {
        while (!agent.Input.Held(action))
            yield return null;
    }

    protected IEnumerator WaitForSeconds(float t)
    {
        float elapsed = 0f;
        while (elapsed < t)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    protected IEnumerator WaitUntil(System.Func<bool> condition)
    {
        while (!condition())
            yield return null;
    }

    protected IEnumerator Exit()
    {
        yield break;
    }

    //Advanced
    protected T GetContext<T>(IAgent agent, string actionName) where T : struct
    {
        var input = agent.Input;
        return input != null ? input.GetContext<T>(actionName) : default;
    }

    protected T GetState<T>(RuntimeState rs) where T : new()
    {
        return rs.Blackboard.GetOrCreate<T>(this);
    }

    //Events
    public virtual void OnAbilityStart(IAgent agent, RuntimeState data) { }

    public virtual void OnAbilityCanceled(IAgent agent, RuntimeState data) { }

    public virtual void OnAbilityEnd(IAgent agent, RuntimeState data) { }
    public virtual bool DoesSatisfyCondition(){ return true; }
    
}