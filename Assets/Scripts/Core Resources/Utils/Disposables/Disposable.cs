using System;

namespace WitchDoctor.CoreResources.Utils.Disposables
{
    public static class Disposables
    {
        public static IDisposable CreateWithState<T>(T state, Action<T> disposeAction)
        {
            return new Disposable<T>(state, disposeAction);
        }

        private class Disposable<T> : IDisposable
        {
            private T _state;
            private Action<T> _dispose;

            public Disposable(T state, Action<T> disposeAction)
            {
                _state = state;
                _dispose = disposeAction;
            }

            public void Dispose()
            {
                if (_dispose != null)
                {
                    Action<T> tmpDispose = _dispose;
                    _dispose = null;

                    tmpDispose(_state);
                    _state = default;
                }
            }
        }
    }
}