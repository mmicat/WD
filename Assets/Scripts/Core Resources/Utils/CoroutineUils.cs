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
    }

    public class WaitUntilForSeconds : CustomYieldInstruction
    {
        float pauseTime;
        float timer;
        Func<bool> myChecker;

        public WaitUntilForSeconds(Func<bool> myChecker, float waitTime)
        {
            this.myChecker = myChecker;
            timer = waitTime;
        }

        public override bool keepWaiting
        {
            get
            {
                bool checkThisTurn = myChecker();

                if (checkThisTurn || timer <= 0)
                {
                    return false;
                }

                timer -= Time.deltaTime;
                return true;
            }
        }
    }
}