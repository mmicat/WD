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
        public bool interact;
        public bool interacting;
        public bool lookingRight;
        public bool recoilingX;
        public bool recoilingY;
        public bool casting;
        public bool castReleased;
        public bool onBench;
        public bool atBench;
        public bool atNPC;
        public bool usingNPC;

        #region State Properties
        public bool IsIdle => !walking && !dashing && !jumping && !attacking;
        public bool CanAttack => walking || dashing || IsIdle || attacking;
        #endregion

        public void Reset()
        {
            walking = false;
            jumping = false;
            dashing = false;
            dashRefreshed = true;
            dashConditionsMet = true;
            attacking = false;
            interact = false;
            interacting = false;
            lookingRight = false;
            recoilingX = false;
            recoilingY = false;
            casting = false;
            castReleased = false;
            onBench = false;
            atBench = false;
            atNPC = false;
            usingNPC = false;
        }
    }
}