using System.Collections;
using System.Collections.Generic;
using WitchDoctor.CoreResources.UIViews.BaseScripts;
using UnityEngine;
using UnityEngine.UI;

public class SampMenuView : UIView<SampMenuView>
{
    public Button SampleButton1;
    public Button SampleButton2;

    public override void DeInitializeViewElements()
    {
    }

    public override void InitializeViewElements()
    {
        SampleButton1.onClick.RemoveAllListeners();
        SampleButton2.onClick.RemoveAllListeners();
    }
}
