using AmalgamGames.Core;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class AnimatorPropertyEffect : DynamicEventsEffect, IRespawnable
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithTransformations[] _transformationEvents;
        [Space]
        [Title("Animator settings")]
        [SerializeField] private Animator[] _targetAnimators;
        [SerializeField] private AnimatorProperty _animatorProperty;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _transformationEvents;

        private object _defaultPropertyValue;
        
        // Coroutines
        private Coroutine _lerpRoutine = null;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if(_targetAnimators.Length > 0)
            {
                _defaultPropertyValue = GetCurrentPropertyValue(_targetAnimators[0]);
            }
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch (evt)
            {
                case RespawnEvent.OnRespawnStart:
                    KillAllCoroutines();
                    ResetPropertyToDefaultValue();
                    break;
            }
        }

        private void ResetPropertyToDefaultValue()
        {
            foreach (Animator animator in _targetAnimators)
            {
                SetPropertyValue(animator, _defaultPropertyValue);
            }
        }

        private void KillAllCoroutines()
        {
            if (_lerpRoutine != null)
            {
                StopCoroutine(_lerpRoutine);
            }
        }

        #endregion

        #region Animator properties

        private void ApplyTransformationToAnimatorProperty(ConditionalTransformationGroup[] transformations, float lerpTime, EasingFunction.Ease lerpEasing, object paramValue = null)
        {
            KillAllCoroutines();

            // Get current material property value for first material
            object transformedValue = paramValue ?? GetCurrentPropertyValue(_targetAnimators[0]);

            // Apply transformations
            for (int j = 0; j < transformations.Length; j++)
            {
                ConditionalTransformationGroup t = transformations[j];
                transformedValue = t.TransformObject(transformedValue);
            }

            if (lerpTime > 0)
            {
                if (_lerpRoutine != null)
                {
                    StopCoroutine(_lerpRoutine);
                }
                _lerpRoutine = StartCoroutine(lerpValueOverTime(_targetAnimators, transformedValue, lerpTime, lerpEasing));
            }
            else
            {
                for (int i = 0; i < _targetAnimators.Length; i++)
                {
                    Animator animator = _targetAnimators[i];
                    SetPropertyValue(animator, transformedValue);
                }
            }
        }

        private object GetCurrentPropertyValue(Animator animator)
        {
            switch (_animatorProperty)
            {
                case AnimatorProperty.Speed:
                    return animator.speed;
                case AnimatorProperty.PlaybackTime:
                    return animator.playbackTime;
            }
            return null;
        }

        private void SetPropertyValue(Animator animator, object newValue)
        {
            switch (_animatorProperty)
            {
                case AnimatorProperty.Speed:
                    animator.speed = (float)newValue;
                    break;
                case AnimatorProperty.PlaybackTime:
                    animator.playbackTime = (float)newValue;
                    break;
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator lerpValueOverTime(Animator[] animators, object endValue, float duration, EasingFunction.Ease easing)
        {
            if (animators.Length == 0)
            {
                yield break;
            }

            float objLerp = 0;

            object newValue = null;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            object startValue = GetCurrentPropertyValue(animators[0]);

            while (objLerp < duration)
            {
                float lerpVal = objLerp / duration;

                switch (_animatorProperty)
                {
                    case AnimatorProperty.Speed:
                        newValue = func((float)startValue, (float)endValue, lerpVal);
                        break;
                }

                if (newValue != null)
                {
                    foreach (Animator animator in animators)
                    {
                        SetPropertyValue(animator, newValue);
                    }
                }
                objLerp += Time.deltaTime;
                yield return null;
            }

            foreach (Animator animator in animators)
            {
                SetPropertyValue(animator, endValue);
            }
        }

        #endregion

        #region Event callbacks

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceEvent;
            ApplyTransformationToAnimatorProperty(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceTransformationEvent;
            ApplyTransformationToAnimatorProperty(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing, evt.UseEventParameter ? param : null);
        }

        #endregion

    }

    public enum AnimatorProperty
    {
        Speed,
        PlaybackTime,
    }
}