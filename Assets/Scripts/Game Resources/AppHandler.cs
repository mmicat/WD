using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchDoctor.CoreResources.Utils.Singleton;
using WitchDoctor.GameResources.CharacterScripts.Player;

namespace WitchDoctor.GameResources
{
    public class AppHandler : MonoSingleton<AppHandler>
    {
        [SerializeField]
        private GameStateMediator _mediator;

        #region Overrides
        public override void InitSingleton()
        {
            base.InitSingleton();

            _mediator.gameObject.SetActive(true);
        }

        public override void CleanSingleton()
        {
            base.CleanSingleton();
        }
        #endregion
    }
}