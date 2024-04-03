using AmalgamGames.Core;
using AmalgamGames.ParticleSystems;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class ParticleSystemPropertyEffect : MonoBehaviour
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventWithTransformations[] _transformationEvents;
        [Space]
        [Title("Particle System settings")]
        [SerializeField] private ParticleSystemPropertyModifier _propertyModifier;
        [SerializeField] private ParticleSystem[] _particleSystems;

        // Particle system
        private object _defaultPropertyValue;

        // Coroutines
        private Coroutine[] _lerpRoutines = null;

        // State
        private bool _isSubscribedToEvents = false;

        #region Lifecycle

        private void Awake()
        {
            // Initialise coroutine array
            _lerpRoutines = new Coroutine[_particleSystems.Length];
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                _lerpRoutines[i] = null;
            }

            if(_particleSystems.Length > 0 )
            {
                _defaultPropertyValue = _propertyModifier.GetPropertyValue(_particleSystems[0]);
            }
            

            SubscribeToEvents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
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
            foreach (ParticleSystem particles in _particleSystems)
            {
                _propertyModifier.SetPropertyValue(particles, _defaultPropertyValue);
            }
        }

        private void KillAllCoroutines()
        {
            foreach (Coroutine coroutine in _lerpRoutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
        }

        #endregion

        #region Material properties

        private void ApplyTransformationToParticleSystemProperty(ConditionalTransformationGroup[] transformations, float lerpTime, EasingFunction.Ease lerpEasing, object paramValue = null)
        {
            KillAllCoroutines();

            for (int i = 0; i < _particleSystems.Length; i++)
            {
                ParticleSystem particles = _particleSystems[i];
                // Get current material property value
                object currentValue = paramValue ?? GetCurrentPropertyValue(particles);

                // Apply transformations
                for (int j = 0; j < transformations.Length; j++)
                {
                    ConditionalTransformationGroup t = transformations[j];
                    currentValue = t.TransformObject(currentValue);
                }

                // Set new material property value
                if (lerpTime > 0)
                {
                    if (_lerpRoutines[i] != null)
                    {
                        StopCoroutine(_lerpRoutines[i]);
                    }

                    _lerpRoutines[i] = StartCoroutine(lerpValueOverTime(particles, GetCurrentPropertyValue(particles), currentValue, lerpTime, lerpEasing));
                }
                else
                {
                    SetPropertyValue(particles, currentValue);
                }
            }
        }

        private object GetCurrentPropertyValue(ParticleSystem particles)
        {
            return _propertyModifier.GetPropertyValue(particles);
        }

        private void SetPropertyValue(ParticleSystem particles, object newValue)
        {
            _propertyModifier.SetPropertyValue(particles, newValue);
        }

        #endregion

        #region Coroutines

        private IEnumerator lerpValueOverTime(ParticleSystem particles, object startValue, object endValue, float duration, EasingFunction.Ease easing)
        {
            float objLerp = 0;

            object newValue = null;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            while (objLerp < duration)
            {
                float lerpVal = objLerp / duration;

                if (startValue.GetType() == typeof(int) || startValue.GetType() == typeof(float))
                {
                    newValue = func((float)startValue, (float)endValue, lerpVal);
                }
                else if (startValue.GetType() == typeof(Vector2))
                {
                    newValue = Tools.LerpWithEasing((Vector2)startValue, (Vector2)endValue, lerpVal, func);
                }
                else if(startValue.GetType() == typeof(Vector3))
                {
                    newValue = Tools.LerpWithEasing((Vector3)startValue, (Vector3)endValue, lerpVal, func);
                }
                else if(startValue.GetType() == typeof(Color))
                {
                    newValue = Tools.LerpWithEasing((Color)startValue, (Color)endValue, lerpVal, func);
                }

                if (newValue != null)
                {
                    SetPropertyValue(particles, newValue);
                }
                objLerp += Time.deltaTime;
                yield return null;
            }

            SetPropertyValue(particles, endValue);
        }

        #endregion

        #region Event callbacks

        private void TriggerEvent(DynamicEventWithTransformations sourceEvent)
        {
            ApplyTransformationToParticleSystemProperty(sourceEvent.Transformations, sourceEvent.LerpValueDuration, sourceEvent.LerpEasing);
        }

        private void TriggerEventWithParam(DynamicEventWithTransformations sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }

            ApplyTransformationToParticleSystemProperty(sourceTransformationEvent.Transformations, sourceTransformationEvent.LerpValueDuration, sourceTransformationEvent.LerpEasing, sourceTransformationEvent.UseEventParameter ? param : null);
        }

        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach (DynamicEventWithTransformations evt in _transformationEvents)
                {
                    Tools.SubscribeToDynamicEvents(evt.DynamicEvents, () => TriggerEvent(evt), (DynamicEvent dynEvent, object param) => TriggerEventWithParam(evt, dynEvent, param));
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach (DynamicEventWithTransformations evt in _transformationEvents)
                {
                    Tools.UnsubscribeFromDynamicEvents(evt.DynamicEvents);
                }

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }
}