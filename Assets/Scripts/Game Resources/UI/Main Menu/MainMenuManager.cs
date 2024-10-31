using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchDoctor.CoreResources.UIViews.BaseScripts;

namespace WitchDoctor.GameResources.UI.MainMenu
{
    public class MainMenuManager : UIViewManager<MainMenuManager, MainMenuView>
    {
        #region Overrides
        protected override void InitializeManager()
        {
            base.InitializeManager();

            view.PlayButton.onClick.AddListener(OnPlay);
            view.SettingsButton.onClick.AddListener(OnSettings);
            view.QuitButton.onClick.AddListener(OnQuit);
        }

        protected override void DeInitializeManager()
        {
            base.DeInitializeManager();
        }

        public override void OnShowPanel()
        {
            base.OnShowPanel();
        }

        public override void OnHidePanel()
        {
            base.OnHidePanel();
        }
        #endregion

        #region Listeners
        private void OnPlay()
        {
            // Play The Game
            GameStateMediator.Instance.StartGame();
        }

        private void OnSettings()
        {
            // Open Settings
        }

        private void OnQuit()
        {
            //  On Quit
        }
        #endregion
    }
}
