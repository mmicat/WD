using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using WitchDoctor.Managers.InputManagement;
using WitchDoctor.GameResources.Utils.ScriptableObjects;
using WitchDoctor.GameResources.CharacterScripts.Player.AnimationEvents;

namespace WitchDoctor.GameResources.CharacterScripts.Player.EntityManagers
{
    public enum PrimaryAttackType
    {
        None = 0,
        Attack1 = 1,
        Attack2 = 2,
        Attack3 = 3,
        ChargedAttack = 4
    }

    public class PlayerCombatManager : GameEntityManager<PlayerCombatManager, PlayerCombatManagerContext>
    {
        #region Private Properties
        private PrimaryAttackType _currAttack = PrimaryAttackType.None;

        private PlayerStates _playerStates;
        private Animator _animator;
        private Animator _chargeFXAnimator;
        private PlayerAnimationEvents _animationEvents;
        private ChargeFXAnimationEvents _chargeFXAnimationEvents;
        private PlayerMovementManager _playerMovementManager;

        private Coroutine _inputWaitingCoroutine;
        private Coroutine _attackChainCoroutine;

        private float _primaryAttackInputTime;
        private float _chargeInitTime;
        private float _currChargeTime;

        private bool _primaryAttackInputStarted;
        private bool _primaryAttackChargeStarted;
        private bool _primaryAttackCharged;

        #endregion

        #region Serialized Properties
        [SerializeField] private PlayerStats _baseStats;
        
        [SerializeField]
        private Transform _meleeAttackCenter;
        #endregion

#if UNITY_EDITOR
        [Space(5)]
        [Header("Debug Options")]
        [SerializeField] private bool _primaryAttack1Hitbox;
        [SerializeField] private bool _primaryAttack2Hitbox, 
            _primaryAttack3Hitbox, 
            _primaryChargedAttackHitbox;
#endif

        #region Overrides
        public override void InitManager()
        {
            base.InitManager();

            _playerStates = InitializationContext.PlayerStates;
            _baseStats = InitializationContext.BaseStats;
            _animator = InitializationContext.Animator;
            _chargeFXAnimator = InitializationContext.ChargeFXAnimator;
            _animationEvents = InitializationContext.PlayerAnimationEvents;
            _chargeFXAnimationEvents = InitializationContext.ChargeFXAnimationEvents;
            _playerMovementManager = InitializationContext.PlayerMovementManager;

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

            InputManager.Player.PrimaryAttack.started += PrimaryAttack_started;
            InputManager.Player.PrimaryAttack.canceled += PrimaryAttack_canceled;
            InputManager.Player.SecondaryAttack.performed += SecondaryAttack_performed;

            _animationEvents.OnApplyPrimaryAttackHitbox += OnApplyHitBox_Event;
            _animationEvents.OnPrimaryAttackComplete += OnAttackComplete_Event;

            _animationEvents.OnChargeAnimationComplete_Character += OnCharacterChargeComplete;
            _chargeFXAnimationEvents.OnChargeComplete += OnChargeFXComplete;

            _inputWaitingCoroutine = null;
        }

        private void RemoveListeners()
        {
            _animationEvents.OnApplyPrimaryAttackHitbox -= OnApplyHitBox_Event;
            _animationEvents.OnPrimaryAttackComplete -= OnAttackComplete_Event;

            if (!InputManager.IsInstantiated) return;
            InputManager.Player.PrimaryAttack.started -= PrimaryAttack_started;
            InputManager.Player.PrimaryAttack.canceled -= PrimaryAttack_canceled;
            InputManager.Player.SecondaryAttack.performed -= SecondaryAttack_performed;
        }

        private void SetCurrentAttack(PrimaryAttackType value, bool updateAnimation = true)
        {
            if (updateAnimation && _currAttack != value)
            {
                _animator.SetInteger("Attack", (int)value);
            }

            _currAttack = value;
        }

        private void SetCharging(bool updateAnimation = true)
        {
            var value = _primaryAttackChargeStarted ^ _primaryAttackCharged;

            if (updateAnimation && _playerStates.chargingAttack != value)
            {
                _animator.SetBool("ChargingAttack", value);

                if (!value) _chargeFXAnimator.gameObject.SetActive(false);
            }

            _playerStates.chargingAttack = value;
        }

