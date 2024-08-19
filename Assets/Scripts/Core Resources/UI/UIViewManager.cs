using System;
using System.Collections;
using UnityEngine;

namespace WitchDoctor.CoreResources.UIViews.BaseScripts
{
    public enum UIViewType
    {
        None, // Don't try loading this unless you want to hate yourself ._.
        MainMenu,
        Loading,
        Settings,
        HUDMenu,
        Chat,
    }


    public enum PopupMessageTypes
    {
        // Errors
        UserRevoked,
        UserNotFound,
        UserTransferred,
        ServerConnectionFailed,
        CustomLoginError,
        RegisterUserError,
        PasswordMatchError,
        NoInternetNetwork,
        PlayerTimedOut,
        LobbyFull,
        LobbyExpire,
        InvalidCode,
        SocketClosed,
        ConsoleNetworkLoginError,

        // Info Messages
        NetworkConnected,
        SteamAutoLogin,
        StartingCustomMatch,
        VerifyingCode,
        CodeAccepted,
        ClientServerDesync,
        LobbyTimeExtend,
        DefaultTeamSelection,

        // Others
        Quit,

        //Shop Messages
        Buy,
        InsufficientCheese,
        InsufficientBread,
        InsufficientTicket,
        TicketConfirmation,

        //Achievement reward Messages
        GotReward
    }

    [RequireComponent(typeof(UIView))]
    public abstract class UIViewManager : MonoBehaviour
    {
        [SerializeField]
        protected UIViewType ViewType;
        protected bool _isInitialized = false;
        private GameObject _container;
        protected CanvasGroup _canvasGroup;

        protected GameObject Container
        {
            get 
            {
                if (_container == null)
                    _container = transform.GetChild(0).gameObject;

                return _container;
            }
        }

        public bool IsEnabled => Container != null && Container.activeInHierarchy;
        public bool IsInitialized => _isInitialized;
        public bool IsInteractable => _canvasGroup != null && _canvasGroup.interactable;
        public UIViewType AssignedViewType { get => ViewType; }

        protected abstract void InitializeManager();
        protected abstract void DeInitializeManager();
        public abstract void ShowPanel();
        public abstract void HidePanel();
        public abstract void OnShowPanel();
        public abstract void OnHidePanel();

        public virtual void SetMenuInteractability(bool enableInteraction, bool ignoreParentGroups = false)
        {
            _canvasGroup.interactable = enableInteraction;
            _canvasGroup.blocksRaycasts = enableInteraction;
            _canvasGroup.ignoreParentGroups = ignoreParentGroups;
        }
    }

    public abstract class UIViewManager<TViewManager, TView> : UIViewManager
        where TViewManager : UIViewManager<TViewManager, TView>
        where TView : UIView<TView>
    {
        protected string Name => nameof(TViewManager);
        [SerializeField]
        protected TView view;

        protected virtual void OnEnable()
        {
            if(!view)
                view = GetComponent<TView>();

            InitializeManager();
        }

        protected virtual void OnDisable()
        {
            DeInitializeManager();
        }

        protected override void InitializeManager()
        {
            if(!_isInitialized)
            {
                if (!Container)
                    throw new ArgumentNullException($"No container exists for {Name}");

                Container.name = "Panel - Initialized";
                view.InitializeViewElements();

                _canvasGroup = GetComponent<CanvasGroup>();
                if (!_canvasGroup)
                {
                    Debug.LogWarning($"Canvas group does not exist on the menu {typeof(TViewManager)}. Creating a new one");
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }

                HidePanel();

                _isInitialized = true;
            }
        }

        protected override void DeInitializeManager()
        {
            if(_isInitialized)
            {
                view.DeInitializeViewElements();
                StopAllCoroutines();
                _isInitialized = false;
            }
        }

        public override void ShowPanel()
        {
            if (Container.activeSelf)
                return;

            Container.SetActive(true);
            Container.name = "Panel - Showing";

            if (IsEnabled && IsInitialized)
            {
                OnShowPanel();
                // InputManager.onControlSchemeChange += ResetUINavigation;
            }
        }

        public override void HidePanel()
        {
            if (IsEnabled)
            {
                /*if (!GameStateManager.ApplicationQuitting)
                {
                    InputManager.onControlSchemeChange -= ResetUINavigation;
                }*/
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.ignoreParentGroups = false;

                OnHidePanel();
            }

            Container.SetActive(false);
            Container.name = "Panel - Hidden";
        }

        public override void OnShowPanel()
        {
            
        }

        public override void OnHidePanel()
        {
            
        }

        public virtual void ResetUINavigation()
        {
            if (!IsInteractable || !IsEnabled)
                return;
        }

        protected virtual void SetManualUISelection(GameObject obj, bool tutorialOverride = false)
        {
            //if (!GameStateManager.ApplicationQuitting && !InputManager.Instance.IsKeyboardAndMouse)
            //    GameConstants.SetUISelectionEvent?.Invoke(obj, tutorialOverride);
        }

        protected virtual void ShowChildPanel(UIViewManager obj, bool blockParentInteraction = false)
        {
            if (IsEnabled && IsInitialized)
                StartCoroutine(ShowChildPanelCoroutine(obj, blockParentInteraction));
        }

        private IEnumerator ShowChildPanelCoroutine(UIViewManager obj, bool blockParentInteraction)
        {
            yield return new WaitUntil(() => obj.IsInitialized);
            obj.ShowPanel();

            if (blockParentInteraction)
            {
                obj.SetMenuInteractability(true, true);
                SetMenuInteractability(false);
            }
        }
    }
}
