using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/Numbers/RaiseToPower", fileName ="RaiseToPowerTransformation")]
    public class RaiseToPowerTransformation : Transformation
    {
        [SerializeField] private float _exponent = 1;

        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(float))
            {
                input = Mathf.Pow((float)input, _exponent);
            }
            else if(input.GetType() == typeof(int)) 
            { 
                input = Mathf.Pow((int)input, _exponent);
            }

            return input;
        }
    }
}