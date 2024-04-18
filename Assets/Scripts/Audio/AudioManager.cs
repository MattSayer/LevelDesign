using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.Timing;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace AmalgamGames.Audio
{

    public class AudioManager : ManagedBehaviour, IAudioManager, IPausable, IRespawnable
    {
        [Title("Dependency Provider")]
        [SerializeField] private DependencyRequest _getAudioManager;
        [Space]
        [Title("Dependency Request")]
        [SerializeField] private DependencyRequest _getPlayerPrefsCache;
        [Space]
        [Title("Spatial Audio")]
        [SerializeField] private int _spatialPoolSize = 10;
        [SerializeField] private GameObject _spatialTemplate;
        [Space]
        [Title("Proxy Audio Sources")]
        [SerializeField] private int _proxyPoolSize = 5;
        [SerializeField] private GameObject _proxyTemplate;
        [Space]
        [Title("UI Audio")]
        [SerializeField] private int _uiPoolSize = 5;
        [SerializeField] private GameObject _uiTemplate;
        [Space]
        [Title("Music")]
        [SerializeField] private AudioSource _musicAudioSource;
        [Space]
        [Title("Cutscene Audio")]
        [SerializeField] private List<AudioSource> _cutsceneAudioSources;
        [Space]
        [Title("Updating")]
        [SerializeField] private float _parentUpdateRate = 0.1f;
        [Space]
        [Title("Fading")]
        [SerializeField] private float _crossfadeTime = 1f;
        [SerializeField] private float _fadeTime = 0.25f;
        [Space]
        [Title("Audio Mixers")]
        [SerializeField] private AudioMixerGroup _masterMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;
        [SerializeField] private AudioMixerGroup _effectsMixerGroup;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [Title("Audio database")]
        [SerializeField] private AudioDatabase _audioDatabase;
        [Space]
        [Title("Failsafes")]
        [SerializeField] private float _playClipBuffer = 0.1f;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getTimeScaler;

        public float PlayerEffectsVolume { get { return _playerMasterVolume * _playerEffectsVolume; } }
        public float GlobalEffectsVolume { get { return Globals.EFFECTS_MULTIPLIER; } }


        // Singleton
        private static AudioManager _instance;

        // STATE
        private bool _isSubscribedToDependencyRequests = false;
        private bool _isSubscribedToPlayerPrefsCache = false;

        // Spatial audio
        private List<AudioSource> _allSpatialAudioSources;
        private Queue<AudioSource> _spatialAudioSourcePool;
        private List<AudioSource> _activeSpatialAudioSources;

        // UI audio
        private List<AudioSource> _allUIAudioSources;
        private Queue<AudioSource> _uiAudioSourcePool;
        private List<AudioSource> _activeUIAudioSources;

        // Proxy audio sources
        private List<AudioSource> _allProxyAudioSources;
        private Queue<AudioSource> _proxyAudioSourcePool;
        private List<AudioSource> _activeProxyAudioSources;
        private Dictionary<ProxyAudioSource, AudioSource> _proxyAudioLookup;
        private Dictionary<AudioSource, ProxyAudioSource> _audioProxyLookup;

        // Gameobjects and Transforms
        private Dictionary<AudioSource, GameObject> _sourceGOs;
        private Dictionary<AudioSource, Transform> _sourceTransforms;

        // Coroutines
        private Dictionary<AudioSource, Coroutine> _sourceCoroutines;
        private Coroutine _musicFadeRoutine = null;
        private Dictionary<AudioSource, Coroutine> _fadeRoutines;
        private Dictionary<AudioSource, float> _fadeTargetVolume;

        // Templates
        private AudioSource _proxyTemplateSource;
        private AudioSource _spatialTemplateSource;
        private AudioSource _uiTemplateSource;

        // Failsafes
        private Dictionary<AudioClip, float> _clipLastPlayed;

        // Audio settings

        // Player preferences
        private float _playerMusicVolume;
        private float _playerEffectsVolume;
        private float _playerUIVolume;
        private float _playerMasterVolume;

        // PlayerPrefsCache
        private PlayerPrefsCache _playerPrefsCache;

        // Music
        private float _musicMultiplier = 1f;

        // Pausing
        private bool _isPaused = false;
        private List<AudioSource> _pausedAudioSources;

        // Time scaling
        private ITimeScaler _timeScaler;

        // Audio database
        private Dictionary<string, AudioDatabaseEntry> _audioDictionary;
        private Dictionary<AudioClip, AudioDatabaseEntry> _clipLookup;

        #region Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            InitialiseAudioDictionary();

            SubscribeToDependencyRequests();

            InitialiseAudioSources();

        }

        
        private void Start()
        {
            _getPlayerPrefsCache.RequestDependency(ReceivePlayerPrefsCache);
            _getTimeScaler.RequestDependency(ReceiveTimeScaler);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToDependencyRequests();
            SubscribeToPlayerPrefsCache();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromDependencyRequests();
            UnsubscribeFromPlayerPrefsCache();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromDependencyRequests();
            UnsubscribeFromPlayerPrefsCache();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            foreach (ProxyAudioSource proxy in _proxyAudioLookup.Keys.ToList())
            {
                proxy.Update();
            }

            CheckForIdleAudioSources();
        }

        #endregion

        #region Initialisation

        private void InitialiseAudioDictionary()
        {
            _audioDictionary = new Dictionary<string, AudioDatabaseEntry>();
            _clipLookup = new Dictionary<AudioClip, AudioDatabaseEntry>();

            foreach(AudioDatabaseEntry entry in _audioDatabase.Entries)
            {
                if(_audioDictionary.ContainsKey(entry.audioClipID))
                {
                    Debug.LogError("Multiple audio database entries with the same ID");
                    continue;
                }

                if(_clipLookup.ContainsKey(entry.clip))
                {
                    Debug.LogError("Multiple audio database entries with the same audio clip");
                    continue;
                }

                _audioDictionary[entry.audioClipID] = entry;
                _clipLookup[entry.clip] = entry;
            }


        }
        private void InitialiseAudioSources()
        {
            _sourceGOs = new Dictionary<AudioSource, GameObject>();
            _sourceTransforms = new Dictionary<AudioSource, Transform>();
            _sourceCoroutines = new Dictionary<AudioSource, Coroutine>();
            _fadeRoutines = new Dictionary<AudioSource, Coroutine>();
            _fadeTargetVolume = new Dictionary<AudioSource, float>();
            _pausedAudioSources = new List<AudioSource>();

            _clipLastPlayed = new Dictionary<AudioClip, float>();

            // Spatial audio setup

            _allSpatialAudioSources = new List<AudioSource>();
            _spatialAudioSourcePool = new Queue<AudioSource>();
            _activeSpatialAudioSources = new List<AudioSource>();

            _spatialTemplateSource = _spatialTemplate.GetComponent<AudioSource>();

            GameObject spatialContainer = new GameObject();
            spatialContainer.name = "Spatial Audio Sources";
            spatialContainer.transform.SetParent(transform);

            // Clone the template audio source and fill the pool 
            for (int i = 0; i < _spatialPoolSize; i++)
            {
                GameObject newSpatialSource = Instantiate(_spatialTemplate);
                newSpatialSource.transform.SetParent(spatialContainer.transform);
                AudioSource sourceComponent = newSpatialSource.GetComponent<AudioSource>();

                _sourceGOs.Add(sourceComponent, newSpatialSource);
                _sourceTransforms.Add(sourceComponent, newSpatialSource.transform);

                _allSpatialAudioSources.Add(sourceComponent);
                _spatialAudioSourcePool.Enqueue(sourceComponent);

                // Disable audio source object by default
                newSpatialSource.SetActive(false);
            }

            // Disable the template once it's no longer needed
            _spatialTemplate.SetActive(false);

            // Proxy audio source setup

            _allProxyAudioSources = new List<AudioSource>();
            _proxyAudioSourcePool = new Queue<AudioSource>();
            _activeProxyAudioSources = new List<AudioSource>();
            _audioProxyLookup = new Dictionary<AudioSource, ProxyAudioSource>();
            _proxyAudioLookup = new Dictionary<ProxyAudioSource, AudioSource>();

            _proxyTemplateSource = _proxyTemplate.GetComponent<AudioSource>();

            GameObject proxyContainer = new GameObject();
            proxyContainer.name = "Proxy Audio Sources";
            proxyContainer.transform.SetParent(transform);

            for (int i = 0; i < _proxyPoolSize; i++)
            {
                GameObject newProxySource = Instantiate(_proxyTemplate);
                newProxySource.transform.SetParent(proxyContainer.transform);
                AudioSource sourceComponent = newProxySource.GetComponent<AudioSource>();

                _sourceGOs.Add(sourceComponent, newProxySource);
                _sourceTransforms.Add(sourceComponent, newProxySource.transform);

                _allProxyAudioSources.Add(sourceComponent);
                _proxyAudioSourcePool.Enqueue(sourceComponent);

                // Disable audio source object by default
                newProxySource.SetActive(false);
            }

            // Disable the template once it's no longer needed
            _proxyTemplate.SetActive(false);

            // UI audio setup

            _allUIAudioSources = new List<AudioSource>();
            _uiAudioSourcePool = new Queue<AudioSource>();
            _activeUIAudioSources = new List<AudioSource>();

            _uiTemplateSource = _uiTemplate.GetComponent<AudioSource>();

            GameObject uiContainer = new GameObject();
            uiContainer.name = "UI Audio Sources";
            uiContainer.transform.SetParent(transform);

            for (int i = 0; i < _uiPoolSize; i++)
            {
                GameObject newUISource = Instantiate(_uiTemplate);
                newUISource.transform.SetParent(uiContainer.transform);
                AudioSource sourceComponent = newUISource.GetComponent<AudioSource>();

                _sourceGOs.Add(sourceComponent, newUISource);
                _sourceTransforms.Add(sourceComponent, newUISource.transform);

                _allUIAudioSources.Add(sourceComponent);
                _uiAudioSourcePool.Enqueue(sourceComponent);

                // Disable audio source object by default
                newUISource.SetActive(false);
            }

            // Disable the template once it's no longer needed
            _uiTemplate.SetActive(false);

        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    StopAllActiveAudio();
                    break;
            }
        }

        private void StopAllActiveAudio()
        {
            // Stop and reclaim all active spatial audio sources
            foreach (AudioSource audioSource in _activeSpatialAudioSources.ToList())
            {
                StopActiveAudioSource(audioSource, AudioType.Spatial);
            }

            // Stop and reclaim all active UI audio sources
            foreach (AudioSource audioSource in _activeUIAudioSources.ToList())
            {
                StopActiveAudioSource(audioSource, AudioType.UI);
            }

            // Just stop all active proxy audio sources
            // Don't reclaim, as owners are still using them
            foreach (AudioSource audioSource in _activeProxyAudioSources)
            {
                audioSource.Stop();
            }
        }

        #endregion

        #region Pausing

        public void Pause()
        {
            PauseGameplayAudio(true);
        }

        public void Resume()
        {
            PauseGameplayAudio(false);
        }

        private void PauseGameplayAudio(bool toPause)
        {
            // Only proceed if the request is different to the current state
            if (_isPaused != toPause)
            {
                _isPaused = toPause;
                if (toPause)
                {
                    // Clear the paused audio source list
                    _pausedAudioSources.Clear();

                    // Pause all active and playing spatial audio sources
                    foreach (AudioSource audioSource in _activeSpatialAudioSources)
                    {
                        if (audioSource.isPlaying)
                        {
                            FadeOut(audioSource, true);
                            audioSource.enabled = false;
                            _pausedAudioSources.Add(audioSource);
                        }
                    }

                    // Pause all active proxy audio sources
                    foreach (AudioSource audioSource in _activeProxyAudioSources)
                    {
                        // Lock proxy audio source from playing
                        _audioProxyLookup[audioSource].LockProperty(ProxyAudioProperty.Play);

                        if (audioSource.isPlaying)
                        {
                            //FadeOut(audioSource, true);
                            audioSource.enabled = false;
                            _pausedAudioSources.Add(audioSource);
                        }
                    }

                    // Reduce music volume
                    _musicMultiplier = Globals.PAUSED_MUSIC_VOLUME;
                    _musicAudioSource.volume = CalculateVolumeForAudioClip(_musicAudioSource.clip, AudioType.Music);

                }
                else
                {
                    // Resume all active audio sources
                    foreach (AudioSource audioSource in _pausedAudioSources)
                    {
                        // Unlock play capability on any paused proxy audio sources
                        if(_audioProxyLookup.ContainsKey(audioSource))
                        {
                            _audioProxyLookup[audioSource].UnlockProperty(ProxyAudioProperty.Play);
                        }

                        audioSource.enabled = true;
                        FadeIn(audioSource);
                    }
                    _pausedAudioSources.Clear();

                    // Revert music volume
                    _musicMultiplier = 1;
                    _musicAudioSource.volume = CalculateVolumeForAudioClip(_musicAudioSource.clip, AudioType.Music);
                }
            }
        }


        #endregion

        #region Proxy audio sources

        /// <summary>
        /// Receiving method for issuing a proxy audio source to the caller. Sets up the proxy audio source based on the incoming request data and returns it via callback
        /// </summary>
        /// <param name="evt">The calling GameEvent</param>
        /// <param name="rawRequest">The AudioProxyRequest detailing the desired audio settings</param>
        /// <param name="callback">Callback function to return the proxy audio source, or null if failed</param>
        public IProxyAudioSource GetProxyAudioSource(AudioProxyRequest request)
        {
            if (_proxyAudioSourcePool.Count > 0 && request.parent != null)
            {
                // Gets a new audio source from pool
                AudioSource audioSource = _proxyAudioSourcePool.Dequeue();
                _sourceGOs[audioSource].SetActive(true);
                _activeProxyAudioSources.Add(audioSource);

                // Set spatial blend to 2D if requested
                if (request.audioType == AudioType.Flat || request.audioType == AudioType.UI)
                {
                    audioSource.spatialBlend = 0;
                }

                // Create new proxy audio source and return it
                ProxyAudioSource proxy = new ProxyAudioSource(this, audioSource, request.audioType, request.parent, _playerEffectsVolume, Globals.EFFECTS_MULTIPLIER);
                _proxyAudioLookup.Add(proxy, audioSource);
                _audioProxyLookup.Add(audioSource, proxy);

                return (IProxyAudioSource)proxy;
            }
            else
            {
                // Return null to indicate failed request
                return null;
            }
        }


        public bool IsProxyFading(ProxyAudioSource proxy)
        {
            return _fadeRoutines.ContainsKey(_proxyAudioLookup[proxy]);
        }

        public void FadeOut(ProxyAudioSource proxy, bool toPause)
        {
            AudioSource audioSource = _proxyAudioLookup[proxy];

            // If audio is paused and a proxy fade out request comes in, remove it from paused audio sources to prevent it being resumed afterwards
            if (_isPaused)
            {
                if (_pausedAudioSources.Contains(audioSource))
                {
                    _pausedAudioSources.Remove(audioSource);
                }
            }
            FadeOut(audioSource, toPause);
        }

        public void FadeIn(ProxyAudioSource proxy)
        {
            FadeIn(_proxyAudioLookup[proxy]);
        }

        /// <summary>
        /// Releases an audio proxy, reclaiming its audio source and removing unneeded references
        /// </summary>
        /// <param name="proxy">The proxy audio source to release</param>
        public void ReleaseProxy(ProxyAudioSource proxy)
        {
            AudioSource audioSource = _proxyAudioLookup[proxy];

            if(audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            _activeProxyAudioSources.Remove(audioSource);
            _proxyAudioSourcePool.Enqueue(audioSource);
            _proxyAudioLookup.Remove(proxy);
            _audioProxyLookup.Remove(audioSource);

            _sourceGOs[audioSource].SetActive(false);

            CopyAudioSettings(_proxyTemplateSource, audioSource);
        }

        #endregion


        #region Fading

        private void FadeOut(AudioSource audioSource, bool toPause)
        {
            if (_fadeRoutines.ContainsKey(audioSource))
            {
                StopCoroutine(_fadeRoutines[audioSource]);
                // Reset volume to where it would have been had the coroutine completed
                audioSource.volume = _fadeTargetVolume[audioSource];
                _fadeTargetVolume.Remove(audioSource);
            }
            _fadeRoutines[audioSource] = StartCoroutine(fadeOut(audioSource, toPause));
        }

        private void FadeIn(AudioSource audioSource)
        {
            if (_fadeRoutines.ContainsKey(audioSource))
            {
                StopCoroutine(_fadeRoutines[audioSource]);
                // Reset volume to where it would have been had the coroutine completed
                audioSource.volume = _fadeTargetVolume[audioSource];
                _fadeTargetVolume.Remove(audioSource);
            }
            _fadeRoutines[audioSource] = StartCoroutine(fadeIn(audioSource));
        }

        #endregion

        #region Utility

        public float GetAudioClipVolume(AudioClip clip)
        {
            if(_clipLookup.ContainsKey(clip))
            {
                return _clipLookup[clip].volume;
            }
            else
            {
                return 1;
            }
        }

        private void CheckForIdleAudioSources()
        {
            List<AudioSource> toDelete = new List<AudioSource>();

            // Only check for idle spatial audio sources when not paused
            // Since the sources will be inactive while paused, but we don't want to purge them
            if (!_isPaused)
            {
                foreach (AudioSource audioSource in _activeSpatialAudioSources)
                {
                    if (!audioSource.isPlaying)
                    {
                        toDelete.Add(audioSource);
                    }
                }

                foreach (AudioSource audioSource in toDelete)
                {
                    StopActiveAudioSource(audioSource, AudioType.Spatial);
                }

                toDelete.Clear();
            }

            foreach (AudioSource audioSource in _activeUIAudioSources)
            {
                if (!audioSource.isPlaying)
                {
                    toDelete.Add(audioSource);
                }
            }

            foreach (AudioSource audioSource in toDelete)
            {
                StopActiveAudioSource(audioSource, AudioType.UI);
            }

        }

        private void StopActiveAudioSource(AudioSource audioSource, AudioType audioType)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            switch(audioType)
            {
                case AudioType.Flat:
                case AudioType.Spatial:
                    _activeSpatialAudioSources.Remove(audioSource);
                    _spatialAudioSourcePool.Enqueue(audioSource);
                    break;
                case AudioType.UI:
                    _activeUIAudioSources.Remove(audioSource);
                    _uiAudioSourcePool.Enqueue(audioSource);
                    break;
            }
            
            _sourceGOs[audioSource].SetActive(false);

            // Kill the parenting coroutine if this audio source had one
            if (_sourceCoroutines.ContainsKey(audioSource))
            {
                StopCoroutine(_sourceCoroutines[audioSource]);
                _sourceCoroutines.Remove(audioSource);
            }

            // Revert to default settings
            switch(audioType)
            {
                case AudioType.Flat:
                case AudioType.Spatial:
                    CopyAudioSettings(_spatialTemplateSource, audioSource);
                    break;
                case AudioType.UI:
                    CopyAudioSettings(_uiTemplateSource, audioSource);
                    break;
            }
        }

        private void CopyAudioSettings(AudioSource source, AudioSource destination)
        {
            destination.loop = source.loop;
            destination.volume = source.volume;
            destination.pitch = source.pitch;
            destination.panStereo = source.panStereo;
            destination.spatialBlend = source.spatialBlend;
            destination.reverbZoneMix = source.reverbZoneMix;
            destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
            destination.dopplerLevel = source.dopplerLevel;
            destination.spread = source.spread;
            destination.rolloffMode = source.rolloffMode;
            destination.minDistance = source.minDistance;
            destination.maxDistance = source.maxDistance;
            destination.priority = source.priority;
            destination.outputAudioMixerGroup = source.outputAudioMixerGroup;
        }

        private void UpdateVolumeSettings()
        {
            // Update volume of all audio sources

            foreach (AudioSource audioSource in _allSpatialAudioSources.ToList())
            {
                audioSource.volume = CalculateVolumeForAudioClip(audioSource.clip, AudioType.Spatial);
            }

            foreach (AudioSource audioSource in _allUIAudioSources.ToList())
            {
                audioSource.volume = CalculateVolumeForAudioClip(audioSource.clip, AudioType.UI);
            }

            foreach (ProxyAudioSource proxy in _proxyAudioLookup.Keys.ToList())
            {
                proxy.UpdateAudioSettings();
            }

            foreach (AudioSource audioSource in _cutsceneAudioSources.ToList())
            {
                audioSource.volume = CalculateVolumeForAudioClip(audioSource.clip, AudioType.Spatial);
            }

            _musicAudioSource.volume = CalculateVolumeForAudioClip(_musicAudioSource.clip, AudioType.Music);
        }

        private float CalculateVolumeForAudioClip(AudioClip audioClip, AudioType audioType)
        {
            float playerTypeVolume = 1;
            float globalTypeVolume = 1;

            switch(audioType)
            {
                case AudioType.Flat:
                case AudioType.Spatial:
                    playerTypeVolume = _playerEffectsVolume;
                    globalTypeVolume = Globals.EFFECTS_MULTIPLIER;
                    break;
                case AudioType.UI:
                    playerTypeVolume = _playerUIVolume;
                    globalTypeVolume = Globals.UI_MULTIPLIER;
                    break;
                case AudioType.Music:
                    playerTypeVolume = _playerMusicVolume;
                    globalTypeVolume = Globals.MUSIC_MULTIPLIER * _musicMultiplier;
                    break;
            }

            return _playerMasterVolume * playerTypeVolume * globalTypeVolume * GetAudioClipVolume(audioClip);
        }

        #endregion

        #region Audio requests

        private string PlaySpatialAudioClip(AudioDatabaseEntry entry, AudioPlayRequest request)
        {
            if (_spatialAudioSourcePool.Count > 0)
            {
                // Generate a unique reference ID
                string uniqueID = System.Guid.NewGuid().ToString();

                // Get audio source from pool
                AudioSource audioSource = _spatialAudioSourcePool.Dequeue();

                // Set audio clip and volume from request
                audioSource.clip = entry.clip;
                audioSource.volume = request.volume * _playerMasterVolume * Globals.EFFECTS_MULTIPLIER * _playerEffectsVolume * entry.volume;

                if (request.maxDistance > 0)
                {
                    audioSource.maxDistance = request.maxDistance;
                }

                // If this audio source should match position of another gameobject, start a routine to handle that
                if (request.parent != null)
                {
                    _sourceCoroutines.Add(audioSource, StartCoroutine(parentAudioSource(audioSource, request.parent)));
                }
                // Otherwise position the audio source at the provided location
                else
                {
                    _sourceTransforms[audioSource].position = request.location;
                }


                _sourceGOs[audioSource].SetActive(true);

                audioSource.Play();

                _activeSpatialAudioSources.Add(audioSource);
                
                return uniqueID;
            }
            else
            {
                return string.Empty;
            }
        }

        private string PlayFlatAudioClip(AudioDatabaseEntry entry, AudioPlayRequest request)
        {
            if (_spatialAudioSourcePool.Count > 0)
            {
                // Generate a unique reference ID
                string uniqueID = System.Guid.NewGuid().ToString();

                // Get audio source from pool
                AudioSource audioSource = _spatialAudioSourcePool.Dequeue();

                // Set audio clip and volume from request data
                audioSource.clip = entry.clip;
                audioSource.volume = request.volume * _playerMasterVolume * Globals.EFFECTS_MULTIPLIER * _playerEffectsVolume * entry.volume;

                // Set audio source to 2D
                audioSource.spatialBlend = 0;

                _sourceGOs[audioSource].SetActive(true);

                audioSource.Play();

                _activeSpatialAudioSources.Add(audioSource);
                return uniqueID;
            }
            else
            {
                return string.Empty;
            }
        }


        private string PlayUIAudioClip(AudioDatabaseEntry entry, AudioPlayRequest request)
        {
            if (_uiAudioSourcePool.Count > 0)
            {
                // Generate a unique reference ID
                string uniqueID = System.Guid.NewGuid().ToString();

                // Get audio source from pool
                AudioSource audioSource = _uiAudioSourcePool.Dequeue();

                // Set audio clip and volume from request data
                audioSource.clip = entry.clip;
                audioSource.volume = request.volume * _playerMasterVolume * Globals.UI_MULTIPLIER * _playerUIVolume * entry.volume;

                _sourceGOs[audioSource].SetActive(true);

                audioSource.Play();

                _activeUIAudioSources.Add(audioSource);
                return uniqueID;
            }
            else
            {
                return string.Empty;
            }
        }


        /// <summary>
        /// Receiving method for audio play requests
        /// </summary>
        /// <param name="request">The AudioPlayRequest detailing the clip to play and other audio settings</param>
        /// <returns>A reference ID for this audio request, for use in subsequent calls relating to this request</returns>

        public string PlayAudioClip(AudioPlayRequest request)
        {
            if(!_audioDictionary.ContainsKey(request.audioClipID))
            {
                return string.Empty;
            }

            
            AudioDatabaseEntry entry = _audioDictionary[request.audioClipID];

            // Checks to see when clip was last played
            // If was too recent, don't play again
            AudioClip requestClip = entry.clip;
            if (!_clipLastPlayed.ContainsKey(requestClip))
            {
                _clipLastPlayed.Add(requestClip, Time.time);
            }
            else
            {
                float nowTime = Time.time;
                float playedDelta = nowTime - _clipLastPlayed[requestClip];
                _clipLastPlayed[requestClip] = nowTime;
                if (playedDelta <= _playClipBuffer)
                {
                    return string.Empty;
                }
            }

            switch (request.audioType)
            {
                case AudioType.Spatial:
                    // Can only play spatial audio if game isn't paused
                    return _isPaused ? string.Empty: PlaySpatialAudioClip(entry, request);
                case AudioType.Flat:
                    // Can only play flat audio if game isn't paused
                    return _isPaused ? string.Empty: PlayFlatAudioClip(entry, request);
                case AudioType.UI:
                    return PlayUIAudioClip(entry, request);
            }

            return string.Empty;
        }

        #endregion


        #region Coroutines

        private IEnumerator fadeIn(AudioSource audioSource)
        {
            float targetVolume = audioSource.volume;

            // Cache target volume in case this coroutine needs to be halted
            _fadeTargetVolume.Add(audioSource, targetVolume);

            audioSource.volume = 0;
            audioSource.Play();
            float fadeLerp = 0;
            while (fadeLerp < _fadeTime)
            {
                audioSource.volume = Mathf.Lerp(0, targetVolume, fadeLerp / _fadeTime);
                fadeLerp += Time.unscaledDeltaTime;
                yield return null;
            }
            audioSource.volume = targetVolume;
            _fadeRoutines.Remove(audioSource);
            _fadeTargetVolume.Remove(audioSource);
        }

        private IEnumerator fadeOut(AudioSource audioSource, bool toPause)
        {
            float cachedVolume = audioSource.volume;

            // Cache target volume in case this coroutine needs to be halted
            _fadeTargetVolume.Add(audioSource, cachedVolume);

            float fadeLerp = 0;
            while (fadeLerp < _fadeTime)
            {
                audioSource.volume = Mathf.Lerp(cachedVolume, 0, fadeLerp / _fadeTime);
                fadeLerp += Time.unscaledDeltaTime;
                yield return null;
            }

            if (toPause)
            {
                audioSource.Pause();
            }
            else
            {
                audioSource.Stop();
            }
            audioSource.volume = cachedVolume;
            _fadeTargetVolume.Remove(audioSource);
            _fadeRoutines.Remove(audioSource);
        }

        /// <summary>
        /// Routine to mimic parenting an audio source to a transform. This approach is to avoid issues with the parent object being destroyed and not releasing the audio source
        /// </summary>
        /// <param name="source">The audio source to parent</param>
        /// <param name="parent">The transform to match the position of</param>
        /// <returns></returns>
        private IEnumerator parentAudioSource(AudioSource source, Transform parent)
        {
            Transform target = _sourceTransforms[source];
            while (true)
            {
                yield return new WaitForSeconds(_parentUpdateRate);
                target.position = parent.position;
            }
        }

        /// <summary>
        /// Crossfades from the existing music clip to the provided clip
        /// </summary>
        /// <param name="newClip">The new music clip to fade into</param>
        /// <returns></returns>
        private IEnumerator crossFadeMusic(AudioClip newClip)
        {
            float fadeLerp = 0;
            float fadeTime = _crossfadeTime / 2;

            float currentVolume = _musicAudioSource.volume;

            // Fade out
            while (fadeLerp < fadeTime)
            {
                _musicAudioSource.volume = Mathf.Lerp(currentVolume, 0, fadeLerp / fadeTime);
                fadeLerp += Time.unscaledDeltaTime;
                yield return null;
            }

            // Swap audio clip
            _musicAudioSource.Stop();
            _musicAudioSource.clip = newClip;
            _musicAudioSource.Play();

            // Fade in
            fadeLerp = 0;
            float targetVolume = _musicMultiplier * Globals.MUSIC_MULTIPLIER * _playerMusicVolume * _clipLookup[newClip].volume;
            while (fadeLerp < fadeTime)
            {
                _musicAudioSource.volume = Mathf.Lerp(0, targetVolume, fadeLerp / fadeTime);
                fadeLerp += Time.unscaledDeltaTime;
                yield return null;
            }
            _musicAudioSource.volume = targetVolume;
        }

        #endregion

        #region Music

        public void PlayMusic(string audioClipID)
        {
            if(!_audioDictionary.ContainsKey(audioClipID))
            {
                return;
            }
            
            AudioDatabaseEntry entry = _audioDictionary[audioClipID];

            if (_musicAudioSource.isPlaying)
            {
                if (_musicFadeRoutine != null)
                {
                    StopCoroutine(_musicFadeRoutine);
                }
                _musicFadeRoutine = StartCoroutine(crossFadeMusic(entry.clip));
            }
            else
            {
                _musicAudioSource.clip = entry.clip;
                _musicAudioSource.Play();
            }

            // Update music audio source volume to account for new clip volume
            _musicAudioSource.volume = CalculateVolumeForAudioClip(entry.clip, AudioType.Music);
        }

        #endregion

        #region PlayerPrefs

        private void OnVolumeChanged(VolumeType volumeType, float value)
        {
            switch(volumeType)
            {
                case VolumeType.Music:
                    _playerMusicVolume = value;
                    break;
                case VolumeType.Effects:
                    _playerEffectsVolume = value; 
                    break;
                case VolumeType.UI:
                    _playerUIVolume = value;
                    break;
                case VolumeType.Master:
                    _playerMasterVolume = value;
                    break;
            }

            UpdateVolumeSettings();
        }

        private void GetCurrentVolumeFromPlayerPrefs()
        {
            _playerMusicVolume = (float)_playerPrefsCache.GetValue(Globals.MUSIC_VOLUME_KEY);
            _playerMasterVolume = (float)_playerPrefsCache.GetValue(Globals.MASTER_VOLUME_KEY);
            _playerEffectsVolume = (float)_playerPrefsCache.GetValue(Globals.EFFECTS_VOLUME_KEY);
            _playerUIVolume = (float)_playerPrefsCache.GetValue(Globals.UI_VOLUME_KEY);
        }

        #endregion

        #region Time scaling

        private void ChangeAudioTimeScale(float timeScale)
        {
            // Timescale should only be set to 0 when the game is paused, or the momentary TimeStopper is active
            // In the former case, pausing audio sources is being handled with the IPausable interface
            // In the latter case, we don't want to stop or slow audio, so we should ignore it
            if(timeScale == 0)
            {
                return;
            }

            // Mapping audio pitch 1:1 with timescale doesn't achieve desirable results,
            // so any timescale other than 1 or 0 is mapped to a predetermined audio pitch
            if(timeScale != 1)
            {
                timeScale = Globals.SLOWMO_AUDIO_SCALE;
            }

            foreach(AudioSource audioSource in _activeProxyAudioSources)
            {
                ProxyAudioSource proxy = _audioProxyLookup[audioSource];
                // Unlock pitch control once time is set back to 1
                if(timeScale == 1)
                {
                    proxy.UnlockProperty(ProxyAudioProperty.Pitch);
                }
                else
                {
                    proxy.LockProperty(ProxyAudioProperty.Pitch);
                }

                audioSource.pitch = timeScale;
            }

            foreach(AudioSource audioSource in _activeSpatialAudioSources)
            {
                audioSource.pitch = timeScale;
            }

            foreach (AudioSource audioSource in _activeUIAudioSources)
            {
                audioSource.pitch = timeScale;
            }

            // Reduce music volume when time scale is less than 1
            //_musicMultiplier = timeScale == 1 ? 1 : Globals.SLOWMO_MUSIC_VOLUME;
            //UpdateVolumeSettings();

            _musicAudioSource.pitch = timeScale;

        }

        #endregion

        #region Dependency Requests

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IAudioManager)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getAudioManager.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getAudioManager.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        private void ReceivePlayerPrefsCache(object rawObj)
        {
            _playerPrefsCache = rawObj as PlayerPrefsCache;
            SubscribeToPlayerPrefsCache();
            GetCurrentVolumeFromPlayerPrefs();
        }

        private void SubscribeToPlayerPrefsCache()
        {
            if (!_isSubscribedToPlayerPrefsCache && _playerPrefsCache != null)
            {
                _playerPrefsCache.SubscribeToValueChanged(this, Globals.MUSIC_VOLUME_KEY, (rawObj) => { OnVolumeChanged(VolumeType.Music, (float)rawObj); });
                _playerPrefsCache.SubscribeToValueChanged(this, Globals.MASTER_VOLUME_KEY, (rawObj) => { OnVolumeChanged(VolumeType.Master, (float)rawObj); });
                _playerPrefsCache.SubscribeToValueChanged(this, Globals.EFFECTS_VOLUME_KEY, (rawObj) => { OnVolumeChanged(VolumeType.Effects, (float)rawObj); });
                _playerPrefsCache.SubscribeToValueChanged(this, Globals.UI_VOLUME_KEY, (rawObj) => { OnVolumeChanged(VolumeType.UI, (float)rawObj); });

                _isSubscribedToPlayerPrefsCache = true;
            }
        }

        private void UnsubscribeFromPlayerPrefsCache()
        {
            if(_isSubscribedToPlayerPrefsCache && _playerPrefsCache != null)
            {
                _playerPrefsCache.UnsubscribeFromValueChanged(this, Globals.MUSIC_VOLUME_KEY);
                _playerPrefsCache.UnsubscribeFromValueChanged(this, Globals.EFFECTS_VOLUME_KEY);
                _playerPrefsCache.UnsubscribeFromValueChanged(this, Globals.MASTER_VOLUME_KEY);
                _playerPrefsCache.UnsubscribeFromValueChanged(this, Globals.UI_VOLUME_KEY);
                _isSubscribedToPlayerPrefsCache = false;
            }
        }

        #endregion


        #region Dependencies

        private void ReceiveTimeScaler(object rawObj)
        {
            _timeScaler = rawObj as ITimeScaler;
            _timeScaler.OnTimeScaleChanged += ChangeAudioTimeScale;
        }

        #endregion

        private enum VolumeType
        {
            Master,
            Music,
            Effects,
            UI,
        }
    }

    public interface IAudioManager
    {
        public void PlayMusic(string audioClipID);
        public IProxyAudioSource GetProxyAudioSource(AudioProxyRequest request);
        public string PlayAudioClip(AudioPlayRequest request);
    }

    [Serializable]
    public class AudioPlayRequest
    {
        public AudioType audioType = AudioType.Spatial;
        public string audioClipID;
        public float volume = 1;
        public Vector3 location;
        public float maxDistance = 0;
        public Transform parent = null;
    }

    [Serializable]
    public class AudioProxyRequest
    {
        public AudioType audioType = AudioType.Spatial;
        public Transform parent;
    }

    public enum AudioType
    {
        Spatial,
        Flat,
        UI,
        Music
    }
}