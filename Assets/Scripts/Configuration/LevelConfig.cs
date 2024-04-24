using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AmalgamGames.Config
{
    [CreateAssetMenu(menuName = "Config/LevelConfig", fileName = "NewLevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Title("Details")]
        [SerializeField] private string _levelID;
        [SerializeField] private string _levelName;
        [SerializeField] private Image _thumbnail;
        [Space]
        [Title("Points Thresholds")]
        [SerializeField] private int _oneStarThreshold;
        [SerializeField] private int _twoStarThreshold;
        [SerializeField] private int _threeStarThreshold;
        [Space]
        [Title("Thresholds")]
        [SerializeField] private float _thresholdTime;
        [SerializeField] private int _thresholdRespawns;
        [SerializeField] private int _thresholdLaunches;
        [Space]
        [Title("Initial properties")]
        [SerializeField] private int _maxJuice = 100;
        [Space]
        [Title("Scene")]
        [SerializeField] private string _sceneName;

        // Public accessors
        public string LevelID { get { return _levelID; } }
        public string LevelName { get { return _levelName; } }
        public Image Thumbnail { get { return _thumbnail; } }
        public int OneStarPointsThreshold {  get { return _oneStarThreshold; } }
        public int TwoStarPointsThreshold { get { return _twoStarThreshold; } }
        public int ThreeStarPointsThreshold { get { return _threeStarThreshold; } }
        public float ThresholdTime { get { return _thresholdTime; } }
        public int ThresholdRespawns { get { return _thresholdRespawns; } }
        public int ThresholdLaunches { get { return _thresholdLaunches; } }
        public int MaxJuice { get { return _maxJuice; } }
        public string SceneName { get { return _sceneName; } }
    }
}