using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class UltimateAbilitySystem : IAbilitySystemExtension
{
    public bool enabled = true;

    [Header("General")]
    [SerializeField] private UltimateAbility ultimateAbility;
    [SerializeField] private int ultimatePriority = 100;

    private AbilitySystem owner;
    //private PlayerInputActions input;
    private Coroutine process;

    public UltimateAbilitySystem() { }

    public UltimateAbilitySystem(AbilitySystem owner)
    {  
        Initialize(owner);
    }

    public void Initialize(AbilitySystem owner)
    {
        if (owner == null) return;
        this.owner = owner;
        //this.input = owner.input;

        SetEventListeners();
    }

    public void Dispose()
    {
        ClearEventListeners();
    }

    public void Handle()
    {
        #if MANDIBLE_PLAYER_CONTROLLER
        if (!enabled) return;
        if (owner == null) return;

        if (owner.inputSystem.ConsumePressed("Ultimate"))
            QueueUltimate();   
        #endif 
    }

    //Process
    protected virtual void OnStart()
    {
        owner.EnterOverrideMode(ultimateAbility.mode);

        if(process != null) owner.StopCoroutine(process);
        process = owner.StartCoroutine(RunProcess());
    }

    protected virtual IEnumerator RunProcess()
    {
        yield return ObserveAbility(ultimateAbility);
        OnEnd();
    }

    protected virtual void OnEnd()
    {
        owner.ExitOverrideMode();
    }

    //Ultimate
    private void QueueUltimate()
    {
        if (ultimateAbility != null)
            owner.QueueAbility(ultimateAbility, ultimatePriority);
    }

    //Ability
    protected virtual void OnAbilityCall(Ability ability)
    {
        if (ability == ultimateAbility)
            OnStart();
    }

    private IEnumerator ObserveAbility(Ability ability)
    {
        while(owner.IsAbilityInUse(ability))
            yield return null;
    }

    //Events
    void SetEventListeners()
    {
        //System
        owner.OnAbilityRun += OnAbilityCall;
    }

    void ClearEventListeners()
    {
        //System
        owner.OnAbilityRun -= OnAbilityCall;
    }
}
