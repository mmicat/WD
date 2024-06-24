using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public struct PlayerStates
    {
        public bool walking;
        public bool interact;
        public bool interacting;
        public bool lookingRight;
        public bool jumping;
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
            interact = false;
            interacting = false;
            lookingRight = false;
            jumping = false;
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