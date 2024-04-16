using AmalgamGames.Effects;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Combiners/ScreenShakeBurstRequest", fileName = "ScreenShakeBurstRequestCombiner_Transformation")]
    public class ScreenShakeBurstRequestCombinerTransformation : Transformation
    {
        [Title("Amplitude")]
        [ShowIf("@_amplitudeTransformations == null || _amplitudeTransformations.Length == 0")]
        [SerializeField] private float _amplitude;
        [SerializeField] private Transformation[] _amplitudeTransformations;
        [Space]
        [Title("Frequency")]
        [ShowIf("@_frequencyTransformations == null || _frequencyTransformations.Length == 0")]
        [SerializeField] private float _frequency;
        [SerializeField] private Transformation[] _frequencyTransformations;
        [Space]
        [Title("Duration")]
        [ShowIf("@_durationTransformations == null || _durationTransformations.Length == 0")]
        [SerializeField] private float _duration;
        [SerializeField] private Transformation[] _durationTransformations;
        [Space]
        [Title("Easing")]
        [ShowIf("@_easingTransformations == null || _easingTransformations.Length == 0")]
        [SerializeField] private EasingFunction.Ease _easing;
        [SerializeField] private Transformation[] _easingTransformations;

        public override object TransformInput(object input)
        {
            ScreenShakeBurstRequest request = new ScreenShakeBurstRequest();

            // Amplitude

            if(_amplitudeTransformations.Length > 0)
            {
                object rawAmplitude = input;
                
                foreach(Transformation transformation in _amplitudeTransformations)
                {
                    rawAmplitude = transformation.TransformInput(rawAmplitude);
                }
                
                if(rawAmplitude.GetType() == typeof(float) || rawAmplitude.GetType() == typeof(int))
                {
                    request.Amplitude = (float)rawAmplitude;
                }
            }
            else
            {
                request.Amplitude = _amplitude;
            }

            // Frequency

            if (_frequencyTransformations.Length > 0)
            {
                object rawFrequency = input;

                foreach (Transformation transformation in _frequencyTransformations)
                {
                    rawFrequency = transformation.TransformInput(rawFrequency);
                }

                if (rawFrequency.GetType() == typeof(float) || rawFrequency.GetType() == typeof(int))
                {
                    request.Frequency = (float)rawFrequency;
                }
            }
            else
            {
                request.Frequency = _frequency;
            }

            // Duration

            if (_durationTransformations.Length > 0)
            {
                object rawDuration = input;

                foreach (Transformation transformation in _durationTransformations)
                {
                    rawDuration = transformation.TransformInput(rawDuration);
                }

                if (rawDuration.GetType() == typeof(float) || rawDuration.GetType() == typeof(int))
                {
                    request.Duration = (float)rawDuration;
                }
            }
            else
            {
                request.Duration = _duration;
            }

            // Easing

            if (_easingTransformations.Length > 0)
            {
                object rawEasing = input;

                foreach (Transformation transformation in _easingTransformations)
                {
                    rawEasing = transformation.TransformInput(rawEasing);
                }

                if (rawEasing.GetType() == typeof(EasingFunction.Ease))
                {
                    request.Easing = (EasingFunction.Ease)rawEasing;
                }
            }
            else
            {
                request.Easing = _easing;
            }

            return request;
        }
    }
}