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
            _currHealth = Mathf.Clamp(_currHealth - damage, 0, _maxHealth);
        }
        #endregion
    }

    public abstract class PlayerEntity : GameEntity
    {
        protected override void InitCharacter()
        {
            base.InitCharacter();
            
            _isPlayer = true;
            _contactDamage = 0;
        }

        protected override void DeInitCharacter()
        {
            base.DeInitCharacter();
        }
    }

    public abstract class AmalgamEntity : GameEntity
    {
        protected override void InitCharacter()
        {
            base.InitCharacter();
            _isPlayer = false;
        }

        protected override void DeInitCharacter()
        {
            base.DeInitCharacter();
        }
    }


    public interface IGameEntityManager
    {
        public IGameEntityManager SetContext(IGameEntityManagerContext context);

        public void InitManager();

        public void DeInitManager();
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
    }

    public abstract class GameEntityManagerContext<TEntityManager, TEntityManagerContext> : IGameEntityManagerContext
        where TEntityManager : GameEntityManager<TEntityManager, TEntityManagerContext>, new()
        where TEntityManagerContext : GameEntityManagerContext<TEntityManager, TEntityManagerContext>
    {
        
    }
}