using AmalgamGames.Core;
using AmalgamGames.Editor;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Control
{
    [RequireComponent(typeof(Collider))]
    public class TriggerZone : MonoBehaviour, IRespawnable
    {
        [Title("Trigger")]
        [RequireInterface(typeof(ITriggerable))]
        [SerializeField] private UnityEngine.Object[] _triggerObjects;
        [Space]
        [Title("Settings")]
        [SerializeField] private bool _onlyTriggerOnce = true;
        [SerializeField] private bool _checkTag = false;
        [ShowIf("@this._checkTag == true")]
        [SerializeField] private string _tagToCheck = "";
        [SerializeField] private float _transformCacheTime = 0.2f;
        [SerializeField] private bool _resetOnRespawn = true;

        private ITriggerable[] _triggers;

        // STATE
        private bool _hasTriggered = false;
        private List<Transform> _cachedTriggerParents = new List<Transform>();

        #region Lifecycle

        private void Start()
        {
            _triggers = new ITriggerable[_triggerObjects.Length];
            for(int i = 0; i < _triggerObjects.Length; i++)
            {
                _triggers[i] = _triggerObjects[i] as ITriggerable;
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

        private void ResetTrigger()
        {
            if (_resetOnRespawn)
            {
                _hasTriggered = false;
            }

            StopAllCoroutines();
            _cachedTriggerParents.Clear();
        }

        #endregion

        #region Triggers

        private void OnTriggerEnter(Collider other)
        {
            bool toTrigger = true;
            
            if(_checkTag)
            {
                if(!other.CompareTag(_tagToCheck))
                {
                    toTrigger = false;
                }
            }

            if(_hasTriggered && _onlyTriggerOnce)
            {
                toTrigger = false;
            }

            Transform colliderParent = other.transform.parent;

            if(_cachedTriggerParents.Contains(colliderParent))
            {
                toTrigger = false;
            }

            if (toTrigger)
            {
                
                foreach(ITriggerable trigger in _triggers)
                {
                    trigger.Trigger();
                }
                _hasTriggered = true;
                
                _cachedTriggerParents.Add(colliderParent);
                StartCoroutine(removeParentFromCache(colliderParent));
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator removeParentFromCache(Transform parent)
        {
            yield return new WaitForSeconds(_transformCacheTime);
            if(_cachedTriggerParents.Contains(parent))
            {
                _cachedTriggerParents.Remove(parent);
            }
        }

        #endregion

    }



    public interface ITriggerable
    {
        public void Trigger();
    }
}