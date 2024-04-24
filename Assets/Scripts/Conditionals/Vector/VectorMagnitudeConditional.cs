using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    [CreateAssetMenu(menuName ="Conditionals/VectorMagnitude", fileName ="VectorMagnitudeConditional")]
    public class VectorMagnitudeConditional : ConditionalCheck
    {
        [Title("Vector input")]
        [SerializeField] private ConditionalOperator _operator;
        [SerializeField] private float _magnitude;

        #region Conditional logic

        public override bool ApplyCheck(object value)
        {
            if (value.GetType() == typeof(Vector2))
            {
                float inputMagnitude = ((Vector2)value).magnitude;
                return ApplyConditionalOperator(inputMagnitude, _operator, _magnitude);
            }
            else if (value.GetType() == typeof(Vector3))
            {
                float inputMagnitude = ((Vector3)value).magnitude;
                return ApplyConditionalOperator(inputMagnitude, _operator, _magnitude);
            }
            else if (value.GetType() == typeof(Vector4))
            {
                float inputMagnitude = ((Vector4)value).magnitude;
                return ApplyConditionalOperator(inputMagnitude, _operator, _magnitude);
            }

            return false;
        }

        #endregion
    }
}