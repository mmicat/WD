using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using WitchDoctor.CoreResources.UIViews.BaseScripts;

namespace WitchDoctor.GameResources.UI.MainMenu
{
    public class MainMenuView : UIView<MainMenuView>
    {
        public Button PlayButton;
        public Button SettingsButton;
        public Button QuitButton;

        public override void InitializeViewElements()
        {

        }

        public override void DeInitializeViewElements()
        {
            PlayButton.onClick.RemoveAllListeners();
            SettingsButton.onClick.RemoveAllListeners();
            QuitButton.onClick.RemoveAllListeners();
        }
    }
}
