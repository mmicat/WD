using System;
using UnityEngine;
using WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers;

namespace WitchDoctor.GameResources.CharacterScripts.Player.AnimationEvents
{
    public class PlayerAnimationEvents : MonoBehaviour
    {
        public Action OnPrimaryAttackComplete;
        public Action<PrimaryAttackType> OnApplyPrimaryAttackHitbox;

        public void PrimaryAttackComplete()
        {
            OnPrimaryAttackComplete?.Invoke();
        }

        public void ApplyPrimaryAttackHitBox(int attackType)
        {
            OnApplyPrimaryAttackHitbox?.Invoke((PrimaryAttackType)attackType);
        }
    }
}