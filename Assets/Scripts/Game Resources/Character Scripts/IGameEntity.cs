using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WitchDoctor.GameResources.CharacterScripts
{
    public interface IGameEntity
    {
        public bool IsPlayer { get; }
        public int MaxHealth { get; }
        public int CurrHealth { get; }
        public int ContactDamage { get; }
        public void TakeDamage(int damage);
    }

    public abstract class GameEntity : MonoBehaviour, IGameEntity
    {
        #region Entity Properties
        protected bool _isPlayer;
        protected int _maxHealth;
        protected int _currHealth;
        protected int _contactDamage;
        protected bool _invincible;

        public bool IsPlayer => _isPlayer;

        public int MaxHealth => _maxHealth;

        // Ensure that current health never exceeds the health max
        public int CurrHealth
        {
            get
            {
                _currHealth = _currHealth > _maxHealth ? _maxHealth : _currHealth;
                return _currHealth;
            }
        }

        public int ContactDamage => _contactDamage;
        #endregion

        #region Entity Managers
        protected List<IGameEntityManager> _managerList;

        /// <summary>
        /// Implement this method to setup any 
        /// managers that are controlled by this 
        /// parent entity. They will be initialized 
        /// automatically
        /// </summary>
        protected abstract void SetManagerContexts();

        protected void InitializeManagers()
        {
            if (_managerList == null || _managerList.Count <= 0)
                return;

            for (int i = 0; i < _managerList.Count; i++)
                _managerList[i].InitManager();

            _invincible = false;
        }

        protected void DeInitializeManagers()
        {
            if (_managerList == null || _managerList.Count <= 0)
                return;

            for (int i = 0; i < _managerList.Count; i++)
            {
                _managerList[i].DeInitManager();
            }

            _managerList.Clear();
        }
        #endregion

        public void TakeDamage(int damage)
        {
            OnDamageTaken(damage);
        }

        #region Unity Methods
        private void OnEnable()
        {
            InitCharacter();
        }

        private void OnDisable()
        {
            DeInitCharacter();
        }

        private void OnDrawGizmos()
        {
            DisplayDebugElements();
        }
        #endregion

        #region Internal Methods
        protected virtual void InitCharacter()
        {
            SetManagerContexts();
            InitializeManagers();
        }

        protected virtual void DeInitCharacter()
        {
            DeInitializeManagers();
        }

        protected virtual void OnDamageTaken(int damage)
        {
            if (_invincible) return;

            _currHealth = Mathf.Clamp(_currHealth - damage, 0, _maxHealth);

            if (_currHealth <= 0) OnDeath();
        }

        protected virtual void OnDeath()
        {
            _invincible = true;

            if ( _managerList == null || _managerList.Count <= 0) return;

            for (int i = 0; i < _managerList.Count; i++)
            {
                _managerList[i].OnEntityDeath();
            }
        }

        protected virtual void DisplayDebugElements()
        {

        }
        #endregion
    }

    public abstract class PlayerEntity : GameEntity
    {
        protected int _primaryAttackDamage;
        protected int _secondaryAttackDamage;
        protected float _chargedAttackFactor;

        public int PrimaryAttackDamage => _primaryAttackDamage;
        public int ChargedAttackDamage => (int) (_primaryAttackDamage * _chargedAttackFactor);
        public int SecondaryAttackDamage => _secondaryAttackDamage;

        protected override void InitCharacter()
        {
            base.InitCharacter();
            
            _isPlayer = true;
            _currHealth = _maxHealth; // set max health in override
            _contactDamage = 0;
        }

        protected override void DeInitCharacter()
        {
            base.DeInitCharacter();
        }
    }

    public abstract class AmalgamEntity : GameEntity
    {
        [Space(5)]
        [Header("Vision Cone")]
        [SerializeField]
        protected bool _displayVisionCone = false;
        [SerializeField]
        protected Transform _visionConeCenter;
        protected Mesh _visionConeMesh;
        [SerializeField, Range(1, 300), Tooltip("Defines the number of triangles used to create the vision cone")]
        protected int _visionConeResolution = 50;
        [SerializeField]
        protected float _visionRadius = 3f;
        [SerializeField, Range(0f, 360f)]
        protected float _viewAngle = 0f;
        [SerializeField, Range (0f, 2f)]
        protected float _visionConeRefreshTime = 1f;
        [SerializeField, Range(0, 10)]
        protected int _alertMaxSteps;

        protected override void InitCharacter()
        {
            base.InitCharacter();
            _isPlayer = false;
        }

        protected override void DeInitCharacter()
        {
            base.DeInitCharacter();
        }

        protected abstract void UpdateAmalgamStates();
    }


    public interface IGameEntityManager
    {
        public IGameEntityManager SetContext(IGameEntityManagerContext context);

        public void InitManager();

        public void DeInitManager();

        public void OnEntityDeath();
    }

    public interface IGameEntityManagerContext
    {

    }

    public abstract class GameEntityManager<TEntityManager, TEntityManagerContext> : MonoBehaviour, IGameEntityManager
        where TEntityManager : GameEntityManager<TEntityManager, TEntityManagerContext>, new()
        where TEntityManagerContext : GameEntityManagerContext<TEntityManager, TEntityManagerContext>
    {
        protected TEntityManagerContext InitializationContext { get; private set; }

        public virtual IGameEntityManager SetContext(IGameEntityManagerContext context)
        {
            if (!(context is TEntityManagerContext)) // Ensures the called context is of the same associated type as the manager
                throw new InvalidCastException($"Attempting to use incorrect context: {context.GetType()} for current manager: {typeof(TEntityManager)}.\nPlease use correct context: {typeof(TEntityManagerContext)}");

            InitializationContext = context as TEntityManagerContext;
            return this as TEntityManager;
        }

        public virtual void InitManager()
        {
            if (InitializationContext == null)
                InitializationContext = default;

            this.enabled = true;
        }

        public virtual void DeInitManager()
        {
        }

        public virtual void OnEntityDeath()
        {
        }

        protected virtual void DisplayDebugElements()
        {

        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            DisplayDebugElements();
        }
#endif
    }

    public abstract class GameEntityManagerContext<TEntityManager, TEntityManagerContext> : IGameEntityManagerContext
        where TEntityManager : GameEntityManager<TEntityManager, TEntityManagerContext>, new()
        where TEntityManagerContext : GameEntityManagerContext<TEntityManager, TEntityManagerContext>
    {
        
    }
}