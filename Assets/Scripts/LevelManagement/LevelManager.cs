using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.Config;
using UnityEngine.SceneManagement;
using AmalgamGames.Core;
using AmalgamGames.Input;

namespace AmalgamGames.LevelManagement
{
    public class LevelManager : MonoBehaviour, ILevelManager
    {

        [Title("Config")]
        [SerializeField] private LevelHierarchyConfig _levelHierarchyConfig;
        [Space]
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getLevelManager;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getInputProcessor;
        [Space]
        [Title("Debug")]
        [SerializeField] private RocketCharacter _testCharacter = RocketCharacter.AllRounder;

        // Singleton
        private static LevelManager _instance;

        // State
        private LevelConfig _currentLevelConfig;
        private CharacterStats _currentCharacterStats;
        //private int _currentDifficultyTierIndex;
        private int _currentThemeIndex;
        private int _currentLevelIndex;

        // Components
        private Scene _currentScene;
        private LoadingScreen _loadingScreen;
        private InputProcessor _inputProcessor;

        // Coroutines
        private Coroutine _loadingRoutine = null;

        // Public accessors
        public LevelConfig CurrentLevelConfig { get { return _currentLevelConfig; } }
        public CharacterStats CurrentCharacterStats { get { return _currentCharacterStats; } }
        public LevelHierarchyConfig LevelHierarchyConfig { get { return _levelHierarchyConfig; } }
        public int CurrentThemeIndex { get { return _currentThemeIndex; } }

        // Dependency state
        private bool _isSubscribedToDependencyRequests = false;

        #region Lifecycle

        private void Awake()
        {
            if(_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitialiseState();

            SubscribeToDependencyRequests();
        }

        #endregion

        #region Initialisation

        private void InitialiseState()
        {
            _currentScene = SceneManager.GetActiveScene();
            
            #if UNITY_EDITOR
            
            // Check if LevelManager has been initialised inside a level (for testing)
            string currentSceneName = _currentScene.name;
            
            foreach(ThemeConfig theme in _levelHierarchyConfig.Themes)
            {
                foreach(LevelConfig level in theme.Levels)
                {
                    if(level.SceneName == currentSceneName)
                    {
                        _currentLevelConfig = level;
                        InitialiseLevel(level);
                        _currentCharacterStats = CharacterStats.GetCharacterStats(_testCharacter);
                        return;
                    }
                }
            }
            
            #endif
        }

        #endregion

        #region Public methods

        public void LoadNextLevel()
        {
            if(HasNextLevel())
            {
                LevelConfig nextLevel = _levelHierarchyConfig.Themes[_currentThemeIndex].Levels[_currentLevelIndex + 1];

                LoadLevel(nextLevel);
            }
        }

        public bool HasNextLevel()
        {
            if(_currentLevelIndex >= _levelHierarchyConfig.Themes[_currentThemeIndex].Levels.Length - 1)
            {
                return false;
            }

            return true;
        }

        public void LoadLevel(LevelConfig levelConfig)
        {
            _currentLevelConfig = levelConfig;
            LoadScene(levelConfig.SceneName, () => InitialiseLevel(levelConfig));
        }

        private void LoadScene(string sceneName, Action onLoadComplete)
        { 
            // Load loading screen scene
            SceneManager.LoadScene(Globals.LOADING_SCREEN_SCENE);

            // Get loading bar component
            _loadingScreen = GameObject.FindObjectOfType<LoadingScreen>();

            if(_loadingRoutine != null)
            {
                return;
            }

            _loadingRoutine = StartCoroutine(loadSceneAsync(sceneName, onLoadComplete));
        }

        public void SetSelectedLevel(LevelConfig levelConfig)
        {
            _currentLevelConfig = levelConfig;
        }

        public void SetCharacter(RocketCharacter character)
        {
            _currentCharacterStats = CharacterStats.GetCharacterStats(character);
        }

        public void LoadSelectedLevel()
        {
            LoadLevel(_currentLevelConfig);
        }

        public void RestartLevel()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName, null);
        }

        public void UpdateCurrentLevelIndex(int index)
        {
            _currentLevelIndex = index;
        }

        public void ChangeCurrentTheme(int newThemeIndex)
        {
            _currentThemeIndex = newThemeIndex;
        }

        #endregion

        #region Level management

        private void InitialiseLevel(LevelConfig levelConfig)
        {
            // Subscribe to InputProcessor to listen for player confirmation

            _inputProcessor = GameObject.FindObjectOfType<InputProcessor>();

            //_inputProcessor.OnConfirm += StartLevel;
            _inputProcessor.OnAnyInput += StartLevel;
            _inputProcessor.SubscribeToAnyButtonPress();

        }

        private void StartLevel()
        {
            //_inputProcessor.OnConfirm -= StartLevel;
            _inputProcessor.OnAnyInput -= StartLevel;

            // Get all ILevelStateListeners and update them that level has started

            var levelStateListeners = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ILevelStateListener>();
            foreach(ILevelStateListener listener in levelStateListeners)
            {
                listener.OnLevelStateChanged(LevelState.Started);
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator loadSceneAsync(string sceneName, Action OnLoadComplete)
        {
            AsyncOperation loadingOp = SceneManager.LoadSceneAsync(sceneName);

            while (!loadingOp.isDone)
            {
                // Update loading screen scene with progress
                _loadingScreen.UpdateProgress(loadingOp.progress);
                yield return null;
            }

            // Update current scene reference
            _currentScene = SceneManager.GetActiveScene();

            OnLoadComplete?.Invoke();

            _loadingRoutine = null;
        }

        #endregion

        #region Dependency provider

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((ILevelManager)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getLevelManager.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getLevelManager.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion
    }

    public interface ILevelManager
    {
        public LevelConfig CurrentLevelConfig {get; }
        public CharacterStats CurrentCharacterStats { get; }
        public LevelHierarchyConfig LevelHierarchyConfig { get; }
        public int CurrentThemeIndex { get; }
        
        /// <summary>
        /// Loads the next level in sequence, based on the current active Level
        /// </summary>
        public void LoadNextLevel();

        /// <summary>
        /// Returns true if the player has unlocked the next level in sequence, based on the current active level
        /// </summary>
        /// <returns></returns>
        public bool HasNextLevel();

        /// <summary>
        /// Loads the specified level
        /// </summary>
        /// <param name="levelConfig"></param>
        public void LoadLevel(LevelConfig levelConfig);

        /// <summary>
        /// Sets the specified LevelConfig as the current Level, but does not load the level yet
        /// </summary>
        /// <param name="levelConfig"></param>
        public void SetSelectedLevel(LevelConfig levelConfig);

        /// <summary>
        /// Sets the provided character as the currently selected character
        /// </summary>
        /// <param name="character"></param>
        public void SetCharacter(RocketCharacter character);

        /// <summary>
        /// Loads the currently selected level with the currently selected character
        /// </summary>
        public void LoadSelectedLevel();

        /// <summary>
        /// Restarts the current level
        /// </summary>
        public void RestartLevel();

        /// <summary>
        /// Loads the specified scene
        /// </summary>
        public void LoadScene(string sceneName);

        /// <summary>
        /// Updates the currently selected theme index, so that it persists between levels and reboots
        /// </summary>
        /// <param name="newThemeIndex"></param>
        public void ChangeCurrentTheme(int newThemeIndex);

        /// <summary>
        /// Updates the current level index for use in determining whether subsequent levels exist
        /// </summary>
        /// <param name="index"></param>
        public void UpdateCurrentLevelIndex(int index);

    }
}