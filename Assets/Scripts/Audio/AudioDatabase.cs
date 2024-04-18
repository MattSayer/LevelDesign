using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Audio
{
    [CreateAssetMenu(menuName = "Audio/AudioDatabase", fileName = "AudioDatabase")]
    public class AudioDatabase : ScriptableObject
    {
        [Searchable]
        [SerializeField] private List<AudioDatabaseEntry> _entries;
        
        public List<AudioDatabaseEntry> Entries { get { return _entries;  } }

    }

    [Serializable]
    public class AudioDatabaseEntry
    {
        public string audioClipID;
        public AudioClip clip;
        [Range(0, 1)]
        public float volume = 1;
    }
}
