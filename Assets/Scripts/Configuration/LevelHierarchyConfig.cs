using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Config
{
    [CreateAssetMenu(menuName = "Config/LevelHierarchyConfig", fileName = "LevelHierarchyConfig")]
    public class LevelHierarchyConfig : ScriptableObject
    {
        [Title("Themes")]
        [SerializeField] private ThemeConfig[] _themes;
        public ThemeConfig[] Themes { get { return _themes; } }
    }

    [Serializable]
    public class DifficultyTierConfig
    {
        [SerializeField] private string _name;
        [SerializeField] private int _starRequirement;
        [SerializeField] private ThemeConfig[] _themes;

        public string Name { get { return _name; } }
        public int StarRequirement { get { return _starRequirement; } } 
        public ThemeConfig[] Themes { get {  return _themes; } }

    }

    [Serializable]
    public class ThemeConfig
    {
        [SerializeField] private string _name;
        [SerializeField] private int _starRequirement;
        [SerializeField] private Color _colourScheme;
        [SerializeField] private LevelConfig[] _levels;

        public string Name { get { return _name; } }
        public Color ColourScheme { get { return _colourScheme; } }
        public int StarRequirement { get { return _starRequirement; } }
        public LevelConfig[] Levels { get {  return _levels; } }
    }
}