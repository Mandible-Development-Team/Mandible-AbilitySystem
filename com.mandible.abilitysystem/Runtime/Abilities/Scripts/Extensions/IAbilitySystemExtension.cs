using System;
using UnityEngine;

public interface IAbilitySystemExtension : IDisposable
{
    public void Initialize(AbilitySystem owner);
    public void Handle();
}
