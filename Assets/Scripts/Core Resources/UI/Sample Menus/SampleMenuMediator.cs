using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SampleMenuMediator : MonoBehaviour
{
    public SampMenuViewManager viewManager;
    public Button EnableSampleMenuButton;

    private void OnEnable()
    {
        EnableSampleMenuButton.onClick.AddListener(ToggleSampleMenu);
    }

    private void OnDisable()
    {
        EnableSampleMenuButton.onClick.RemoveAllListeners();
    }

    public void ToggleSampleMenu()
    {
        if (viewManager.IsEnabled)
            viewManager.HidePanel();
        else
            viewManager.ShowPanel();
    }
}
