using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AmalgamGames.Conditionals;
using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace AmalgamGames.Abilities
{
    public class Slowmo : MonoBehaviour, IRestartable, IRespawnable, IPausable, ISlowmo
    {
        [Title("Events")]
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _cancelEvents;
        [FoldoutGroup("Events")][SerializeField] private DynamicEvent[] _reenableEvents;
        [Title("Settings")]
        [SerializeField] private float _juiceDrainRatePerSecond = 10;
        [SerializeField] private float _slowMoTimeScale = 0.5f;
        [SerializeField] private float _timeScaleTransitionTime = 0.5f;
        [Space]
        [Title("Components")]
        [SerializeField] private Transform _rocketObject;
        [SerializeField] private SharedFloatValue _juice;

        // CONSTANTS
        private float PHYSICS_TIMESTEP;

        // EVENTS
        public event Action OnSlowmoStart;
        public event Action OnSlowmoEnd;

        // STATE
        private bool _isSubscribedToEvents = false;
        private bool _isSubscribedToInput = false;

        private bool _isActive = false;
        private bool _canActivate = false;
        private bool _isPaused = false;
        
        private bool _isLocked = false;
        private bool _ignoreInput = false;
        private float _cachedInput = 0;

        private bool _cachedIsActive = false;

        // COROUTINES
        private Coroutine _drainRoutine = null;
        private Coroutine _lerpTimescaleRoutine = null;

        // COMPONENTS
        private IInputProcessor _inputProcessor;

        #region Lifecycle

        // TODO: move to proper classes
        class LevelConfig
        {
            public string LevelID { get; set; }
            public string LevelName { get; set; }
            public Image Thumbnail { get; set; }    
            public int[] StarPointThresholds { get; set; }
            public float ThresholdTime {  get; set; }
            public int ThresholdRespawns { get; set; }
            public int ThresholdLaunches { get; set; }
            public int MaxJuice {  get; set; }
            public Scene SceneFile { get; set; }

        }

        class LevelSaveData
        {
            public string LevelID { get; set; }
            public bool HasBeenCompleted { get; set; }
            public int NumAttempts { get; set; }
            public LevelCompletionStats LevelCompletionStats { get; set; }
        }

        class LevelCompletionStats
        {
            public int StarCount {  get; set; }
            public int Score {  get; set; }
            public float TimeTaken { get; set; }
            public int BonusPoints { get; set; }
            public int NumRespawns { get; set; }
            public int NumLaunches { get; set; }
            public float JuiceRemaining {  get; set; }
            public RocketCharacter Character { get; set; }
        }

        enum RocketCharacter
        {
            Heavy,
            Technical,
            AllRounder
        }

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(_rocketObject);
            
            SubscribeToInput();
            SubscribeToEvents();

            PHYSICS_TIMESTEP = Time.fixedDeltaTime;

            /*
            LevelConfig testLevel = new LevelConfig() { LevelID = "1", LevelName = "Test" , MaxJuice = 100, StarPointThresholds = new int[] { 1000,2000,3000 }, ThresholdLaunches = 5, ThresholdRespawns = 5, ThresholdTime = 90 };

            LevelCompletionStats stats = new LevelCompletionStats() { BonusPoints = 100, Character = RocketCharacter.AllRounder, JuiceRemaining = 23, NumLaunches = 4, NumRespawns = 3, Score = 5000, StarCount = 2, TimeTaken = 65 };

            LevelSaveData saveData = new LevelSaveData() { HasBeenCompleted = true, LevelID = "1", NumAttempts = 3, LevelCompletionStats = stats };

            Dictionary<string, object> flattenedDictionary = Tools.GetPropertyDictionary(new object[] { testLevel, saveData });

            foreach(string key in flattenedDictionary.Keys)
            {
                Debug.Log($"{key}: {flattenedDictionary[key]}");
            }
            */

        }

       
        private void OnEnable()
        {
            SubscribeToInput();
            SubscribeToEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
            UnsubscribeFromEvents();
        }

        #endregion

        #region Respawning/restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    _canActivate = false;
                    CancelSlowmo();
                    break;
            }
        }

        public void OnRestart()
        {
            CancelSlowmo();
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
            ApplyCachedSlowmo();
        }

        #endregion

        #region Charging/Launching

        public void IgnoreInput(bool ignoreInput)
        {
            _ignoreInput = ignoreInput;
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            EndSlowmo();

            // Enable slowmo on first launch after respawn/restart
            if (!_canActivate)
            {
                _canActivate = true;
            }
        }

        private void OnSlowmoInputChange(float inputVal)
        {
            _cachedInput = inputVal;

            if (_ignoreInput)
            {
                _cachedIsActive = inputVal > 0;
                return;
            }
            if (_isPaused)
            {
                _cachedIsActive = inputVal > 0;
            }
            else
            {
                if (inputVal > 0 && !_isActive)
                {
                    ActivateSlowmo();
                }
                else if (inputVal == 0 && _isActive)
                {
                    EndSlowmo();
                }
            }

            // Remove lock when trigger is released
            if(inputVal == 0)
            {
                _isLocked = false;
            }
        }

        #endregion

        #region Slow-mo

        private void ApplyCachedSlowmo()
        {
            if(_cachedIsActive && !_isActive)
            {
                ActivateSlowmo();
            }
            else if (!_cachedIsActive && _isActive)
            {
                EndSlowmo();
            }
        }

        private void ActivateSlowmo()
        {
            if(!_isActive && _canActivate && HasJuice() && !_isLocked)
            {
                _isActive = true;

                StopAllCoroutines();

                _drainRoutine = StartCoroutine(drainSlowMo());
                OnSlowmoStart?.Invoke();

                float timeScaleDiff = (Time.timeScale - _slowMoTimeScale) / (1 - _slowMoTimeScale);

                _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, _slowMoTimeScale, _timeScaleTransitionTime * timeScaleDiff,
                        (value) => 
                        {
                            Time.timeScale = value;
                            Time.fixedDeltaTime = PHYSICS_TIMESTEP * value;
                        },
                        () =>
                        {
                            Time.timeScale = _slowMoTimeScale;
                            Time.fixedDeltaTime = PHYSICS_TIMESTEP * _slowMoTimeScale;
                            _lerpTimescaleRoutine = null;
                        }
                    ));
            }
        }

        public void ForceActivateSlowmo()
        {
            _isActive = true;

            StopAllCoroutines();

            OnSlowmoStart?.Invoke();

            float timeScaleDiff = (Time.timeScale - _slowMoTimeScale) / (1 - _slowMoTimeScale);

            _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, _slowMoTimeScale, _timeScaleTransitionTime * timeScaleDiff,
                    (value) =>
                    {
                        Time.timeScale = value;
                        Time.fixedDeltaTime = PHYSICS_TIMESTEP * value;
                    },
                    () =>
                    {
                        Time.timeScale = _slowMoTimeScale;
                        Time.fixedDeltaTime = PHYSICS_TIMESTEP * _slowMoTimeScale;
                        _lerpTimescaleRoutine = null;
                    }
                ));
        }

        public void EndSlowmo()
        {
            if (_isActive)
            {
                StopAllCoroutines();

                float timeScaleDiff = (1 - Time.timeScale) / (1 - _slowMoTimeScale);

                _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, 1, _timeScaleTransitionTime * timeScaleDiff,
                        (value) =>
                        {
                            Time.timeScale = value;
                            Time.fixedDeltaTime = value * PHYSICS_TIMESTEP;
                        },
                        () =>
                        {
                            Time.timeScale = 1;
                            Time.fixedDeltaTime = PHYSICS_TIMESTEP;
                            _lerpTimescaleRoutine = null;
                        }
                    ));


                _isActive = false;

                OnSlowmoEnd?.Invoke();

            }
        }

        private void ReenableSlowmo()
        {
            // Re-enable slowmo on first launch after respawn/restart
            if (!_canActivate)
            {
                _canActivate = true;
            }
        }

        private void ReenableSlowmoWithParam(DynamicEvent sourceEvent, object param)
        {
            foreach (ConditionalCheck conditional in sourceEvent.Conditionals)
            {
                if (!conditional.ApplyCheck(param))
                {
                    return;
                }
            }
            ReenableSlowmo();
        }

        public void CancelSlowmo()
        {
            if(_isActive)
            {
                StopAllCoroutines();

                Time.fixedDeltaTime = PHYSICS_TIMESTEP;
                Time.timeScale = 1;

                OnSlowmoEnd?.Invoke();
                _isActive = false;

                // If trigger is currently held down, lock slowmo until trigger is fully released
                if(_cachedInput > 0)
                {
                    _isLocked = true;
                }
            }
        }

        private void OnCancelEventWithParam(DynamicEvent sourceEvent, object param)
        {
            foreach (ConditionalCheck conditional in sourceEvent.Conditionals)
            {
                if (!conditional.ApplyCheck(param))
                {
                    return;
                }
            }
            CancelSlowmo();
        }

        #endregion

        #region Juice

        private bool HasJuice()
        {
            return _juice.CanSubtract(Time.unscaledDeltaTime * _juiceDrainRatePerSecond);
        }

        #endregion

        #region Coroutines

        private IEnumerator drainSlowMo()
        {
            while(HasJuice())
            {
                _juice.SubtractValue(Time.unscaledDeltaTime * _juiceDrainRatePerSecond);
                yield return null;
            }
            _drainRoutine = null;
            EndSlowmo();
        }

        #endregion

        #region Subscriptions

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowmoInputChange -= OnSlowmoInputChange;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnSlowmoInputChange += OnSlowmoInputChange;
                _isSubscribedToInput = true;
            }
        }

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                Tools.SubscribeToDynamicEvents(_cancelEvents, CancelSlowmo, OnCancelEventWithParam);

                Tools.SubscribeToDynamicEvents(_reenableEvents, ReenableSlowmo, ReenableSlowmoWithParam);

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                Tools.UnsubscribeFromDynamicEvents(_cancelEvents);
                Tools.UnsubscribeFromDynamicEvents(_reenableEvents);

                _isSubscribedToEvents = false;
            }
        }


        #endregion

    }

    public interface ISlowmo
    {
        public void ForceActivateSlowmo();
        public void EndSlowmo();
        public void CancelSlowmo();
        public void IgnoreInput(bool ignoreInput);
    }
}