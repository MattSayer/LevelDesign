using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/Vector/VectorMultiplication", fileName ="VectorMultiplyTransformation")]
    public class VectorMultiplyTransformation : Transformation
    {
        [Title("Vector")]
        [SerializeField] private VectorType _vectorType;
        [ShowIf("@_vectorType == VectorType.Vector2")]
        [SerializeField] private Vector2 _vector2;
        [ShowIf("@_vectorType == VectorType.Vector3")]
        [SerializeField] private Vector3 _vector3;
        [ShowIf("@_vectorType == VectorType.Vector4")]
        [SerializeField] private Vector4 _vector4;

        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(float) || input.GetType() == typeof(int)) 
            { 
                float floatVal = (float)input;
                switch(_vectorType)
                {
                    case VectorType.Vector2:
                        return floatVal * _vector2;
                    case VectorType.Vector3:
                        return floatVal * _vector3;
                    case VectorType.Vector4:
                        return floatVal * _vector4;
                    default:
                        return input;
                }
            }
            else
            {
                return input;
            }
        }
    }

    public enum VectorType
    {
        Vector2,
        Vector3, 
        Vector4
    }
}
