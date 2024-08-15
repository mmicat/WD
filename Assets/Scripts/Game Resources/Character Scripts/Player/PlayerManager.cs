using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchDoctor.GameResources.CharacterScripts.Player.AnimationEvents;
using WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Utils;

namespace WitchDoctor.GameResources.CharacterScripts.Player
{
    public class PlayerManager : PlayerEntity
    {
        #region Movement and Physics
        [SerializeField]
        private Transform _characterRenderTransform;
        [SerializeField]
        private Transform _cameraFollowTransform;
        [SerializeField]
        private PlayerStats _baseStats;
        
        [Space(5)]

        [Header("Animations and Visual Effects")]
        [SerializeField]
        private Animator _animator;
        [SerializeField]
        private Animator _playerFXAnimator;
        [SerializeField]
        private PlayerAnimationEvents _playerAnimationEvents;
        [SerializeField]
        private ChargeFXAnimationEvents _chargeFXAnimationEvents;
        [SerializeField]
        private TrailRenderer _dashTrail;

        [Space(5)]
        
        [Header("Platform Checks")]
        [SerializeField]
        private Transform _groundTransform;
        [SerializeField]
        private Transform _ledgeTransform;
        [SerializeField]
        private Transform _roofTransform;
        
        private Rigidbody2D _rb;
        private PlayerStates _playerStates;
        #endregion

        #region Entity Managers
        [Space(5)]
        [Header("Entity Managers")]
        [SerializeField] private PlayerCameraManager _playerCameraManager;
        [SerializeField] private PlayerMovementManager _playerMovementManager;
        [SerializeField] private PlayerCombatManager _playerCombatManager;
        #endregion

        #region Overrides
        protected override void SetManagerContexts()
        {
            _managerList = new List<IGameEntityManager>()
            {
                _playerCameraManager.SetContext(
                    new PlayerCameraManagerContext(
                        _rb,
                        _characterRenderTransform,
                        _cameraFollowTransform)),
                _playerMovementManager.SetContext(
                    new PlayerMovementManagerContext(
                        _characterRenderTransform, _cameraFollowTransform, _animator,
                        _dashTrail, _baseStats, _groundTransform, _ledgeTransform,
                        _roofTransform, _rb, _playerStates, _playerCameraManager
                        )),
                _playerCombatManager.SetContext(
                    new PlayerCombatManagerContext(
                        _playerStates, _baseStats, _animator, _playerFXAnimator, 
                        _playerAnimationEvents, _chargeFXAnimationEvents, 
                        _playerMovementManager))
            };
        }

        protected override void InitCharacter()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody2D>();
            if (_animator == null) throw new NullReferenceException("Missing Animator Component");
            if (_playerStates == null) _playerStates = new PlayerStates();
            _playerStates.Reset();

            _maxHealth = _baseStats.BaseHealth;
            _primaryAttackDamage = _baseStats.BasePrimaryAttackDamage;
            _secondaryAttackDamage = _baseStats.BaseSecondaryAttackDamage;
            _chargedAttackFactor = _baseStats.BaseChargedAttackFactor;

            base.InitCharacter();
        }

        protected override void DeInitCharacter()
        {
            _playerStates.Reset();

            base.DeInitCharacter();
        }

        protected override void OnDamageTaken(int damage)
        {
            base.OnDamageTaken(damage);
            Debug.Log($"Damage Taken: {damage}\nCurrent Health: {_currHealth}");
        }

        protected override void OnDeath()
        {
            Debug.Log("Player Died");
            // Process Player Death
            _playerStates.Reset();
            _playerStates.dead = true;

            base.OnDeath();
        }
        #endregion

        #region Unity Methods
        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (_baseStats.PlayerDamagableLayers.Contains(collision.gameObject.layer))
            {
                TakeDamage(10); // get contact damage from IGameEntity
                if (_playerStates.dead) _playerMovementManager.ProcessEnemyCollision(collision);
            }
        }
        #endregion
    }
}