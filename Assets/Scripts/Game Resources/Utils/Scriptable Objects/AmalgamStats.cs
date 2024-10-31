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
        public float MovementSpeed = 25f;

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

        [Header("Obstacle Checking")]
        public float ObstacleCheckDist = 0.2f;
        public float ObstacleCheckX = 0.2f;
        public float ObstacleCheckY = 1f;
        
        [Space(5)]

        [Header("Combat - Attacking")]
        public LayerMask AmalgamAttackableLayers;

        [Header("Combat - Getting Attacked")]
        public LayerMask AmalgamDamagableLayers;
        public int recoilXSteps = 4;
        public int recoilYSteps = 10;
        public float recoilSpeedDecayConstant = 3;
        public float recoilXSpeed = 45f;
        public float recoilYSpeed = 45f;
        public float recoilGravityScale = 1f;

    }
}