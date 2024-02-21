using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.SceneManagement;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using AmalgamGames.Editor;
using System;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Simulation;

namespace AmalgamGames.Effects
{
    public class RocketTrajectory : ManagedBehaviour, IRespawnable
    {
        [Title("Settings")]
        [SerializeField] private int _stepsToSimulate;
        [SerializeField] private int _physicsTimestepMultiplier = 1;
        [SerializeField, ColorUsage(true,true)] private Color _safeColour;
        [SerializeField, ColorUsage(true, true)] private Color _unsafeColour;
        [Title("Components")]
        [SerializeField] private GameObject _rocketObject;
        [SerializeField] private GameObject _simulatedRocketObjectPrefab;
        [SerializeField] private LineRenderer _lineRenderer;

        // Components
        private SimulatedScene _simulatedScene;
        private PhysicsScene _physicsScene;
        private GameObject _simulatedRocketObject;
        private SimulatedRocketController _simulatedRocketController;
        private IRocketController _rocketController;
        private Material _trajectoryMaterial;

        // State
        private bool _isSimulating = false;
        private bool _isSubscribedToCharging = false;
        private float _chargeLevel;
        private bool _simulatedRocketHasCollided = false;

        // Constants
        private int MAIN_COLOUR_PROP;
        private float PHYSICS_TIMESTEP;

        // Trajectory
        private Vector3[] _points;

        #region Lifecycle

        private void Start()
        {
            PHYSICS_TIMESTEP = Time.fixedDeltaTime * _physicsTimestepMultiplier;
            MAIN_COLOUR_PROP = Shader.PropertyToID(Globals.MAIN_COLOUR_KEY);

            GameObject[] colliders = GameObject.FindGameObjectsWithTag("Collider");

            // Generate physics scene
            _simulatedScene = PhysicsSimulation.CreateSimulatedScene(_simulatedRocketObjectPrefab, colliders);

            // Extract references
            _simulatedRocketObject = _simulatedScene.simulatedObject;
            _physicsScene = _simulatedScene.simulatedPhysicsScene;
            _simulatedRocketController = _simulatedRocketObject.GetComponent<SimulatedRocketController>();
            _rocketController = _rocketObject.GetComponent<IRocketController>();

            // Set up line renderer
            _lineRenderer.positionCount = _stepsToSimulate;
            _points = new Vector3[_stepsToSimulate];
            _lineRenderer.enabled = false;
            _trajectoryMaterial = _lineRenderer.material;

            // Subscribe to charging event
            SubscribeToCharging();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToCharging();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromCharging();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromCharging();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if (_isSimulating)
            {
                SimulateLaunch();
            }
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    StopSimulating();
                    break;
            }
        }

        #endregion

        #region Simulation

        public void SimulateLaunch()
        {
            _simulatedRocketObject.transform.position = _rocketObject.transform.position;
            _simulatedRocketObject.transform.rotation = _rocketObject.transform.rotation;
            
            _simulatedRocketController.ChargeLevel = _chargeLevel;

            _simulatedRocketController.Launch();

            ResetSimulatedRocketCollision();

            _simulatedRocketController.OnCollision += OnSimulatedRocketCollision;
            for(int i = 0; i < _stepsToSimulate; i++)
            {
                _physicsScene.Simulate(PHYSICS_TIMESTEP);
                _simulatedRocketController.ManualFixedUpdate(PHYSICS_TIMESTEP);
                _points[i] = _simulatedRocketObject.transform.position;
                _lineRenderer.SetPosition(i, _points[i]);
                if(_simulatedRocketHasCollided)
                {
                    _trajectoryMaterial.SetColor(MAIN_COLOUR_PROP, _unsafeColour);
                    _lineRenderer.positionCount = i;
                    break;
                }
            }
            _simulatedRocketController.OnCollision -= OnSimulatedRocketCollision;
        }

        protected void StartSimulating()
        {
            _isSimulating = true;
            _lineRenderer.enabled = true;
        }

        protected void StopSimulating()
        {
            _chargeLevel = 0;
            _isSimulating = false;
            _lineRenderer.enabled = false;
        }

        #endregion

        #region Collision

        private void OnSimulatedRocketCollision()
        {
            _simulatedRocketHasCollided = true;
        }

        private void ResetSimulatedRocketCollision()
        {
            _lineRenderer.positionCount = _stepsToSimulate;
            _trajectoryMaterial.SetColor(MAIN_COLOUR_PROP, _safeColour);
            _simulatedRocketHasCollided = false;
        }

        #endregion

        #region Rocket events

        private void OnChargeLevelChanged(object rawValue)
        {
            if(rawValue.GetType() == typeof(float))
            {
                float chargeLevel = (float)rawValue;
                if(_chargeLevel == 0 && chargeLevel > 0)
                {
                    StartSimulating();
                }
                _chargeLevel = chargeLevel;
            }
        }

        private void OnLaunch(LaunchInfo launchInfo)
        {
            StopSimulating();
        }

        #endregion

        #region Subscriptions

        private void SubscribeToCharging()
        {
            if (!_isSubscribedToCharging && _rocketController != null)
            {
                ((IValueProvider)_rocketController).SubscribeToValue(Globals.CHARGE_LEVEL_CHANGED_KEY, OnChargeLevelChanged);
                _rocketController.OnLaunch += OnLaunch;
                _isSubscribedToCharging = true;
            }
        }

        private void UnsubscribeFromCharging()
        {
            if (_isSubscribedToCharging && _rocketController != null)
            {
                ((IValueProvider)_rocketController).UnsubscribeFromValue(Globals.CHARGE_LEVEL_CHANGED_KEY, OnChargeLevelChanged);
                _rocketController.OnLaunch -= OnLaunch;
                _isSubscribedToCharging = false;
            }
        }

        #endregion

    }
}