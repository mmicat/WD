using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public class PlayerStates
    {
        public bool walking;
        public bool jumping;
        public bool dashing;
        public bool dashRefreshed;
        public bool dashConditionsMet;
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

        public void Reset()
        {
            walking = false;
            jumping = false;
            dashing = false;
            dashRefreshed = true;
            dashConditionsMet = true;
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