using UnityEngine;

namespace Mandible.AbilitySystem
{
    public abstract class MovementAbility : Ability
    {
        public virtual void Activate(IAgent agent){ }
    }
}