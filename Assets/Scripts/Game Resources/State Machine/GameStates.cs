using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using WitchDoctor.CoreResources.StateMachine;
using WitchDoctor.CoreResources.UIViews.BaseScripts;

namespace WitchDoctor.GameResources.StateMachine
{
    public class GameState : StateHistory<GameStateMachine, GameState>
    {
    
    }

    public class GameState_Entry : GameState
    {
        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }

    public class GameState_Menu : GameState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            SceneManager.LoadScene(1);
            UIMediator.Instance.ShowMenu(UIViewType.MainMenu);
        }

        public override void OnExit()
        {
            if (NextState.GetType() == typeof(GameState_Level1))
            {
                UIMediator.Instance.ShowMenu(UIViewType.HUDMenu);
            }

            base.OnExit();
        }
    }

    public class GameState_Level1 : GameState
    {
        public override void OnEnter()
        {
            base.OnEnter();

            if (AppHandler.Instance.LoadTestLevel)
                SceneManager.LoadScene(2);
            else
                SceneManager.LoadScene(3);
        }

        public override void OnExit()
        {
            if (NextState.GetType().Equals(typeof(GameState_Menu)))
            {
                UIMediator.Instance.ShowMenu(UIViewType.MainMenu);
            }

            base.OnExit();
        }
    }

    public class GameState_Exit : GameState
    {
        public override void OnEnter()
        {
            base.OnEnter();
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}