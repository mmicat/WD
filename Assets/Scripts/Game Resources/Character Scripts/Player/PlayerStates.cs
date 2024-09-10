using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.CharacterScripts
{
    [System.Serializable]
    public class PlayerStates
    {
        public bool walking;
        public bool jumping;
        public bool dashing;
        public bool dashRefreshed;
        public bool dashConditionsMet;
        public bool attacking;
        public bool chargingAttack;
        public bool interact;
        public bool interacting;
        public bool recoilingX;
        public bool recoilingY;
        public bool casting;
        public bool castReleased;
        public bool onBench;
        public bool atBench;
        public bool atNPC;
        public bool usingNPC;
        public bool dead;

        #region State Properties
        public bool IsIdle => !walking && !dashing && !jumping && !attacking && !chargingAttack && !recoilingX && !recoilingY && !dead;
        public bool CanAttack => !jumping && (walking || dashing || IsIdle || attacking);
        public bool IsMoving => walking || dashing || jumping;
        public bool IsRecoiling => recoilingX || recoilingY;
        public bool CanMove => !attacking && (IsMoving || IsIdle);
        #endregion

        public void Reset()
        {
            walking = false;
            jumping = false;
            dashing = false;
            dashRefreshed = true;
            dashConditionsMet = true;
            attacking = false;
            chargingAttack = false;
            interact = false;
            interacting = false;
            recoilingX = false;
            recoilingY = false;
            casting = false;
            castReleased = false;
            onBench = false;
            atBench = false;
            atNPC = false;
            usingNPC = false;
            dead = false;
        }
    }
}