using System;
using System.Collections;
using UnityEngine;

namespace WitchDoctor.Utils
{
    public static class CoroutineUtils
    {
        public static IEnumerator LoadFromResources<T>(string path, Action<T> onCompleted) where T : UnityEngine.Object
        {
            ResourceRequest handle = Resources.LoadAsync<T>(path);

            yield return handle;

            onCompleted?.Invoke(handle.asset as T);
        }

        public static void CancelCoroutine(this Coroutine coroutine)
        {
            if (coroutine == null) return;

            CancelCoroutine(coroutine);
            coroutine = null;
        }
    }
}