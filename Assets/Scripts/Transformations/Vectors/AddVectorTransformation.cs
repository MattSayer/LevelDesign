using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Vector/AddVector", fileName ="AddVectorTransformation")]
    public class AddVectorTransformation : Transformation
    {
        [SerializeField] private Vector3 _vectorToAdd = Vector3.zero;

        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(Vector3))
            {
                input = ((Vector3)input) + _vectorToAdd;
            }
            else if(input.GetType() == typeof(Vector2)) 
            {
                input = ((Vector2)input) + (Vector2)_vectorToAdd;
            }

            return input;
        }
    }
}