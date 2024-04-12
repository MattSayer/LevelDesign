using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Audio
{
    [CreateAssetMenu(menuName = "Audio/AudioDatabase", fileName = "AudioDatabase")]
    public class AudioDatabase : ScriptableObject
    {
        [SerializeField] private List<AudioClipVolume> _audioClips;

        public float GetAudioClipVolume(AudioClip clip)
        {
            foreach (AudioClipVolume acv in _audioClips)
            {
                if (acv.clip == clip)
                {
                    return acv.volume;
                }
            }
            // Default volume to 1 if clip not found
            return 1;
        }

    }

    [Serializable]
    public class AudioClipVolume
    {
        public AudioClip clip;
        [Range(0, 1)]
        public float volume;
    }
}