        #region Combat Scripts
        private void UpdateCombatStates()
        {
            if (!_playerStates.attacking)
            {
                if (_primaryAttackInputStarted)
                    PrimaryAttack();
                else if (_primaryAttackChargeStarted && !_primaryAttackCharged)
                    ChargeAttack();
                else if (_playerMovementManager.IsGrounded && InputManager.Player.PrimaryAttack.phase == (InputActionPhase.Started | InputActionPhase.Waiting | InputActionPhase.Performed) &&
                    !_primaryAttackInputStarted && !_primaryAttackCharged && !_primaryAttackChargeStarted && 
                    (Time.time - _primaryAttackInputTime) > _baseStats.PrimaryAttackChargeDelay)
                {
                    // Debug.Log("Charge Start");
                    _primaryAttackChargeStarted = true;
                    _chargeInitTime = Time.time;
                    _currChargeTime = 0f;

                    SetCharging();
                }
            }
        }

        private void PrimaryAttack()
        {
            _playerStates.attacking = true;

            if (_attackChainCoroutine != null)
            {
                // Debug.Log("Stopping Attack Chain Cancelling Coroutine");
                StopCoroutine(_attackChainCoroutine);
                _attackChainCoroutine = null;
            }

            if (_primaryAttackCharged)
            {
                // Debug.Log("Using Charged Attack");
                SetCurrentAttack(PrimaryAttackType.ChargedAttack);
                _primaryAttackCharged = false;

                SetCharging();
            }
            else
            {
                SetCurrentAttack((_currAttack == (PrimaryAttackType)3 ? (PrimaryAttackType)1 : _currAttack + 1));
                // Debug.Log($"Standard Attack: {_currAttack}");
            }

            // _animator.SetInteger("Attack", (int)_currAttack);
            _primaryAttackInputStarted = false;
        }

        private void ChargeAttack()
        {
            //if (_currChargeTime >= _baseStats.PrimaryAttackChargeTime)
            //{
            //    // Debug.Log("Charge Complete");
            //    _primaryAttackChargeStarted = false;
            //    _primaryAttackCharged = true;
            //}
            //else
            //{
                // Debug.Log("Charging Attack");
                _currChargeTime = Time.time - _chargeInitTime;
            //}

            // _animator.SetBool("ChargingAttack", _primaryAttackChargeStarted);
            SetCharging();
            
        }

        private IEnumerator AttackChain_Coroutine()
        {
            yield return new WaitForSeconds(_baseStats.AttackResetDuration);
            // Debug.Log("Attack Chain Stop Time Reached. Resetting Combo Chain");
            SetCurrentAttack(PrimaryAttackType.None);
            // _animator.SetInteger("Attack", (int)_currAttack);

            _attackChainCoroutine = null;
        }
        #endregion
        #endregion

        #region Event Listeners
        private void PrimaryAttack_started(InputAction.CallbackContext obj)
        {
            if (obj.started && _playerStates.CanAttack && _playerMovementManager.IsGrounded) // We'll change the is grounded flag later if required
            {
                _playerMovementManager.InterruptMovementActions();

                // Debug.Log("Primary Attack Started");
                _primaryAttackInputStarted = false;
                _primaryAttackCharged = false;
                _primaryAttackInputTime = Time.time;

                SetCharging(false);
            }
        }

        private void PrimaryAttack_canceled(InputAction.CallbackContext obj)
        {
            if (obj.canceled && !_primaryAttackInputStarted && _playerMovementManager.IsGrounded)
            {
                // Debug.Log("Attack Cancelled");
                _primaryAttackInputStarted = true;
                _primaryAttackChargeStarted = false;

                SetCharging();
            }
        }

        private void SecondaryAttack_performed(InputAction.CallbackContext obj)
        {
            throw new System.NotImplementedException();
        }

