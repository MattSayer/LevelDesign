using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName ="Transformations/SetFloatValue", fileName ="SetFloatValueTransformation")]
    public class SetFloatValueTransformation : Transformation
    {
        [Title("Fixed value")]
        [SerializeField] private float _value;

        #region Transformation

        public override object TransformInput(object input)
        {
            return _value;
        }

        #endregion
    }
}