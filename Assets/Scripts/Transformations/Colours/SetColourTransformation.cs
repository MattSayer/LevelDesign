using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/SetColour", fileName = "SetColour_Transformation")]
    public class SetColourTransformation : Transformation
    {
        [SerializeField] private Color _colour;
        public override object TransformInput(object input)
        {
            return _colour;
        }
    }
}