        private void OnApplyHitBox_Event(PrimaryAttackType attackType)
        {
            Collider2D[] collidersInContact;
            switch (attackType)
            {
                case PrimaryAttackType.Attack1:
                    collidersInContact = Physics2D.OverlapCircleAll(_meleeAttackCenter.position + (Vector3) _baseStats.Attack1Offset, _baseStats.Attack1HitboxRadius, _baseStats.PlayerAttackableLayers);
                    break;
                case PrimaryAttackType.Attack2:
                    collidersInContact = Physics2D.OverlapCircleAll(_meleeAttackCenter.position + (Vector3)_baseStats.Attack2Offset, _baseStats.Attack2HitboxRadius, _baseStats.PlayerAttackableLayers);
                    break;
                case PrimaryAttackType.Attack3:
                    collidersInContact = Physics2D.OverlapCircleAll(_meleeAttackCenter.position + (Vector3)_baseStats.Attack3Offset, _baseStats.Attack3HitboxRadius, _baseStats.PlayerAttackableLayers);
                    break;
                case PrimaryAttackType.ChargedAttack:
                    collidersInContact = Physics2D.OverlapCircleAll(_meleeAttackCenter.position + (Vector3)_baseStats.ChargedAttackOffset, _baseStats.ChargedAttackHitboxRadius, _baseStats.PlayerAttackableLayers);
                    break;
                default:
                    collidersInContact = null;
                    break;
            }

            if (collidersInContact == null || collidersInContact.Length <= 0)
                return;

            for (int i = 0; i < collidersInContact.Length; i++)
            {
                // Process the seperate collisions here
                Debug.Log($"Found Amalgam {collidersInContact[i].gameObject.name}");
            }
        }

        private void OnAttackComplete_Event()
        {
            // Debug.Log("Attack Animation Complete");
            _playerStates.attacking = false;
            _primaryAttackCharged = false;

            SetCharging();

            _attackChainCoroutine = StartCoroutine(AttackChain_Coroutine());
        }

        private void OnCharacterChargeComplete()
        {
            // This is just the current implementation since
            // we only have one animation might change later
            _chargeFXAnimator.gameObject.SetActive(true);
        }

        private void OnChargeFXComplete()
        {
            // Debug.Log("Charge Complete");
            _primaryAttackChargeStarted = false;
            _primaryAttackCharged = true;

            _chargeFXAnimator.gameObject.SetActive(false);
        }
        #endregion

        #region Unity Methods
        private void Update()
        {
            UpdateCombatStates();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_primaryAttack1Hitbox)
            {
                Gizmos.DrawWireSphere(_meleeAttackCenter.position + (Vector3)_baseStats.Attack1Offset, _baseStats.Attack1HitboxRadius);
            }
            if (_primaryAttack2Hitbox)
            {
                Gizmos.DrawWireSphere(_meleeAttackCenter.position + (Vector3)_baseStats.Attack2Offset, _baseStats.Attack2HitboxRadius);
            }
            if (_primaryAttack3Hitbox)
            {
                Gizmos.DrawWireSphere(_meleeAttackCenter.position + (Vector3)_baseStats.Attack3Offset, _baseStats.Attack3HitboxRadius);
            }
            if (_primaryChargedAttackHitbox)
            {
                Gizmos.DrawWireSphere(_meleeAttackCenter.position + (Vector3)_baseStats.ChargedAttackOffset, _baseStats.ChargedAttackHitboxRadius);
            }
        }
#endif
        #endregion
    }

    public class PlayerCombatManagerContext : GameEntityManagerContext<PlayerCombatManager, PlayerCombatManagerContext>
    {
        public PlayerStates PlayerStates;
        public PlayerStats BaseStats;
        public Animator Animator;
        public Animator ChargeFXAnimator;
        public PlayerAnimationEvents PlayerAnimationEvents;
        public ChargeFXAnimationEvents ChargeFXAnimationEvents;
        public PlayerMovementManager PlayerMovementManager;

        public PlayerCombatManagerContext(PlayerStates playerStates, PlayerStats baseStats, 
            Animator animator, Animator chargeFXAnimator, PlayerAnimationEvents playerAnimationEvents, 
            ChargeFXAnimationEvents chargeFXAnimationEvents, PlayerMovementManager playerMovementManager)
        {
            PlayerStates = playerStates;
            BaseStats = baseStats;
            Animator = animator;
            ChargeFXAnimator = chargeFXAnimator;
            PlayerAnimationEvents = playerAnimationEvents;
            ChargeFXAnimationEvents = chargeFXAnimationEvents;
            PlayerMovementManager = playerMovementManager;
        }
    }
}