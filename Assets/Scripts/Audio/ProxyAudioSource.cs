using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace AmalgamGames.Audio
{
    public class ProxyAudioSource : IProxyAudioSource
    {
        // Volume settings
        private float _playerMultiplier = 1;
        private float _globalMultiplier = 1;
        private float _internalVolume = 1;

        // Components
        private AudioManager _audioManager;
        private AudioSource _audioSource;
        private Transform _targetParent;
        private Transform _sourceTransform;

        private AudioType _audioType;

        // State
        private bool _isActive = false;

        private Dictionary<ProxyAudioProperty, bool> _propertyLocks;

        #region Initialisation

        public ProxyAudioSource(AudioManager controller, AudioSource audioSource, AudioType audioType, Transform parent, float playerMultiplier, float globalMultiplier)
        {
            if (controller == null || audioSource == null || parent == null)
            {
                throw new ArgumentNullException("Cannot pass null parameters to constructor");
            }
            else
            {
                _audioManager = controller;
                _audioSource = audioSource;
                _targetParent = parent;
                _audioType = audioType;
                _playerMultiplier = playerMultiplier;
                _globalMultiplier = globalMultiplier;
                _sourceTransform = audioSource.transform;
                UpdateVolume();

                // Initialise all property locks, with no lock as default
                _propertyLocks = new Dictionary<ProxyAudioProperty, bool>();
                foreach(int i in Enum.GetValues(typeof(ProxyAudioProperty)))
                {
                    _propertyLocks[(ProxyAudioProperty)i] = false;
                }
            }
        }

        #endregion

        #region Update

        public void Update()
        {
            // If proxy is a spatial audio source and has a valid parent, update its position to match parent
            if (_audioType == AudioType.Spatial && _isActive && _targetParent != null)
            {
                _sourceTransform.position = _targetParent.position;
            }
            else if (_targetParent == null)
            {
                // Target parent has been destroyed, so release this proxy audio source
                ReleaseProxy();
            }
        }

        #endregion

        #region Utility

        private void ReleaseProxy()
        {
            _audioManager.ReleaseProxy(this);
            // Null all references to prevent further use
            _audioSource = null;
            _targetParent = null;
            _sourceTransform = null;
            _audioManager = null;
        }

        /// <summary>
        /// Update volume of this proxy's audio source based on latest multipliers
        /// </summary>
        private void UpdateVolume()
        {
            _audioSource.volume = _playerMultiplier * _globalMultiplier * _internalVolume * _audioManager.GetAudioClipVolume(_audioSource.clip);
        }

        #endregion

        #region Updating settings

        public void UpdateAudioSettings()
        {
            _playerMultiplier = _audioManager.PlayerEffectsVolume;
            _globalMultiplier = _audioManager.GlobalEffectsVolume;

            UpdateVolume();
        }

        #endregion

        #region Activation/deactivation

        public void Activate()
        {
            _isActive = true;
            // Only plays the audio if the audio source is enabled
            if (_audioSource.enabled)
            {
                if (!_audioSource.isPlaying || _audioManager.IsProxyFading(this))
                {
                    _audioManager.FadeIn(this);
                }
            }
        }

        public void Deactivate()
        {
            _isActive = false;
            _audioManager.FadeOut(this, false);
        }

        #endregion

        #region Audio

        public void Play()
        {
            if (_isActive && _audioSource.enabled && !_propertyLocks[ProxyAudioProperty.Play])
            {
                _audioManager.FadeIn(this);
            }
        }

        public void SetAudioClip(AudioClip clip)
        {
            if (!_propertyLocks[ProxyAudioProperty.Clip])
            {
                bool wasPlaying = _audioSource.isPlaying;
                _audioSource.clip = clip;
                UpdateVolume();
                if (wasPlaying && _audioSource.enabled)
                {
                    _audioSource.Play();
                }
            }
        }

        public void SetVolume(float volume)
        {
            if (!_propertyLocks[ProxyAudioProperty.Volume])
            {
                _internalVolume = Mathf.Clamp01(volume);
                UpdateVolume();
            }
        }

        public void Pause()
        {
            _audioManager.FadeOut(this, true);
        }

        public void Unpause()
        {
            if (_isActive && _audioSource.enabled)
            {
                _audioManager.FadeIn(this);
            }
        }

        public void ChangeAudibleRadius(float minDist, float maxDist)
        {
            if (!_propertyLocks[ProxyAudioProperty.AudibleRadius])
            {
                _audioSource.minDistance = minDist;
                _audioSource.maxDistance = maxDist;
            }
        }

        public void Stop()
        {
            _audioManager.FadeOut(this, false);
        }

        public void SetPitch(float pitch)
        {
            if (!_propertyLocks[ProxyAudioProperty.Pitch])
            {
                _audioSource.pitch = pitch;
            }
        }

        #endregion

        #region Proxy management

        public void DeleteProxy()
        {
            ReleaseProxy();
        }

        #endregion

        #region Property locking

        public void LockProperty(ProxyAudioProperty property)
        {
            _propertyLocks[property] = true;
        }

        public void UnlockProperty(ProxyAudioProperty property)
        {
            _propertyLocks[property] = false;
        }

        #endregion
    }

    public interface IProxyAudioSource
    {
        public void Pause();
        public void Unpause();
        public void ChangeAudibleRadius(float minDist, float maxDist);
        public void Stop();
        public void SetPitch(float pitch);
        public void SetVolume(float volume);
        public void DeleteProxy();
        public void SetAudioClip(AudioClip audioClip);
        public void Play();
        public void Activate();
        public void Deactivate();
    }

    public enum ProxyAudioProperty
    {
        Volume,
        Pitch,
        AudibleRadius,
        Clip,
        Play
    }

}