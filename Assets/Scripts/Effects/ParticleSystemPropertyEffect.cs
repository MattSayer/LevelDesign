using AmalgamGames.Core;
using AmalgamGames.ParticleSystems;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace AmalgamGames.Effects
{
    public class ParticleSystemPropertyEffect : DynamicEventsEffect
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithTransformations[] _transformationEvents;
        [Space]
        [Title("Particle System settings")]
        [SerializeField] private ParticleSystemPropertyModifier _propertyModifier;
        [SerializeField] private ParticleSystem[] _particleSystems;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _transformationEvents;

        // Particle system
        private object _defaultPropertyValue;

        // Coroutines
        private Coroutine _lerpRoutine = null;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

            if(_particleSystems.Length > 0 )
            {
                _defaultPropertyValue = _propertyModifier.GetPropertyValue(_particleSystems[0]);
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
            foreach (ParticleSystem particles in _particleSystems)
            {
                _propertyModifier.SetPropertyValue(particles, _defaultPropertyValue);
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

        #region Material properties

        private void ApplyTransformationToParticleSystemProperty(ConditionalTransformationGroup[] transformations, float lerpTime, EasingFunction.Ease lerpEasing, object paramValue = null)
        {
            if(_particleSystems.Length == 0)
            {
                return;
            }

            KillAllCoroutines();

            // Get current property value for first particle system
            object transformedValue = paramValue ?? GetCurrentPropertyValue(_particleSystems[0]);

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

                _lerpRoutine = StartCoroutine(lerpValueOverTime(_particleSystems, transformedValue, lerpTime, lerpEasing));
            }
            else
            {
                for (int i = 0; i < _particleSystems.Length; i++)
                {
                    ParticleSystem particles = _particleSystems[i];
                    // Set new material property value
                    SetPropertyValue(particles, transformedValue);
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

        private IEnumerator lerpValueOverTime(ParticleSystem[] particles, object endValue, float duration, EasingFunction.Ease easing)
        {
            float objLerp = 0;

            object newValue = null;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            object startValue = GetCurrentPropertyValue(particles[0]);

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
                    foreach(ParticleSystem p in particles)
                    {
                        SetPropertyValue(p, newValue);
                    }
                }
                objLerp += Time.deltaTime;
                yield return null;
            }

            foreach (ParticleSystem p in particles)
            {
                SetPropertyValue(p, endValue);
            }
            
        }

        #endregion

        #region Event callbacks

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceEvent;
            ApplyTransformationToParticleSystemProperty(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceTransformationEvent;
            ApplyTransformationToParticleSystemProperty(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing, evt.UseEventParameter ? param : null);
        }

        #endregion
    }
}