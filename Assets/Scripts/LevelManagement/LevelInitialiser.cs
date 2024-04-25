using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Abilities;
using AmalgamGames.Config;
using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.LevelManagement
{
    public class LevelInitialiser : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private CharacterModel[] _characters;
        [RequireInterface(typeof(IRocketController))]
        [SerializeField] private UnityEngine.Object _rocketControllerObj;
        [SerializeField] private SharedFloatValue _juice;
        [RequireInterface(typeof(ITargetOrienter))]
        [SerializeField] private UnityEngine.Object _targetOrienterObj;
        [RequireInterface(typeof(ICameraController))]
        [SerializeField] private UnityEngine.Object _cameraControllerObj;
        [RequireInterface(typeof(INudger))]
        [SerializeField] private UnityEngine.Object _nudgeObj;
        [RequireInterface(typeof(ISlowmo))]
        [SerializeField] private UnityEngine.Object _slowmoObj;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getLevelManager;
        
        // Components
        private ILevelManager _levelManager;
        private IRocketController _rocketController => _rocketControllerObj as IRocketController;
        private ITargetOrienter _targetOrienter => _targetOrienterObj as ITargetOrienter;
        private ICameraController _cameraController => _cameraControllerObj as ICameraController;
        private INudger _nudger => _nudgeObj as INudger;
        private ISlowmo _slowmo => _slowmoObj as ISlowmo;
        
        #region Lifecycle

        private void Start()
        {
            _getLevelManager.RequestDependency(ReceiveLevelManager);
        }

        #endregion

        #region Initialisation

        private void InitialiseLevel()
        {
            LevelConfig levelConfig = _levelManager.CurrentLevelConfig;
            CharacterStats characterStats = _levelManager.CurrentCharacterStats;
            
            // Juice
            _juice.SetMaxValue(levelConfig.MaxJuice);
            
            // Rocket config
            _rocketController.SetConfig(characterStats.RocketConfig);
            
            // Turn speed
            _targetOrienter.SetRotateSpeed(characterStats.TurnSpeed);
            
            // Camera speed
            _cameraController.SetHorizontalSpeed(characterStats.HorizontalCameraSpeed);
            _cameraController.SetVerticalSpeed(characterStats.VerticalCameraSpeed);
            
            // Nudge
            _nudger.SetNudgeForce(characterStats.NudgeForce);
            _nudger.SetNudgeDrainPerSecond(characterStats.NudgeJuiceDrainPerSecond);
            
            // Slowmo
            _slowmo.SetTimeScale(characterStats.SlowmoTimeScale);
            _slowmo.SetJuiceDrainPerSecond(characterStats.SlowmoJuiceDrainPerSecond);
            
            // Mesh
            foreach(CharacterModel model in _characters)
            {
                if(model.Character == characterStats.Character)
                {
                    model.Model.SetActive(true);
                }
                else
                {
                    model.Model.SetActive(false);
                }
            }
        }

        #endregion

        #region Dependencies

        private void ReceiveLevelManager(object rawObj)
        {
            _levelManager = (ILevelManager)rawObj;
            InitialiseLevel();
        }

        #endregion
        
        [Serializable]
        private class CharacterModel 
        {
            public RocketCharacter Character;
            public GameObject Model;
        }
    }
}