using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    public abstract class ConditionalCheck : ScriptableObject
    {
        public abstract bool ApplyCheck(object value);

        protected bool ApplyConditionalOperator(float firstValue, ConditionalOperator conditionalOperator, float secondValue)
        {
            switch(conditionalOperator)
            {
                case ConditionalOperator.Equal:
                    return firstValue == secondValue;
                case ConditionalOperator.GreaterThan:
                    return firstValue > secondValue;
                case ConditionalOperator.GreaterThanOrEqual:
                    return firstValue >= secondValue;
                case ConditionalOperator.LessThanOrEqual:
                    return firstValue <= secondValue;
                case ConditionalOperator.LessThan:
                    return firstValue < secondValue;
            }
            return false;
        }
    }

    public enum ConditionalOperator
    {
        LessThan,
        LessThanOrEqual,
        Equal,
        GreaterThan,
        GreaterThanOrEqual
    }

    [Serializable]
    public class ConditionalGroup
    {
        public ConditionalORGroup[] OrChecks;
    }

    [Serializable]
    public class ConditionalORGroup
    {
        public ConditionalCheckItem[] AndChecks;
    }

    [Serializable]
    public class ConditionalCheckItem
    {
        public ConditionalCheck Check;
        public bool NegateCheck = false;
    }
}