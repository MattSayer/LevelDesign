using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    public abstract class Transformation : ScriptableObject
    {
        public abstract object TransformObject(object input);
    }
}