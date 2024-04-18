using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Audio
{
    public class BGMPlayer : MonoBehaviour
    {
        [Title("Music")]
        [SerializeField] private string _musicClipID;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getAudioManager;

        // Components
        private IAudioManager _audioManager;

        #region Lifecycle

        private void Start()
        {
            _getAudioManager.RequestDependency(ReceiveAudioManager);
        }

        #endregion

        #region Music

        private void PlayMusic()
        {
            _audioManager?.PlayMusic(_musicClipID);
        }

        #endregion

        #region Dependencies

        private void ReceiveAudioManager(object rawObj)
        {
            _audioManager = rawObj as IAudioManager;
            PlayMusic();
        }

        #endregion
    }
}