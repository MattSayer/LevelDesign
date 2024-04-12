using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Audio
{
    public class ProxyAudioSource
    {
        private float _playerMultiplier = 1;
        private float _globalMultiplier = 1;
        private float _internalVolume = 1;

        private AudioManager _audioManager;
        private AudioSource _audioSource;
        private Transform _targetParent;
        private Transform _sourceTransform;

        private AudioType _audioType;

        private bool _isActive = false;

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

        #region Public methods

        public void UpdateAudioSettings()
        {
            _playerMultiplier = _audioManager.PlayerEffectsVolume;
            _globalMultiplier = _audioManager.GlobalEffectsVolume;

            UpdateVolume();
        }

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

        public void Play()
        {
            if (_isActive && _audioSource.enabled)
            {
                _audioManager.FadeIn(this);
            }
        }

        public void SetAudioClip(AudioClip clip)
        {
            bool wasPlaying = _audioSource.isPlaying;
            _audioSource.clip = clip;
            UpdateVolume();
            if (wasPlaying && _audioSource.enabled)
            {
                _audioSource.Play();
            }
        }

        public void SetVolume(float volume)
        {
            _internalVolume = Mathf.Clamp01(volume);
            UpdateVolume();
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
            _audioSource.minDistance = minDist;
            _audioSource.maxDistance = maxDist;
        }

        public void Stop()
        {
            _audioManager.FadeOut(this, false);
        }

        public void SetPitch(float pitch)
        {
            _audioSource.pitch = pitch;
        }

        public void DeleteProxy()
        {
            ReleaseProxy();
        }

        #endregion
    }
}