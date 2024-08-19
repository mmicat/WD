using UnityEngine;

namespace WitchDoctor.CoreResources.UIViews.BaseScripts
{
    public abstract class UIView : MonoBehaviour
    {
        public abstract void InitializeViewElements();
        public abstract void DeInitializeViewElements();
    }

    public abstract class UIView<T> : UIView where T : UIView<T>
    {
    }

}
