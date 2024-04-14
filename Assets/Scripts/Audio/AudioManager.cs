using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
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
        [Title("Audio Clips")]
        [SerializeField] private AudioClip _menuMusic;
        [Space]
        [Title("Audio Mixers")]
        [SerializeField] private AudioMixerGroup _masterMixerGroup;
        [SerializeField] private AudioMixerGroup _uiMixerGroup;
        [SerializeField] private AudioMixerGroup _effectsMixerGroup;
        [SerializeField] private AudioMixerGroup _musicMixerGroup;
        [Title("Audio database")]
        [SerializeField] private AudioDatabase _database;
        [Space]
        [Title("Failsafes")]
        [SerializeField] private float _playClipBuffer = 0.1f;

        public float PlayerEffectsVolume { get { return _playerMasterVolume * _playerEffectsVolume; } }
        public float GlobalEffectsVolume { get { return _effectsMultiplier; } }


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
        private Dictionary<ProxyAudioSource, AudioSource> _audioProxies;

        // Music
        private AudioClip _currentMusicClip;

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

        // Global multipliers
        private float _musicMultiplier = 0.5f;
        private float _effectsMultiplier = 0.5f;
        private float _uiMultiplier = 0.5f;

        // Pausing
        private bool _isPaused = false;
        private List<AudioSource> _pausedAudioSources;

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

            SubscribeToDependencyRequests();

            InitialiseAudioSources();
        }

        
        private void Start()
        {
            _getPlayerPrefsCache.RequestDependency(ReceivePlayerPrefsCache);
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
            foreach (ProxyAudioSource proxy in _audioProxies.Keys.ToList())
            {
                proxy.Update();
            }

            CheckForIdleAudioSources();
        }

        #endregion

        #region Initialisation

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
            _audioProxies = new Dictionary<ProxyAudioSource, AudioSource>();

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

            _musicAudioSource.Play();
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
                        if (audioSource.isPlaying)
                        {
                            //FadeOut(audioSource, true);
                            audioSource.enabled = false;
                            _pausedAudioSources.Add(audioSource);
                        }
                    }
                }
                else
                {
                    // Resume all active audio sources
                    foreach (AudioSource audioSource in _pausedAudioSources)
                    {
                        audioSource.enabled = true;
                        FadeIn(audioSource);
                    }
                    _pausedAudioSources.Clear();
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
        public ProxyAudioSource GetProxyAudioSource(AudioProxyRequest request)
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
                ProxyAudioSource proxy = new ProxyAudioSource(this, audioSource, request.audioType, request.parent, _playerEffectsVolume, _effectsMultiplier);
                _audioProxies.Add(proxy, audioSource);

                return proxy;
            }
            else
            {
                // Return null to indicate failed request
                return null;
            }
        }


        public bool IsProxyFading(ProxyAudioSource proxy)
        {
            return _fadeRoutines.ContainsKey(_audioProxies[proxy]);
        }

        public void FadeOut(ProxyAudioSource proxy, bool toPause)
        {
            AudioSource audioSource = _audioProxies[proxy];

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
            FadeIn(_audioProxies[proxy]);
        }

        /// <summary>
        /// Releases an audio proxy, reclaiming its audio source and removing unneeded references
        /// </summary>
        /// <param name="proxy">The proxy audio source to release</param>
        public void ReleaseProxy(ProxyAudioSource proxy)
        {
            AudioSource audioSource = _audioProxies[proxy];

            if(audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            _activeProxyAudioSources.Remove(audioSource);
            _proxyAudioSourcePool.Enqueue(audioSource);
            _audioProxies.Remove(proxy);

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
            return _database.GetAudioClipVolume(clip);
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
                audioSource.volume = _playerMasterVolume * _playerEffectsVolume * _effectsMultiplier * _database.GetAudioClipVolume(audioSource.clip);
            }

            foreach (AudioSource audioSource in _allUIAudioSources.ToList())
            {
                audioSource.volume = _playerMasterVolume * _playerUIVolume * _uiMultiplier * _database.GetAudioClipVolume(audioSource.clip);
            }

            foreach (ProxyAudioSource proxy in _audioProxies.Keys.ToList())
            {
                proxy.UpdateAudioSettings();
            }

            foreach (AudioSource audioSource in _cutsceneAudioSources.ToList())
            {
                audioSource.volume = _playerMasterVolume * _playerEffectsVolume * _effectsMultiplier;
            }

            _musicAudioSource.volume = _playerMasterVolume * _playerMusicVolume * _musicMultiplier * _database.GetAudioClipVolume(_musicAudioSource.clip);

        }

        #endregion

        #region Audio requests

        private bool PlaySpatialAudioClip(AudioPlayRequest request)
        {

            if (_spatialAudioSourcePool.Count > 0)
            {
                // Get audio source from pool
                AudioSource audioSource = _spatialAudioSourcePool.Dequeue();

                // Set audio clip and volume from request
                audioSource.clip = request.clip;
                audioSource.volume = request.volume * _playerMasterVolume * _effectsMultiplier * _playerEffectsVolume * _database.GetAudioClipVolume(request.clip);

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
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool PlayFlatAudioClip(AudioPlayRequest request)
        {
            if (_spatialAudioSourcePool.Count > 0)
            {
                // Get audio source from pool
                AudioSource audioSource = _spatialAudioSourcePool.Dequeue();

                // Set audio clip and volume from request data
                audioSource.clip = request.clip;
                audioSource.volume = request.volume * _playerMasterVolume * _effectsMultiplier * _playerEffectsVolume * _database.GetAudioClipVolume(request.clip);

                // Set audio source to 2D
                audioSource.spatialBlend = 0;

                _sourceGOs[audioSource].SetActive(true);

                audioSource.Play();

                _activeSpatialAudioSources.Add(audioSource);
                return true;
            }
            else
            {
                return false;
            }
        }


        private bool PlayUIAudioClip(AudioPlayRequest request)
        {
            if (_uiAudioSourcePool.Count > 0)
            {
                // Get audio source from pool
                AudioSource audioSource = _uiAudioSourcePool.Dequeue();

                // Set audio clip and volume from request data
                audioSource.clip = request.clip;
                audioSource.volume = request.volume * _playerMasterVolume * _uiMultiplier * _playerUIVolume * _database.GetAudioClipVolume(request.clip);

                _sourceGOs[audioSource].SetActive(true);

                audioSource.Play();

                _activeUIAudioSources.Add(audioSource);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Receiving method for audio play requests
        /// </summary>
        /// <param name="evt">The calling GameEvent</param>
        /// <param name="rawRequest">The AudioPlayRequest detailing the clip to play and other audio settings</param>
        /// <param name="callback">Callback function to return true/false depending on whether the play request succeeded/failed</param>
        public bool PlayAudioClip(AudioPlayRequest request)
        {
            // Checks to see when clip was last played
            // If was too recent, don't play again
            AudioClip requestClip = request.clip;
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
                    return false;
                }
            }

            switch (request.audioType)
            {
                case AudioType.Spatial:
                    // Can only play spatial audio if game isn't paused
                    return _isPaused ? false : PlaySpatialAudioClip(request);
                case AudioType.Flat:
                    // Can only play flat audio if game isn't paused
                    return _isPaused ? false : PlayFlatAudioClip(request);
                case AudioType.UI:
                    return PlayUIAudioClip(request);
            }

            return false;
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
            float targetVolume = _musicMultiplier * _playerMusicVolume * _database.GetAudioClipVolume(newClip);
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

        public void ChangeMusic(AudioClip clip)
        {
            if (_musicAudioSource.isPlaying)
            {
                if (_musicFadeRoutine != null)
                {
                    StopCoroutine(_musicFadeRoutine);
                }
                _musicFadeRoutine = StartCoroutine(crossFadeMusic(clip));
            }
            else
            {
                _musicAudioSource.clip = clip;
                _musicAudioSource.Play();
            }
        }

        public void ToggleMenuMusic(bool toActivate)
        {
            if (toActivate)
            {
                // Cache current music clip
                _currentMusicClip = _musicAudioSource.clip;

                // Play menu music
                if (_musicFadeRoutine != null)
                {
                    StopCoroutine(_musicFadeRoutine);
                }
                _musicFadeRoutine = StartCoroutine(crossFadeMusic(_menuMusic));
            }
            else
            {
                // Restore gameplay music
                if (_musicFadeRoutine != null)
                {
                    StopCoroutine(_musicFadeRoutine);
                }
                _musicFadeRoutine = StartCoroutine(crossFadeMusic(_currentMusicClip));
            }
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
        public void ToggleMenuMusic(bool toActivate);
        public void ChangeMusic(AudioClip clip);
        public ProxyAudioSource GetProxyAudioSource(AudioProxyRequest request);
        public bool PlayAudioClip(AudioPlayRequest request);
    }

    [Serializable]
    public class AudioPlayRequest
    {
        public AudioType audioType = AudioType.Spatial;
        public AudioClip clip;
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
        UI
    }
}