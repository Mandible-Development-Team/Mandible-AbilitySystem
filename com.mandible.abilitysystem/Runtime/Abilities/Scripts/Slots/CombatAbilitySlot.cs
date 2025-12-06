using UnityEngine;
public class CombatAbilitySlot : AbilitySlot
{
    [SerializeField, Range(1, 9)] int slotNumber;

    protected override string ResolveInputAction()
    {
        return $"Ability{slotNumber}";
    }
}
