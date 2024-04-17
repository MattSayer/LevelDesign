using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class BurstScreenShakeEffect : DynamicEventsEffect
    {
        [FoldoutGroup("Events")][SerializeField] private ScreenShakeEvent[] _events;
        [Space]
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getScreenShaker;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _events;

        private IScreenShaker _screenShaker;

        #region Lifecycle

        private void Start()
        {
            _getScreenShaker.RequestDependency(ReceiveScreenShaker);
        }

        #endregion

        #region Effects

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            ScreenShakeEvent evt = (ScreenShakeEvent)sourceEvent;

            ScreenShakeBurst(evt.ScreenShakeRequest);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceEvt, DynamicEvent sourceEvent, object param)
        {
            ScreenShakeEvent evt = (ScreenShakeEvent)sourceEvt;

            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }

            if (!evt.UseEventParameter)
            {
                OnTriggerEvent(evt);
            }
            else
            {
                object transformedValue = param;
                
                // Apply transformations
                for (int j = 0; j < evt.Transformations.Length; j++)
                {
                    ConditionalTransformationGroup t = evt.Transformations[j];
                    transformedValue = t.TransformObject(transformedValue);
                }

                if (transformedValue.GetType() == typeof(ScreenShakeBurstRequest))
                {
                    ScreenShakeBurst((ScreenShakeBurstRequest)transformedValue);
                }
            }
        }

        private void ScreenShakeBurst(ScreenShakeBurstRequest request)
        {
            ScreenShakeIntensity intensity = new ScreenShakeIntensity { Amplitude = request.Amplitude, Frequency = request.Frequency };
            _screenShaker.ScreenShakeBurst(intensity, request.Duration, request.Easing);
        }

        #endregion

        #region Dependencies

        private void ReceiveScreenShaker(object rawObj)
        {
            _screenShaker = rawObj as IScreenShaker;
        }

        #endregion

    }

    [Serializable]
    public class ScreenShakeBurstRequest
    {
        public float Amplitude;
        public float Frequency;
        public float Duration;
        public EasingFunction.Ease Easing = EasingFunction.Ease.Linear;
    }

    [Serializable]
    public class ScreenShakeEvent : DynamicEventsContainer
    {
        public ConditionalTransformationGroup[] Transformations;
        [ShowIf("@UseEventParameter == false")]
        public ScreenShakeBurstRequest ScreenShakeRequest;
    }
}