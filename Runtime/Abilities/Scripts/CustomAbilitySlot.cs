using UnityEngine;

namespace Mandible.AbilitySystem
{
    public class CustomAbilitySlot : AbilitySlot
    {
        [Header("Custom Ability Slot")]
        [SerializeField] string actionName;
        [SerializeField] bool abilityOverridesAction = false;

        protected override string ResolveInputAction()
        {
            if (abilityOverridesAction && ability != null && !string.IsNullOrEmpty(ability.GetCustomAction()))
                return ability.GetCustomAction();

            return actionName;
        }

        bool CanOverrideInputFromAbility()
        {
            return abilityOverridesAction == true && ability != null && ability.GetCustomAction() != null;
        }

    }
}
