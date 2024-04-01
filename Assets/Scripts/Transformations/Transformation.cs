using AmalgamGames.Conditionals;
using AmalgamGames.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    public abstract class Transformation : ScriptableObject
    {
        public abstract object TransformInput(object input);
    }

    [Serializable]
    public class ConditionalTransformation
    {
        public ConditionalGroup _conditionals;
        public Transformation _transformation;

        public object TransformObject(object input)
        {
            bool shouldTransform = Tools.ApplyConditionals(input, _conditionals);
            if(shouldTransform)
            {
                return _transformation.TransformInput(input);
            }
            else
            {
                return input;
            }
        }
    }

    [Serializable]
    public class ConditionalTransformationGroup
    {
        public ConditionalTransformation[] _conditionalTransformations;

        public object TransformObject(object input)
        {
            // Applies the transformation for the first conditional transformation that changes the input
            // (i.e. the first transformation where its conditions return true)
            for(int i = 0; i < _conditionalTransformations.Length; i++)
            {
                object transformedInput = _conditionalTransformations[i].TransformObject(input);
                
                if(transformedInput != input)
                {
                    return transformedInput;
                }
            }

            return input;
        }
    }
}