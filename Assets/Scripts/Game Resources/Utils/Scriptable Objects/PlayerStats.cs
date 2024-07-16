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
        public float _dashingVelocity = 14f;
        public float _dashingTime = 0.5f;
        public float _dashingRefreshTime = 0.8f;

        [Space(5)]

        [Header("Ground Checking")]
        public float GroundCheckY = 0.2f; //How far on the Y axis the groundcheck Raycast goes.
        public float GroundCheckX = 1f; //Same as above but for X.
        public LayerMask GroundLayer;

        [Space(5)]

        [Header("Roof Checking")]
        public float RoofCheckY = 0.2f;
        public float RoofCheckX = 1f;
    }
}