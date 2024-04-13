using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace AmalgamGames.Timing
{
    public abstract class TimedFunction : ScriptableObject
    {
        public abstract Task<System.Action> RunFunction(GameObject target, float duration);
    }
}