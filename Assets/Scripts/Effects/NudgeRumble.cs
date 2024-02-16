using AmalgamGames.Abilities;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class NudgeRumble : MonoBehaviour
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

        #region Dependency Requests

        private void ReceiveRumbleController(object rawObj)
        {
            _rumbleController = rawObj as IRumbleController;
        }

        #endregion

        #region Nudge

        private void OnNudgeDirectionChanged(Vector2 nudgeDirection)
        {
            float nudgeMagnitude = Mathf.Pow(nudgeDirection.magnitude, _directionPower);
            RumbleIntensity rumbleIntensity = new RumbleIntensity(_maxLowIntensity * nudgeMagnitude, _maxHighIntensity * nudgeMagnitude);
            _rumbleController.ContinuousRumble(gameObject, rumbleIntensity);
        }

        private void OnNudgeEnd()
        {
            _rumbleController.StopContinuousRumble(gameObject);
        }

        #endregion

        #region Subscriptions

        private void SubscribeToNudger()
        {
            if(!_isSubscribedToNudger && _nudger != null)
            {
                _nudger.OnNudgeDirectionChanged += OnNudgeDirectionChanged;
                _nudger.OnNudgeEnd += OnNudgeEnd;
                _isSubscribedToNudger = true;
            }
        }

        private void UnsubscribeFromNudger()
        {
            if (!_isSubscribedToNudger && _nudger != null)
            {
                _nudger.OnNudgeDirectionChanged -= OnNudgeDirectionChanged;
                _nudger.OnNudgeEnd -= OnNudgeEnd;
                _isSubscribedToNudger = true;
            }
        }

        #endregion

    }
}