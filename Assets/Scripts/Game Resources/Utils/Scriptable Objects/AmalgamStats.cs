using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.Utils.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Amalgam Stats", menuName = "Scriptable Objects/Amalgam Stats", order = 3)]
    public class AmalgamStats : ScriptableObject
    {
        [Header("Base Stats")]
        public int BaseHealth = 100;
        public int BaseAttackDamage = 15;
        public int ContactDamage = 10;

        [Space(5)]

        [Header("X Axis Movement")]
        public float WalkSpeed = 25f;

        [Space(5)]

        [Header("Combat - Attacking")]
        public LayerMask PlayerAttackableLayers;

    }
}