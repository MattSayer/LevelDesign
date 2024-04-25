using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/Vector/Normalize", fileName ="NormalizeTransformation")]
    public class NormalizeTransformation : Transformation
    {
        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(Vector3))
            {
                input = ((Vector3)input).normalized;
            }
            else if(input.GetType() == typeof(Quaternion))
            {
                input = ((Quaternion)input).normalized;
            }
            else if(input.GetType() == typeof (Vector2))
            {
                input = ((Vector2)input).normalized;
            }

            return input;
        }
    }
}