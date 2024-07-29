using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.CharacterScripts.Player.AnimationEvents
{
    public class ChargeFXAnimationEvents : MonoBehaviour
    {
        public Action OnChargeComplete;

        public void ChargeComplete()
        {
            OnChargeComplete?.Invoke();
        }
    }
}