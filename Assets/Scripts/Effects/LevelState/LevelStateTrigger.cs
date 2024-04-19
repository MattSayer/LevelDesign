using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class LevelStateTrigger : MonoBehaviour, ILevelStateListener
    {
        [Title("ITriggerables to trigger")]
        [SerializeField] private ConditionalTriggerables[] _triggerables;

        #region Level state changes

        public void OnLevelStateChanged(LevelState levelState)
        {
            foreach(ConditionalTriggerables ct in _triggerables)
            {
                bool toTrigger = Tools.ApplyConditionals(levelState, ct.Conditions);

                if(toTrigger)
                {
                    foreach(Triggerable triggerable in ct.Triggerables)
                    {
                        triggerable.TriggerObject.Trigger(triggerable.TriggerKey);
                    }
                }
            }
        }

        #endregion
    }
}