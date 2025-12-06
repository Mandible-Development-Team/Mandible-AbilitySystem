using UnityEngine;
using Mandible.Systems;
using System.Collections.Generic;

namespace Mandible.AbilitySystem
{
    [CreateAssetMenu(menuName = "Abilities/OverrideMode")]
    public class AbilityOverrideMode : ScriptableObject
    {
        [Header("Override Abilities")]
        public ObservedList<Ability> abilities = new ObservedList<Ability>();

        [Header("Advanced")]
        public AbilityResetMask resetMask = AbilityResetMask.Swappable;
        public AbilityResetMask exitResetMask = AbilityResetMask.Swappable;
    }

    [System.Flags]
    public enum AbilityResetMask
    {
        None        = 0,
        Base        = 1 << 0,
        Swappable   = 1 << 1,
        All         = ~0
    }
}

