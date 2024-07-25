using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.Managers.InputManagement;

namespace WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers
{
    public enum PrimaryAttackType
    {
        None = 0,
        Attack1 = 1,
        Attack2 = 2,
        Attack3 = 3
    }

    public class PlayerCombatManager : GameEntityManager<PlayerCombatManager, PlayerCombatManagerContext>
    {
        private PrimaryAttackType _currAttack = PrimaryAttackType.None;

        private PlayerStates _playerStates;
        private PlayerStats _baseStats;
        private Animator _animator;
        private PlayerAnimationEvents _animationEvents;

        private Coroutine _inputWaitingCoroutine;
        private Coroutine _attackChainCoroutine;

        private float _chargeStartupTime;
        private float _chargeTime;

        public bool _primaryAttackInputStarted;
        public bool _primaryAttackChargeStarted;
        public bool _primaryAttackCharged;

        #region Overrides
        public override void InitManager()
        {
            base.InitManager();

            _playerStates = InitializationContext.PlayerStates;
            _baseStats = InitializationContext.BaseStats;
            _animator = InitializationContext.Animator;
            _animationEvents = InitializationContext.AnimationEvents;

            _inputWaitingCoroutine = StartCoroutine(AddListeners());
        }

        public override void DeInitManager()
        {
            if (_inputWaitingCoroutine != null)
            {
                StopCoroutine(_inputWaitingCoroutine);
                _inputWaitingCoroutine = null;
            }

            RemoveListeners();

            base.DeInitManager();
        }
        #endregion

        #region Private Methods
        private IEnumerator AddListeners()
        {
            yield return new WaitUntil(() => InputManager.IsInstantiated);

            InputManager.InputActions.Player.PrimaryAttack.started += PrimaryAttack_started;
            InputManager.InputActions.Player.PrimaryAttack.performed += PrimaryAttack_performed;
            InputManager.InputActions.Player.PrimaryAttack.canceled += PrimaryAttack_canceled;
            InputManager.InputActions.Player.SecondaryAttack.performed += SecondaryAttack_performed;

            _animationEvents.OnApplyHitBox += OnApplyHitBox_Event;
            _animationEvents.OnAttackComplete += OnAttackComplete_Event;

            _inputWaitingCoroutine = null;
        }

        private void RemoveListeners()
        {
            _animationEvents.OnApplyHitBox -= OnApplyHitBox_Event;
            _animationEvents.OnAttackComplete -= OnAttackComplete_Event;

            if (!InputManager.IsInstantiated) return;
            InputManager.InputActions.Player.PrimaryAttack.performed -= PrimaryAttack_performed;
            InputManager.InputActions.Player.PrimaryAttack.canceled -= PrimaryAttack_canceled;
            InputManager.InputActions.Player.SecondaryAttack.performed -= SecondaryAttack_performed;
        }

        #region Combat Scripts
        private void UpdateCombatStates()
        {
            // _animator.SetInteger("Attack", (int)_currAttack);
            if (!_playerStates.attacking)
            {
                if (_primaryAttackInputStarted)
                    PrimaryAttack();
                else if (_primaryAttackChargeStarted && !_primaryAttackCharged)
                    ChargeAttack();

            }
        }

        private void PrimaryAttack()
        {
            _playerStates.attacking = true;

            if (_attackChainCoroutine != null)
            {
                Debug.Log("Stopping Attack Chain Cancelling Coroutine");
                StopCoroutine(_attackChainCoroutine);
                _attackChainCoroutine = null;
            }

            if (_primaryAttackCharged)
            {
                Debug.Log("Using Charged Attack");
                _currAttack = PrimaryAttackType.Attack3;
                _primaryAttackCharged = false;
            }
            else
            {
                _currAttack = _currAttack == (PrimaryAttackType)3 ? (PrimaryAttackType)1 : _currAttack + 1;
                Debug.Log($"Standard Attack: {_currAttack}");
            }

            _animator.SetInteger("Attack", (int)_currAttack);
            _primaryAttackInputStarted = false;
        }

        private void ChargeAttack()
        {
            if (_chargeTime >= _baseStats.PrimaryAttackChargeTime)
            {
                Debug.Log("Charge Complete");
                _primaryAttackChargeStarted = false;
                _primaryAttackCharged = true;
            }
            else
            {
                Debug.Log("Charging Attack");
                _chargeTime = Time.time - _chargeStartupTime;
            }
        }
        #endregion
        #endregion

        #region Event Listeners
        private void PrimaryAttack_started(InputAction.CallbackContext obj)
        {
            if (obj.started && _playerStates.CanAttack)
            {
                Debug.Log("Primary Attack Started");
                _primaryAttackInputStarted = false;
                _primaryAttackCharged = false;
            }
        }

        private void PrimaryAttack_performed(InputAction.CallbackContext obj)
        {
            if (obj.performed && !_primaryAttackInputStarted && !_playerStates.attacking)
            {
                if (!_primaryAttackCharged)
                {
                    if (!_primaryAttackChargeStarted)
                    {
                        Debug.Log("Charge Start");
                        _primaryAttackChargeStarted = true;
                        _chargeStartupTime = Time.time;
                        _chargeTime = 0f;
                    }
                    
                }
            }
        }

        private void PrimaryAttack_canceled(InputAction.CallbackContext obj)
        {
            if (obj.canceled && !_primaryAttackInputStarted)
            {
                Debug.Log("Attack Cancelled");
                _primaryAttackInputStarted = true;
                _primaryAttackChargeStarted = false;
            }
        }

        private void SecondaryAttack_performed(InputAction.CallbackContext obj)
        {
            throw new System.NotImplementedException();
        }

        private void OnApplyHitBox_Event(PrimaryAttackType attackType)
        {

        }

        private void OnAttackComplete_Event()
        {
            Debug.Log("Attack Animation Complete");
            _playerStates.attacking = false;
            _primaryAttackCharged = false;

            _attackChainCoroutine = StartCoroutine(AttackChain_Coroutine());
        }

        private IEnumerator AttackChain_Coroutine()
        {
            yield return new WaitForSeconds(_baseStats.InputLag);
            Debug.Log("Attack Chain Stop Time Reached. Resetting Combo Chain");
            _currAttack = PrimaryAttackType.None;
            _animator.SetInteger("Attack", (int)_currAttack);

            _attackChainCoroutine = null;
        }
        #endregion

        #region Unity Methods
        private void Update()
        {
            UpdateCombatStates();
        }
        #endregion
    }

    public class PlayerCombatManagerContext : GameEntityManagerContext<PlayerCombatManager, PlayerCombatManagerContext>
    {
        public PlayerStates PlayerStates;
        public PlayerStats BaseStats;
        public Animator Animator;
        public PlayerAnimationEvents AnimationEvents;

        public PlayerCombatManagerContext(PlayerStates playerStates, PlayerStats baseStats, Animator animator, PlayerAnimationEvents animationEvents)
        {
            PlayerStates = playerStates;
            BaseStats = baseStats;
            Animator = animator;
            AnimationEvents = animationEvents;
        }
    }
}