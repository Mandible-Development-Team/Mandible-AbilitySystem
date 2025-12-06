using System;
using UnityEngine;

namespace Mandible.AbilitySystem
{
    public interface IAbilitySystemExtension : IDisposable
    {
        public void Initialize(AbilitySystem owner);
        public void Handle();
    }
}
