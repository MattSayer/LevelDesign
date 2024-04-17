using AmalgamGames.Abilities;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class NudgeRumble : MonoBehaviour, IRespawnable
    {
        [Title("Rumble settings")]
        [SerializeField] private float _maxHighIntensity;
        [SerializeField] private float _maxLowIntensity;
        [SerializeField] private float _directionPower = 2;
        [Title("Components")]
        [SerializeField] private Transform _nudgeTransform;
        [Space]
        [Title("Dependency Requests")]
        [SerializeField] private DependencyRequest _getRumbleController;

        // State
        private bool _isSubscribedToNudger = false;

        // Components
        private INudger _nudger;
        private IRumbleController _rumbleController;

        #region Lifecycle

        private void Start()
        {
            _nudger = Tools.GetFirstComponentInHierarchy<INudger>(_nudgeTransform);
            SubscribeToNudger();

            _getRumbleController.RequestDependency(ReceiveRumbleController);
        }

        private void OnDisable()
        {
            UnsubscribeFromNudger();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNudger();
        }

        private void OnEnable()
        {
            SubscribeToNudger();
        }

        #endregion

        #region Respawning/Restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    _rumbleController?.StopContinuousRumble(this);
                    break;
            }
        }

        #endregion

        #region Dependency Requests

        private void ReceiveRumbleController(object rawObj)
        {
            _rumbleController = rawObj as IRumbleController;
        }

        #endregion

        #region Nudge

        private void OnNudgeDirectionChanged(object rawNudgeDirection)
        {
            if (rawNudgeDirection.GetType() == typeof(Vector2))
            {
                Vector2 nudgeDirection = (Vector2)rawNudgeDirection;
                float nudgeMagnitude = Mathf.Pow(nudgeDirection.magnitude, _directionPower);
                RumbleIntensity rumbleIntensity = new RumbleIntensity(_maxLowIntensity * nudgeMagnitude, _maxHighIntensity * nudgeMagnitude);
                _rumbleController.ContinuousRumble(this, rumbleIntensity);
            }
        }

        private void OnNudgeEnd()
        {
            _rumbleController.StopContinuousRumble(this);
        }

        #endregion

        #region Subscriptions

        private void SubscribeToNudger()
        {
            if(!_isSubscribedToNudger && _nudger != null)
            {
                ((IValueProvider)_nudger).SubscribeToValue(Globals.NUDGE_DIRECTION_CHANGED_KEY, OnNudgeDirectionChanged);
                _nudger.OnNudgeEnd += OnNudgeEnd;
                _isSubscribedToNudger = true;
            }
        }

        private void UnsubscribeFromNudger()
        {
            if (!_isSubscribedToNudger && _nudger != null)
            {
                ((IValueProvider)_nudger).UnsubscribeFromValue(Globals.NUDGE_DIRECTION_CHANGED_KEY, OnNudgeDirectionChanged);
                _nudger.OnNudgeEnd -= OnNudgeEnd;
                _isSubscribedToNudger = true;
            }
        }

        #endregion

    }
}