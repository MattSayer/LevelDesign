using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class TransformPositionEffect : DynamicEventsEffect, IRespawnable
    {
        [FoldoutGroup("Events")][SerializeField] private TransformPositionEvent[] _events;
        [Space]
        [Title("Transform settings")]
        [SerializeField] private Transform _targetTransform;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _events;

        // Position
        private Vector3 _initialLocalPosition;

        // Coroutines
        private Coroutine _moveRoutine = null;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if(_targetTransform == null)
            {
                _targetTransform = transform;
            }

            _initialLocalPosition = _targetTransform.localPosition;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    KillAllCoroutines();
                    ResetToInitialPosition();
                    break;
            }
        }

        private void ResetToInitialPosition()
        {
            _targetTransform.localPosition = _initialLocalPosition;
        }

        private void KillAllCoroutines()
        {
            if(_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
                _moveRoutine = null;
            }
        }

        #endregion

        #region Effects

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            TransformPositionEvent evt = (TransformPositionEvent)sourceEvent;
            MoveTransformToTarget(evt.TargetPosition, evt.TravelTime, evt.UseLocalPositions, evt.Easing);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceEvt, DynamicEvent sourceEvent, object param)
        {
            TransformPositionEvent evt = (TransformPositionEvent)sourceEvt;

            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }

            if(!evt.UseEventParameter)
            {
                OnTriggerEvent(evt);
            }
            else
            {
                if(param.GetType() == typeof(Transform))
                {
                    MoveTransformToTarget((Transform)param, evt.TravelTime, evt.UseLocalPositions, evt.Easing);
                }
                else if(param.GetType() == typeof(Vector3))
                {
                    MoveTransformToPosition((Vector3)param, evt.TravelTime, evt.UseLocalPositions, evt.Easing);
                }
            }
        }

        private void MoveTransformToTarget(Transform target, float travelTime, bool useLocalPositions, EasingFunction.Ease easing)
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
            }

            if (travelTime <= 0)
            {
                if(useLocalPositions)
                {
                    _targetTransform.localPosition = target.localPosition;
                }
                else
                {
                    _targetTransform.position = target.position;
                }
            }
            else
            {
                _moveRoutine = StartCoroutine(lerpPositionOverTime(target, travelTime, useLocalPositions, easing));
            }
        }

        private void MoveTransformToPosition(Vector3 position, float travelTime, bool useLocalPositions, EasingFunction.Ease easing)
        {
            if (_moveRoutine != null)
            {
                StopCoroutine(_moveRoutine);
            }

            if (travelTime <= 0)
            {
                if (useLocalPositions)
                {
                    _targetTransform.localPosition = position;
                }
                else
                {
                    _targetTransform.position = position;
                }
            }
            else
            {
                _moveRoutine = StartCoroutine(lerpPositionOverTime(position, travelTime, useLocalPositions, easing));
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator lerpPositionOverTime(Transform target, float travelTime, bool useLocalPositions, EasingFunction.Ease easing)
        {
            float positionLerp = 0;

            Vector3 startPos = useLocalPositions ? _targetTransform.localPosition : _targetTransform.position;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            while(positionLerp < travelTime)
            {
                float lerpVal = positionLerp / travelTime;
                
                if(useLocalPositions)
                {
                    _targetTransform.localPosition = Tools.LerpWithEasing(startPos, target.localPosition, lerpVal, func);
                }
                else
                {
                    _targetTransform.position = Tools.LerpWithEasing(startPos, target.position, lerpVal, func);
                }

                positionLerp += Time.deltaTime;
                yield return null;
            }

            if (useLocalPositions)
            {
                _targetTransform.localPosition = target.localPosition;
            }
            else
            {
                _targetTransform.position = target.position;
            }

            _moveRoutine = null;

        }

        private IEnumerator lerpPositionOverTime(Vector3 targetPos, float travelTime, bool useLocalPositions, EasingFunction.Ease easing)
        {
            float positionLerp = 0;

            Vector3 startPos = useLocalPositions ? _targetTransform.localPosition : _targetTransform.position;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            while (positionLerp < travelTime)
            {
                float lerpVal = positionLerp / travelTime;

                if (useLocalPositions)
                {
                    _targetTransform.localPosition = Tools.LerpWithEasing(startPos, targetPos, lerpVal, func);
                }
                else
                {
                    _targetTransform.position = Tools.LerpWithEasing(startPos, targetPos, lerpVal, func);
                }

                positionLerp += Time.deltaTime;
                yield return null;
            }

            if (useLocalPositions)
            {
                _targetTransform.localPosition = targetPos;
            }
            else
            {
                _targetTransform.position = targetPos;
            }

            _moveRoutine = null;

        }

        #endregion

    }

    [Serializable]
    public class TransformPositionEvent : DynamicEventsContainer
    {
        public Transform TargetPosition;
        public float TravelTime;
        public EasingFunction.Ease Easing = EasingFunction.Ease.Linear;
        public bool UseLocalPositions = false;
    }
}