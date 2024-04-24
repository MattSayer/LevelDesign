using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    [CreateAssetMenu(menuName = "Conditionals/Vector2", fileName = "Vector2Conditional")]
    public class Vector2Conditional : ConditionalCheck
    {
        [Title("Vector input")]
        [SerializeField] private bool _checkXComponent = false;
        [ShowIf("@_checkXComponent == true")]
        [SerializeField] private ConditionalOperator _xCheckOperator;
        [ShowIf("@_checkXComponent == true")]
        [SerializeField] private float _xCheckValue = 0;
        [Space]
        [SerializeField] private bool _checkYComponent = false;
        [ShowIf("@_checkYComponent == true")]
        [SerializeField] private ConditionalOperator _yCheckOperator;
        [ShowIf("@_checkYComponent == true")]
        [SerializeField] private float _yCheckValue = 0;

        #region Conditional logic

        public override bool ApplyCheck(object value)
        {
            if (value.GetType() == typeof(Vector2))
            {
                Vector2 vector2 = (Vector2)value;

                bool hasPassed = true;
                if (_checkXComponent)
                {
                    hasPassed = ApplyConditionalOperator(vector2.x, _xCheckOperator, _xCheckValue);
                }

                if (!hasPassed)
                {
                    return false;
                }

                if (_checkYComponent)
                {
                    hasPassed = ApplyConditionalOperator(vector2.y, _yCheckOperator, _yCheckValue);
                }

                return hasPassed;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}