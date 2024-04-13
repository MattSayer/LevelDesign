using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Timing
{
    public class TimedFunctionTrigger : DynamicEventsEffect, IRespawnable
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithTimedFunction[] _dynamicEvents;
        [Space]
        [Title("Reset functions")]
        [SerializeField] private ResetFunction[] _resetFunctions;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _dynamicEvents;

        #region Triggers

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithTimedFunction evt = (DynamicEventsWithTimedFunction)sourceEvent;

            RunTimedFunctions(evt);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTimedEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithTimedFunction evt = (DynamicEventsWithTimedFunction)sourceTimedEvent;

            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }

            if (!evt.UseEventParameter)
            {
                OnTriggerEvent(evt);
            }
            else
            {
                if (param.GetType() == typeof(GameObject))
                {
                    evt.TargetObject = (GameObject)param;
                    RunTimedFunctions(evt);
                }
            }

        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    ResetTrigger();
                    break;
            }
        }

        private async void ResetTrigger()
        {
            StopAllCoroutines();

            foreach(ResetFunction func in _resetFunctions)
            {
                await func.TimedFunction?.RunFunction(func.TargetObject, 0);
            }
        }

        #endregion


        #region Coroutines

        private async void RunTimedFunctions(DynamicEventsWithTimedFunction func)
        {
            List<System.Action> undoFunctions = new List<Action>();
            foreach(TimedFunction timedFunction in func.TimedFunctions)
            {
                System.Action undoFunction = await timedFunction.RunFunction(func.TargetObject, func.Duration);
                if (func.UndoOnComplete)
                {
                    undoFunctions.Add(undoFunction);
                }
            }

            // Execute the undo functions in reverse order
            undoFunctions.Reverse();

            foreach(System.Action undoFunction in undoFunctions)
            {
                undoFunction();
            }

        }

        #endregion


        [Serializable]
        private class DynamicEventsWithTimedFunction : DynamicEventsContainer
        {
            public GameObject TargetObject;
            public float Duration;
            public bool UndoOnComplete;
            public TimedFunction[] TimedFunctions;
        }

        [Serializable]
        private class ResetFunction
        {
            public GameObject TargetObject;
            public TimedFunction TimedFunction;
        }
    }
}