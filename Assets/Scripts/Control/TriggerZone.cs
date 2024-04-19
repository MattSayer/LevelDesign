using AmalgamGames.Conditionals;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Effects;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Control
{
    [RequireComponent(typeof(Collider))]
    public class TriggerZone : MonoBehaviour, IRespawnable
    {
        [Title("Transformations on triggering object")]
        [SerializeField] private Transformation.Transformation[] _triggerTransformations;
        [Space]
        [Title("Conditional trigger functions")]
        [SerializeField] private ConditionalTriggerFunctions[] _triggerFunctions;
        [Space]
        [Title("Conditional triggerables")]
        [SerializeField] private ConditionalTriggerables[] _triggerables;
        [Space]
        [Title("Settings")]
        [SerializeField] private bool _onlyTriggerOnce = true;
        [SerializeField] private float _triggerCacheTime = 0.2f;
        [SerializeField] private bool _resetOnRespawn = true;

        // STATE
        private bool _hasTriggered = false;
        private List<object> _cachedTriggerObjects = new List<object>();

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

        private void ResetTrigger()
        {
            if (_resetOnRespawn)
            {
                _hasTriggered = false;
            }

            StopAllCoroutines();
            _cachedTriggerObjects.Clear();
        }

        #endregion

        #region Triggers

        private void OnTriggerEnter(Collider other)
        {
            bool toTrigger = true;
            
            object targetObject = other.gameObject;

            // Apply transformations
            foreach(Transformation.Transformation transformation in _triggerTransformations)
            {
                targetObject = transformation.TransformInput(targetObject);
            }

            // Check whether we've triggered already
            if(_hasTriggered && _onlyTriggerOnce)
            {
                toTrigger = false;
            }

            if(_cachedTriggerObjects.Contains(targetObject))
            {
                toTrigger = false;
            }

            if (toTrigger)
            {
                // Trigger functions
                foreach(ConditionalTriggerFunctions ctf in _triggerFunctions)
                {
                    bool conditionCheck = Tools.ApplyConditionals(targetObject, ctf.Conditions);
                    
                    if(conditionCheck)
                    {
                        foreach(TriggerFunction func in ctf.TriggerFunctions)
                        {
                            func.RunTriggerFunction(targetObject);
                        }
                    }
                }

                // Triggerables
                foreach(ConditionalTriggerables ct in _triggerables)
                {
                    bool conditionCheck = Tools.ApplyConditionals(targetObject, ct.Conditions);

                    if(conditionCheck)
                    {
                        foreach(Triggerable triggerable in ct.Triggerables)
                        {
                            triggerable.TriggerObject.Trigger(triggerable.TriggerKey);
                        }
                    }
                }

                _hasTriggered = true;
                
                _cachedTriggerObjects.Add(targetObject);
                StartCoroutine(removeParentFromCache(targetObject));
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator removeParentFromCache(object targetObject)
        {
            yield return new WaitForSeconds(_triggerCacheTime);
            if(_cachedTriggerObjects.Contains(targetObject))
            {
                _cachedTriggerObjects.Remove(targetObject);
            }
        }

        #endregion

    }

    [Serializable]
    public class ConditionalTriggerFunctions
    {
        [SerializeField] private ConditionalGroup _conditions;
        [Space]
        [SerializeField] private TriggerFunction[] _triggerFunctions;
        
        public ConditionalGroup Conditions { get { return _conditions; } }    
        public TriggerFunction[] TriggerFunctions { get { return _triggerFunctions; } } 
    }

    [Serializable]
    public class ConditionalTriggerables
    {
        [SerializeField] private ConditionalGroup _conditions;
        [Space]
        [SerializeField] private Triggerable[] _triggerables;
        public Triggerable[] Triggerables { get { return _triggerables; } }
        public ConditionalGroup Conditions { get { return _conditions; } }

    }

    [Serializable]
    public class Triggerable
    {
        [RequireInterface(typeof(ITriggerable))]
        [SerializeField] private UnityEngine.Object _triggerObject;
        [Space]
        [SerializeField] private string _triggerKey;

        public ITriggerable TriggerObject => _triggerObject as ITriggerable;
        public string TriggerKey => _triggerKey;
    }

    public interface ITriggerable
    {
        public void Trigger(string triggerKey);
    }
}