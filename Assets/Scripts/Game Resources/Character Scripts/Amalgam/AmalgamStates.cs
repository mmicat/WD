using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.CharacterScripts.Amalgam
{
    [System.Serializable]
    public class AmalgamStates
    {
        public bool alert;
        public bool walking;
        public bool blockedByObstacle;
        public bool jumping;
        public bool dashing;
        public bool dashRefreshed;
        public bool dashConditionsMet;
        public bool attacking;
        public bool chargingAttack;
        public bool recoilingX;
        public bool recoilingY;
        public bool casting;
        public bool castReleased;
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
            alert = false;
            walking = false;
            jumping = false;
            dashing = false;
            dashRefreshed = true;
            dashConditionsMet = true;
            attacking = false;
            chargingAttack = false;
            recoilingX = false;
            recoilingY = false;
            casting = false;
            castReleased = false;
            dead = false;
        }
    }
}
