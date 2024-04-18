using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Audio;
using AmalgamGames.Conditionals;
using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.Timing;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
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
        [Space]
        [Title("Audio")]
        [SerializeField] private string _slowmoActivateClipID;
        [SerializeField] private string _slowmoDeactivateClipID;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getTimeScaler;
        [SerializeField] private DependencyRequest _getAudioManager;

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
        private ITimeScaler _timeScaler;
        private IAudioManager _audioManager;

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

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(_rocketObject);
            
            SubscribeToInput();
            SubscribeToEvents();

            _getTimeScaler.RequestDependency(ReceiveTimeScaler);
            _getAudioManager.RequestDependency(ReceiveAudioManager);

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

        // DO NOT REMOVE - is being called dynamically as an event trigger
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

        #region Audio

        private void PlayActivateSound()
        {
            _audioManager.PlayAudioClip(new AudioPlayRequest { audioClipID = _slowmoActivateClipID, audioType = Audio.AudioType.Flat });
        }

        private void PlayDeactivateSound()
        {
            _audioManager.PlayAudioClip(new AudioPlayRequest { audioClipID = _slowmoDeactivateClipID, audioType = Audio.AudioType.Flat });
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
                PlayActivateSound();

                _isActive = true;

                StopAllCoroutines();

                _drainRoutine = StartCoroutine(drainSlowMo());
                OnSlowmoStart?.Invoke();

                float timeScaleDiff = (Time.timeScale - _slowMoTimeScale);

                _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, _slowMoTimeScale, _timeScaleTransitionTime * timeScaleDiff,
                        (value) => 
                        {
                            _timeScaler.SetTimeScale(value);
                        },
                        () =>
                        {
                            _timeScaler.SetTimeScale(_slowMoTimeScale);
                            _lerpTimescaleRoutine = null;
                        }
                    ));
            }
        }

        public void ForceActivateSlowmo(float transitionTime = 0)
        {
            if(transitionTime <= 0)
            {
                transitionTime = _timeScaleTransitionTime;
            }

            _isActive = true;

            PlayActivateSound();

            StopAllCoroutines();

            OnSlowmoStart?.Invoke();

            float timeScaleDiff = (Time.timeScale - _slowMoTimeScale);

            _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, _slowMoTimeScale, transitionTime * timeScaleDiff,
                    (value) =>
                    {
                        _timeScaler.SetTimeScale(value);
                    },
            () =>
            {
                        _timeScaler.SetTimeScale(_slowMoTimeScale);
                        _lerpTimescaleRoutine = null;
                    }
            ));
        }

        public void EndSlowmo()
        {
            if (_isActive)
            {
                PlayDeactivateSound();

                StopAllCoroutines();

                float timeScaleDiff = (1 - Time.timeScale);

                _lerpTimescaleRoutine = StartCoroutine(Tools.lerpFloatOverTimeUnscaled(Time.timeScale, 1, _timeScaleTransitionTime * timeScaleDiff,
                        (value) =>
                        {
                            _timeScaler.SetTimeScale(value);
                        },
                        () =>
                        {
                            _timeScaler.SetTimeScale(1);
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
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if(!conditionalCheck)
            {
                return;
            }
            ReenableSlowmo();
        }

        public void CancelSlowmo()
        {
            if(_isActive)
            {
                StopAllCoroutines();

                PlayDeactivateSound();

                _timeScaler.SetTimeScale(1);

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
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if(!conditionalCheck)
            {
                return;
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

        #region Dependencies

        private void ReceiveTimeScaler(object rawObj)
        {
            _timeScaler = rawObj as ITimeScaler;
        }

        private void ReceiveAudioManager(object rawObj)
        {
            _audioManager = rawObj as IAudioManager;
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
        public void ForceActivateSlowmo(float transitionTime = 0);
        public void EndSlowmo();
        public void CancelSlowmo();
        public void IgnoreInput(bool ignoreInput);
    }
}