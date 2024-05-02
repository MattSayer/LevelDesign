using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Config
{
    [CreateAssetMenu(menuName = "Config/StringDatabase", fileName ="StringDatabase")]
    public class StringDatabase : ScriptableObject
    {
        [Title("Strings")]
        [Searchable]
        [MultiLineProperty(4)]
        [SerializeField] private string[] _strings;
        
        public string[] GetAllStrings()
        {
            return _strings;
        }
        
        public string GetRandomString()
        {
            int randomIndex = Random.Range(0, _strings.Length);
            return _strings[randomIndex];
        }

    }
}