using UnityEngine;
using Mandible.Core;

namespace Mandible.AbilitySystem
{
    public abstract class MovementAbility : Ability
    {
        public virtual void Activate(IAgent agent){ }
    }
}