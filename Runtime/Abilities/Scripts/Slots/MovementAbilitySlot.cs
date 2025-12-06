using UnityEngine;

public class MovementAbilitySlot : AbilitySlot
{
    protected override string ResolveInputAction()
    {
        return "MovementAbility";
    }
}