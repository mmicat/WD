using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchDoctor.CoreResources.Managers.CameraManagement;
using WitchDoctor.CoreResources.Managers.GeneralUtils;
using WitchDoctor.CoreResources.Utils.Singleton;
using WitchDoctor.GameResources.StateMachine;
using WitchDoctor.Managers.InputManagement;

public class GameStateMediator : DestroyableMonoSingleton<GameStateMediator>
{
    private GameStateMachine _fsm;

    public GameState CurrentState => _fsm.CurrentState;

    [Space(5)]
    
    [Header("Mediators")]
    [SerializeField]
    private CameraManager _cameraManager;
    [SerializeField]
    private InputManager _inputManager;
    [SerializeField]
    private UIMediator _uIMediator;
    [SerializeField]
    private SoundManager _soundManager;

    #region Overrides
    public override void InitSingleton()
    {
        base.InitSingleton();

        _cameraManager.gameObject.SetActive(true);
        _inputManager.gameObject.SetActive(true);
        _uIMediator.gameObject.SetActive(true);
        _soundManager.gameObject.SetActive(true);

        _fsm = new GameStateMachine();
        _fsm.GoToState<GameState_Menu>();
    }

    public override void CleanSingleton()
    {
        base.CleanSingleton();
    }
    #endregion

    #region Public Methods
    public void StartGame()
    {
        if (CurrentState.GetType().Equals(typeof(GameState_Menu)))
            _fsm.GoToState<GameState_Level1>();
    }
    #endregion
}
