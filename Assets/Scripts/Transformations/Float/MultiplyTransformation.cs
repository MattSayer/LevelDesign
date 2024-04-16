using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/Multiply", fileName ="MultiplyTransformation")]
    public class MultiplyTransformation : Transformation
    {
        [SerializeField] private float _multiplier;

        public override object TransformInput(object input)
        {
            if (input.GetType() == typeof(float))
            {
                input = ((float)input) * _multiplier;
            }
            else if (input.GetType() == typeof(int))
            {
                input = ((int)input) * _multiplier;
            }
            else if (input.GetType() == typeof(Vector2))
            {
                input = ((Vector2)input) * _multiplier;
            }
            else if (input.GetType() == typeof(Vector3))
            {
                input = ((Vector3)input) * _multiplier;
            }

            return input;
        }
    }
}