using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WitchDoctor.CoreResources.UIViews.BaseScripts;
using WitchDoctor.CoreResources.Utils.Singleton;
using WitchDoctor.GameResources.UI.HUD;
using WitchDoctor.GameResources.UI.Loading;
using WitchDoctor.GameResources.UI.MainMenu;
using WitchDoctor.GameResources.UI.Settings;
using WitchDoctor.Utils;

public class UIMediator : DestroyableMonoSingleton<UIMediator>
{
    [SerializeField]
    private MainMenuManager _mainMenuManager;
    [SerializeField]
    private SettingsManager _settingsManager;
    [SerializeField]
    private HUDManager _hUDManager;
    [SerializeField]
    private LoadingManager _loadingManager;

    private Dictionary<UIViewType, UIViewManager> _viewManagers;

    #region Overrides
    public override void InitSingleton()
    {
        base.InitSingleton();

        _viewManagers = new Dictionary<UIViewType, UIViewManager> {
            {UIViewType.MainMenu, _mainMenuManager},
            {UIViewType.Settings, _settingsManager},
            {UIViewType.HUDMenu, _hUDManager},
            {UIViewType.Loading, _loadingManager}
        };

        var keyList = _viewManagers.Keys.ToList();

        for (int i = 0; i < _viewManagers.Count; i++)
        {
            var mngr = _viewManagers[keyList[i]];
            mngr.gameObject.SetActive(true);
            mngr.HidePanel();
        }
    }

    public override void CleanSingleton()
    {
        base.CleanSingleton();
    }
    #endregion

    #region Public Methods
    public void ShowMenu(UIViewType viewType, bool toggleMenus = true)
    {
        if (toggleMenus)
        {
            foreach (var mng in _viewManagers.Values)
            {
                mng.HidePanel();
            }
        }

        if (_viewManagers.TryGetValue(viewType, out var manager))
        {
            manager.ShowPanel();
        }
        else
            Debug.LogError("Manager doesn't exist");
    }

    public void ResetMenus()
    {
        foreach (var mng in _viewManagers.Values)
        {
            mng.HidePanel();
        }
    }
    #endregion
}
