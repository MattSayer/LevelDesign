using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Timing
{
    public class Countdown : MonoBehaviour, IPausable, IRespawnable
    {
        [Title("Settings")]
        [SerializeField] private ValueUpdateMode _updateMode;

        // Events
        public event Action OnCountdownStarted;
        public event Action OnCountdownFinished;
        public event Action OnCountdownCanceled;
        public event Action<float> OnCountdownUpdated;

        // State
        private bool _isPaused = false;

        // Coroutines
        private Coroutine _countdownRoutine = null;

        #region Timing

        public void StartCountdown(float duration)
        {
            if(_countdownRoutine != null)
            {
                Debug.LogError("Cannot start countdown while countdown is in progress");
                return;
            }

            _countdownRoutine = StartCoroutine(countdownTimer(duration));

            OnCountdownStarted?.Invoke();
        }

        public void CancelTimer()
        {
            if(_countdownRoutine != null)
            {
                StopCoroutine(_countdownRoutine);
                OnCountdownCanceled?.Invoke();
            }
        }
        
        #endregion

        #region Pausing

        public void Pause()
        {
            PauseTimer();
        }

        public void Resume()
        {
            ResumeTimer();
        }

        private void PauseTimer()
        {
            _isPaused = true;
        }

        private void ResumeTimer()
        {
            _isPaused = false;
        }


        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    CancelTimer();
                    break;
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator countdownTimer(float duration)
        {
            float countdownLerp = 0;
            int intCheck = 0;

            OnCountdownUpdated?.Invoke(duration);

            while (countdownLerp < duration)
            {
                switch(_updateMode)
                {
                    case ValueUpdateMode.Decimal:
                        OnCountdownUpdated?.Invoke(duration - countdownLerp);
                        break;
                    case ValueUpdateMode.Integer:
                        int currentInt = Mathf.FloorToInt(countdownLerp);
                        if(intCheck - currentInt < 0)
                        {
                            intCheck = currentInt;
                            OnCountdownUpdated?.Invoke(duration - intCheck);
                        }
                        break;
                }

                // Don't progress countdown if paused
                if (!_isPaused)
                {
                    countdownLerp += Time.deltaTime;
                }
                yield return null;
            }

            OnCountdownUpdated?.Invoke(0);

            OnCountdownFinished?.Invoke();

            _countdownRoutine = null;
        }

        #endregion

        private enum ValueUpdateMode
        {
            Decimal,
            Integer,
        }
    }
}