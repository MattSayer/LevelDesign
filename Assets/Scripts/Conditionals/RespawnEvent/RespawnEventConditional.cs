using AmalgamGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Conditionals
{
    [CreateAssetMenu(menuName = "Conditionals/RespawnEvent", fileName = "RespawnEventConditional")]
    public class RespawnEventConditional : ConditionalCheck
    {
        [SerializeField] private RespawnEvent[] _validRespawnEvents;

        public override bool ApplyCheck(object value)
        {
            if(value.GetType() == typeof(RespawnEventInfo))
            {
                RespawnEventInfo thisEvent = (RespawnEventInfo)value;
                foreach(RespawnEvent validEvent in _validRespawnEvents)
                {
                    if(thisEvent.Event == validEvent)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}