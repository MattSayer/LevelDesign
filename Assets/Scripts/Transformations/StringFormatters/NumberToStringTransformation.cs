using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/String/NumberToString", fileName = "NumberToString_Transformation")]
    public class NumberToStringTransformation : Transformation
    {
        [Title("Settings")]
        [SerializeField] private int _numDecimalPlaces = 0;
        [SerializeField] private string _prefix = "";
        [SerializeField] private string _suffix = "";
        
        public override object TransformInput(object input)
        {
            Type inputType = input.GetType();
            if(inputType == typeof(float) || inputType == typeof(int))
            {
                float inputFloat = Convert.ToSingle(input);
                
                return _prefix + String.Format("{0:N" + _numDecimalPlaces + "}",inputFloat) + _suffix;
            }
            
            return input;
        }
    }
}