using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    public abstract class ConditionalCheck : ScriptableObject
    {
        public abstract bool ApplyCheck(object value);
    }
}