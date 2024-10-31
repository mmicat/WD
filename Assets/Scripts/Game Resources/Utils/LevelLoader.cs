using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WitchDoctor.CoreResources.Managers.CameraManagement;
using WitchDoctor.CoreResources.Utils.Singleton;

public class LevelLoader : DestroyableMonoSingleton<LevelLoader>
{
    [SerializeField]
    private CinemachineVirtualCamera[] _allVirtualCameras;
    [SerializeField]
    private GameObject _cameraTriggerParent;
    [SerializeField]
    private GameObject _characterSetParent;

    #region Overrides
    public override void InitSingleton()
    {
        base.InitSingleton();

        CameraManager.Instance.SetupCameraSystem(_allVirtualCameras);
        
        _cameraTriggerParent.SetActive(true);
        _characterSetParent.SetActive(true);
    }

    public override void CleanSingleton()
    {
        base.CleanSingleton();
    }
    #endregion
}
