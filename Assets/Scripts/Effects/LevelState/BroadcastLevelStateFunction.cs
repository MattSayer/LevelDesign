using AmalgamGames.Core;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmalgamGames.Effects
{
    [CreateAssetMenu(menuName = "Trigger Functions/BroadcastLevelState", fileName = "BroacastLevelState_TriggerFunction")]
    public class BroadcastLevelStateFunction : TriggerFunction
    {
        [Title("Level state")]
        [SerializeField] private LevelState _stateToBroadcast;

        public override void RunTriggerFunction(object triggerObject)
        {
            // Get all level state listeners
            var listeners = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<ILevelStateListener>();
            foreach (ILevelStateListener listener in listeners)
            {
                listener.OnLevelStateChanged(_stateToBroadcast);
            }
        }

    }
}