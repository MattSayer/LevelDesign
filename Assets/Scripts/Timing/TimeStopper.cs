using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Timing
{
    public class TimeStopper : MonoBehaviour, ITimeStopper
    {
        private float _cachedTimeScale = 1;

        private Coroutine _stopTimeRoutine = null;

        #region Timing

        public void StopTime(float duration)
        {
            if(_stopTimeRoutine != null)
            {
                Debug.LogError("Attempted to stop time while time already stopped. This shouldn't happen");
                return;
            }

            _stopTimeRoutine = StartCoroutine(stopTime(duration));
        }

        #endregion

        #region Coroutines

        private IEnumerator stopTime(float duration)
        {
            _cachedTimeScale = Time.timeScale;
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = _cachedTimeScale;

            _stopTimeRoutine = null;
        }

        #endregion
    }

    public interface ITimeStopper
    {
        public void StopTime(float duration);
    }
}
