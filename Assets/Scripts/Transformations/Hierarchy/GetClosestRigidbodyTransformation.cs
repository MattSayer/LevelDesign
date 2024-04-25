using AmalgamGames.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Hierarchy/GetClosestRigidbody", fileName = "GetClosestRigidbody_Transformation")]
    public class GetClosestRigidbodyTransformation : Transformation
    {
        public override object TransformInput(object input)
        {
            Transform transform;

            var type = input.GetType();

            var hasTransform = type.GetProperty("transform");

            if(hasTransform != null)
            {
                transform = (Transform)hasTransform.GetValue(input);
                return Tools.GetClosestParentComponent<Rigidbody>(transform);
            }

            return input;
        }
    }
}