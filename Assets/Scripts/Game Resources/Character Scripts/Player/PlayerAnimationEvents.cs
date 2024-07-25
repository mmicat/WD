using System;
using UnityEngine;
using WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers;

namespace WitchDoctor.GameResources.CharacterScripts.Player
{
    public class PlayerAnimationEvents : MonoBehaviour
    {
        public Action OnAttackComplete;
        public Action<PrimaryAttackType> OnApplyHitBox;

        public void AttackComplete()
        {
            OnAttackComplete?.Invoke();
        }

        public void ApplyHitBox(int attackType)
        {
            OnApplyHitBox?.Invoke((PrimaryAttackType)attackType);
        }
    }
}