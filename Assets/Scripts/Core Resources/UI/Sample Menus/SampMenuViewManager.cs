using System.Collections;
using System.Collections.Generic;
using WitchDoctor.CoreResources.UIViews.BaseScripts;
using UnityEngine;

public class SampMenuViewManager : UIViewManager<SampMenuViewManager, SampMenuView>
{
    #region Overrides
    protected override void InitializeManager()
    {
        base.InitializeManager();

        view.SampleButton1.onClick.AddListener(OnSampleButton1Clicked);
        view.SampleButton2.onClick.AddListener(OnSampleButton2Clicked);
    }

    protected override void DeInitializeManager()
    {
        base.DeInitializeManager();
    }

    public override void OnShowPanel()
    {
        Debug.Log("Panel Enabled");
    }

    public override void OnHidePanel()
    {
        Debug.Log("Panel Disabled");
    }
    #endregion

    #region Event Listeners
    private void OnSampleButton1Clicked()
    {
        Debug.Log("Sample Button 1 Clicked");
    }

    private void OnSampleButton2Clicked()
    {
        Debug.Log("Sample Button 2 Clicked");
    }
    #endregion
}
