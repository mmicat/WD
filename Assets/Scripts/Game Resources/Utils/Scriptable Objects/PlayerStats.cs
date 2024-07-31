using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.Utils.ScriptableObjects
{
    [CreateAssetMenu(fileName = "Player Stats", menuName = "Scriptable Objects/Player Stats", order = 2)]
    public class PlayerStats : ScriptableObject
    {
        [Header("X Axis Movement")]
        public float WalkSpeed = 25f;

        [Space(5)]

        [Header("Y Axis Movement")]
        public float JumpSpeed = 45f;
        public float FallSpeed = 45f;
        public int JumpSteps = 20;
        public int JumpThreshold = 7;

        [Space(5)]

        [Header("Dashing")]
        public float DashingVelocity = 14f;
        public float DashingTime = 0.5f;
        public float DashingRefreshTime = 0.8f;

        [Space(5)]

        [Header("Ground Checking")]
        public float GroundCheckDist = 0.2f; // The distance of the origin point of the Raycast from the parent object
        public float GroundCheckY = 0.2f; //How far on the Y axis the groundcheck Raycast goes.
        public float GroundCheckX = 1f; //Same as above but for X.
        public LayerMask GroundLayer;

        [Space(5)]

        [Header("Roof Checking")]
        public float RoofCheckDist = 0.2f;
        public float RoofCheckY = 0.2f;
        public float RoofCheckX = 1f;

        [Space(5)]
        [Header("Ledge Checking")]
        public float LedgeCheckDist = 0.2f;
        public float LedgeCheckY = 0.2f;
        public float LedgeCheckX = 1f;

        [Space(5)]
        [Header("Combat")]
        public LayerMask PlayerAttackableLayers;
        [Tooltip("Time frame after which the attack chain is reset")] 
        public float AttackResetDuration = 0.3f;
        [Tooltip("A delay period after which the charge begins. Used to ensure that the charge does not begin for tapping interactions")]
        public float PrimaryAttackChargeDelay = 0.1f;
        [Tooltip("Time it takes for primary attack to charge")]
        public float PrimaryAttackChargeTime = 0.85f;
        public float Attack1HitboxRadius = 1f;
        public float Attack2HitboxRadius = 1f;
        public float Attack3HitboxRadius = 1f;
        public float ChargedAttackHitboxRadius = 1f;
        public Vector2 Attack1Offset = Vector2.zero;
        public Vector2 Attack2Offset = Vector2.zero;
        public Vector2 Attack3Offset = Vector2.zero;
        public Vector2 ChargedAttackOffset = Vector2.zero;
    }
}