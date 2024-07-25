using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchDoctor.CoreResources.Utils.Singleton;
using WitchDoctor.GameResources.CharacterScripts.Player;

namespace WitchDoctor.GameResources
{
    public class AppHandler : MonoSingleton<AppHandler>
    {
        // Remove this later we're not going to directly reference the player manager from the app handler
        [SerializeField] private PlayerManager _playerManager;



        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            
        }

        public override void CleanSingleton()
        {
            base.CleanSingleton();
        }
        #endregion
    }
}