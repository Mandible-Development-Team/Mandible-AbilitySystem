using UnityEngine;
using System.Collections;
using System.Collections.Generic;

//using Mandible.Systems;
using Mandible.Core;

namespace Mandible.AbilitySystem
{
    [CreateAssetMenu(menuName = "Abilities/Ultimate/Generic Ultimate")]
    public class UltimateAbility : Ability
    {
        public AbilityOverrideMode mode;
        public UltimateAbility(AbilityOverrideMode mode) => this.mode = mode;

        public override void OnAbilityStart(IAgent agent, RuntimeState data){ }

        public override IEnumerator Activate(IAgent agent, RuntimeState data)
        {
            float duration = GetDuration();

            yield return WaitForSeconds(duration);
        }


        public override void OnAbilityEnd(IAgent agent, RuntimeState data){ }
    }
}