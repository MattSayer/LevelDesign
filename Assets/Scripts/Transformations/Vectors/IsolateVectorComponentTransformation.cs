using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/Extractors/IsolateVectorComponent", fileName ="IsolateVectorComponentTransformation")]
    public class IsolateVectorComponentTransformation : Transformation
    {
        [SerializeField] private VectorComponent _vectorComponent;

        public override object TransformInput(object input)
        {
            if(input.GetType()== typeof(Vector2))
            {
                Vector2 vec2 = (Vector2)input;
                switch(_vectorComponent)
                {
                    case VectorComponent.X:
                        return vec2.x;
                    case VectorComponent.Y:
                        return vec2.y;
                    case VectorComponent.Z:
                    default:
                        return input;
                }
                
            }
            else if(input.GetType() == typeof(Vector3))
            {
                Vector3 vec3 = (Vector3)input;
                switch (_vectorComponent)
                {
                    case VectorComponent.X:
                        return vec3.x;
                    case VectorComponent.Y:
                        return vec3.y;
                    case VectorComponent.Z:
                        return vec3.z;
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

    public enum VectorComponent
    {
        X,
        Y,
        Z
    }
}