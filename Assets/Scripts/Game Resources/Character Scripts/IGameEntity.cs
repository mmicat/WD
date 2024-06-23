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

        [SerializeField]
        protected float _speed;
        [SerializeField]
        protected float _jumpForce;

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
            throw new System.NotImplementedException();
        }

        protected virtual void DeInitCharacter()
        {
            throw new System.NotImplementedException();
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
            _isPlayer = true;
            _contactDamage = 0;
        }

        protected override void DeInitCharacter()
        {
            
        }
    }

    public abstract class AmalgamEntity : GameEntity
    {
        protected override void InitCharacter()
        {
            _isPlayer = false;
        }

        protected override void DeInitCharacter()
        {

        }
    }
}