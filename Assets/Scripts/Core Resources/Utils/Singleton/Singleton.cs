using UnityEngine;
using System;
using WitchDoctor.CoreResources.Utils.Disposables;
using System.Collections.Generic;

namespace WitchDoctor.CoreResources.Utils.Singleton
{
    #region Abstractions
    public interface IGenericSingleton
    {
        void InitSingleton();
        void CleanSingleton();
    }

    public interface IGenericSingleton<T> : IGenericSingleton
    {
        static T Instance { get; }
    }

    public abstract class NonMonoSingleton : IGenericSingleton
    {
        protected List<IDisposable> _disposables;

        public virtual void CleanSingleton()
        {
            throw new NotImplementedException();
        }

        public virtual void InitSingleton()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class MonoSingleton : MonoBehaviour, IGenericSingleton
    {
        protected List<IDisposable> _disposables;

        public virtual void InitSingleton()
        {
            throw new NotImplementedException();
        }

        public virtual void CleanSingleton()
        {
            throw new NotImplementedException();
        }
    }

    public abstract class DestroyableMonoSingleton : MonoBehaviour, IGenericSingleton
    {
        protected List<IDisposable> _disposables;

        public virtual void InitSingleton()
        {
            throw new NotImplementedException();
        }

        public virtual void CleanSingleton()
        {
            throw new NotImplementedException();
        }
    }
    #endregion

    #region Concretes
    /// <summary>
    /// A modified singleton without monobehavior. Needs to be set explicitly and will not create a new instance on Instance calls
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class NonMonoSingleton<T> : NonMonoSingleton, IGenericSingleton<T> where T : NonMonoSingleton<T>
    {
        #region Private Fields
        private static T _instance;
        #endregion

        #region Properties
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new UnassignedReferenceException($"Accessing {typeof(T).Name} before it has been set.");
                }

                return _instance;
            }
        }

        public static bool IsInstantiated => _instance != null;
        #endregion

        #region Public Methods
        public static TCustom SetInstanceType<TCustom>() where TCustom : T, new()
        {
            TCustom newInstance;

            if (_instance == null)
            {
                newInstance = new TCustom();
                _instance = newInstance;
                _instance.InitSingleton();
            }
            else
            {
                newInstance = _instance as TCustom;

                if (newInstance == null)
                {
                    _instance.CleanSingleton();
                    newInstance = new TCustom();
                    _instance = newInstance;
                    _instance.InitSingleton();
                }
            }

            return newInstance;
        }
        #endregion

        #region Overrides 
        public override void InitSingleton()
        {

        }

        public override void CleanSingleton()
        {
            if (_disposables != null)
            {
                _disposables.ClearDisposables();
                _disposables = null;
            }
        }
        #endregion
    }

    /// <summary>
    /// A standard singleton with monobehavior. Sets itself if called via Instance calls. Does not get destroyed on scene loads
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class MonoSingleton<T> : MonoSingleton, IGenericSingleton<T> where T : MonoSingleton<T>
    {
        #region Private Fields
        private static T _instance;
        #endregion

        #region Properties
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    //if (_instance == null)
                    //{
                    //    GameObject obj = new GameObject();
                    //    obj.name = typeof(T).Name;
                    //    _instance = obj.AddComponent<T>();
                    //}
                }

                return _instance;
            }
        }

        public static bool IsInstantiated => _instance != null;
        #endregion

        #region Overrides
        public override void InitSingleton()
        {
            if (Instance != null && GetInstanceID() != Instance.GetInstanceID())
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
        }

        public override void CleanSingleton()
        {
            if (_disposables != null)
            {
                _disposables.ClearDisposables();
                _disposables = null;
            }
        }

        protected void Awake()
        {
            InitSingleton();
        }

        protected void OnDestroy()
        {
            CleanSingleton();
        }
        #endregion
    }

    /// <summary>
    /// A modified singleton with monobehavior. Needs to be set explicitly and will not create a new instance on Instance calls
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class DestroyableMonoSingleton<T> : DestroyableMonoSingleton, IGenericSingleton<T> where T : DestroyableMonoSingleton<T>
    {
        #region Private Fields
        private static T _instance;
        #endregion

        #region Properties
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    throw new UnassignedReferenceException($"Accessing {typeof(T).Name} before it has been set.");
                    // Can do this but it's too expensive
                    // _instance = FindObjectOfType<T>();
                    //if (_instance == null)
                    //{
                    //    // Debug.LogWarning($"DestroyableSingleton of type {typeof(T)} does not exist in the current context");
                    //    return null;
                    //}
                }
                return _instance;
            }
        }

        public static bool IsInstantiated => _instance != null;
        #endregion

        #region Overrides
        public override void InitSingleton()
        {
            if (Instance != null && GetInstanceID() != Instance.GetInstanceID())
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this as T;
            }
        }

        public override void CleanSingleton()
        {
            if (_disposables != null)
            {
                _disposables.ClearDisposables();
                _disposables = null;
            }
        }

        protected void Awake()
        {
            InitSingleton();
        }

        protected void OnDestroy()
        {
            CleanSingleton();
        }
        #endregion

        #region Public Methods
        public static T CreateInstance(Transform parent = null)
        {
            if (_instance != null)
            {
                Debug.LogError($"Instance of {typeof(T)} already exists");
                return null;
            }

            var spawnedObj = new GameObject();
            spawnedObj.name = typeof(T).Name;

            if (parent != null)
            {
                spawnedObj.transform.SetParent(parent);
            }

            var typeObj = spawnedObj.AddComponent<T>();

            return typeObj;
        }
        #endregion
    }
    #endregion
}