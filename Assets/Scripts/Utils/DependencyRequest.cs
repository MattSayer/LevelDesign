using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Utils
{
    [CreateAssetMenu(menuName ="Dependency Request", fileName ="NewDependencyRequest")]
    public class DependencyRequest : ScriptableObject
    {
        public event Action<Action<object>> OnDependencyRequest;

        public void RequestDependency(Action<object> callback)
        {
            OnDependencyRequest?.Invoke(callback);
        }
    }
}