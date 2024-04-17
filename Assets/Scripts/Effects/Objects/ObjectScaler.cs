using AmalgamGames.Core;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class ObjectScaler : DynamicEventsEffect, IRespawnable
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithTransformations[] _transformationEvents;
        [Space]
        [Title("Settings")]
        [SerializeField] private Transform _targetTransform;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _transformationEvents;

        // Scale
        private Vector3 _initialScale;

        // Coroutines
        private Coroutine _scaleRoutine = null;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if (_targetTransform == null)
            {
                _targetTransform = transform;
            }

            _initialScale = _targetTransform.localScale;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch (evt)
            {
                case RespawnEvent.OnRespawnStart:
                    KillAllCoroutines();
                    ResetToInitialScale();
                    break;
            }
        }

        private void ResetToInitialScale()
        {
            _targetTransform.localPosition = _initialScale;
        }

        private void KillAllCoroutines()
        {
            if (_scaleRoutine != null)
            {
                StopCoroutine(_scaleRoutine);
                _scaleRoutine = null;
            }
        }

        #endregion

        #region Effects

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceEvent;
            ScaleObject(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceTransformationEvent;
            ScaleObject(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing, evt.UseEventParameter ? param : null);
        }

        #endregion

        #region Scaling

        private void ScaleObject(ConditionalTransformationGroup[] transformations, float lerpTime, EasingFunction.Ease lerpEasing, object paramValue = null)
        {
            KillAllCoroutines();

            object transformedValue = paramValue ?? _targetTransform.localScale;

            // Apply transformations
            for (int j = 0; j < transformations.Length; j++)
            {
                ConditionalTransformationGroup t = transformations[j];
                transformedValue = t.TransformObject(transformedValue);
            }

            if(transformedValue.GetType() == typeof(Vector3))
            {
                Vector3 targetScale = (Vector3)transformedValue;
                if (lerpTime > 0)
                {
                    if (_scaleRoutine != null)
                    {
                        StopCoroutine(_scaleRoutine);
                    }
                    _scaleRoutine = StartCoroutine(lerpScaleOverTime(targetScale, lerpTime, lerpEasing));
                }
                else
                {
                    _targetTransform.localScale = targetScale;
                }
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator lerpScaleOverTime(Vector3 targetScale, float scaleTime, EasingFunction.Ease easing)
        {
            float scaleLerp = 0;

            Vector3 startScale = _targetTransform.localScale;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            while (scaleLerp < scaleTime)
            {
                float lerpVal = scaleLerp / scaleTime;

                _targetTransform.localScale = Tools.LerpWithEasing(startScale, targetScale, lerpVal, func);

                scaleLerp += Time.deltaTime;
                yield return null;
            }

            _targetTransform.localScale = targetScale;

            _scaleRoutine = null;

        }

        #endregion

    }
}