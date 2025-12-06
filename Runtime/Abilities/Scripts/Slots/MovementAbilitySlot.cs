using UnityEngine;

namespace Mandible.AbilitySystem
{
    public class MovementAbilitySlot : AbilitySlot
    {
        protected override string ResolveInputAction()
        {
            return "MovementAbility";
        }
    }
}