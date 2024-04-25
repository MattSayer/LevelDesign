using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Colour/ColourIntensity", fileName = "ColourIntensity_Transformation")]
    public class ColourIntensityTransformation : Transformation
    {
        [SerializeField] private Color _colour;

        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(float) || input.GetType() == typeof(int))
            {
                float intensity = (float)input;
                return _colour * intensity;
            }
            else
            {
                return input;
            }
        }
    }
}