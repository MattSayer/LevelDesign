using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class ObjectSwitcher : MonoBehaviour, IObjectSwitcher
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getObjectSwitcher;
        
        // State
        private bool _isSubscribedToDependencyRequests = false;
        
        private Dictionary<string, GameObject> _panels = new Dictionary<string, GameObject>();

        #region Lifecycle
        
        private void Awake()
        {
            foreach(Transform child in transform)
            {
                _panels[child.name] = child.gameObject;
            }
            
            SubscribeToDependencyRequests();
        }
        
        private void OnEnable() 
        {
            SubscribeToDependencyRequests();
        }
        
        private void OnDisable() 
        {
            UnsubscribeFromDependencyRequests();
        }
        
        private void OnDestroy() 
        {
            UnsubscribeFromDependencyRequests();
        }
        
        #endregion

        #region Switching
        
        public void SwitchTo(string panelName)
        {
            if(!_panels.ContainsKey(panelName))
            {
                return;
            }
            
            foreach(string childName in _panels.Keys)
            {
                if(childName == panelName)
                {
                    _panels[childName].SetActive(true);
                }
                else
                {
                    _panels[childName].SetActive(false);
                }
            }
        }
        
        #endregion
        
        #region Dependency provider

        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IObjectSwitcher)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getObjectSwitcher.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getObjectSwitcher.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }

        #endregion
    }
    
    public interface IObjectSwitcher
    {
        public void SwitchTo(string panelName);
    }
}