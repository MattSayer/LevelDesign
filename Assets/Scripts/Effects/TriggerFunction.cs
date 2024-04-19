using AmalgamGames.Conditionals;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public abstract class TriggerFunction : ScriptableObject
    {
        public abstract void RunTriggerFunction(object triggerObject);
    }
}