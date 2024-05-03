using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace AmalgamGames.Control
{
    public class ObjectSwitcher : MonoBehaviour, IObjectSwitcher
    {
        [Title("Switch history")]
        [SerializeField] private string _defaultObject;
        [SerializeField] private UnityEvent _noHistoryAction;
        [Space]
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getObjectSwitcher;
        
        // State
        private bool _isSubscribedToDependencyRequests = false;
        private Stack<string> _switchHistory = new Stack<string>();
        
        private Dictionary<string, GameObject> _objects = new Dictionary<string, GameObject>();
        private string _currentObject = "";
        
        #region Lifecycle
        
        private void Awake()
        {
            string defaultObject = _defaultObject;
            foreach(Transform child in transform)
            {
                if(String.IsNullOrEmpty(defaultObject))
                {
                    if(child.gameObject.activeSelf)
                    {
                        defaultObject = child.gameObject.name;
                    }
                }
                _objects[child.name] = child.gameObject;
            }
            
            foreach(Transform child in transform)
            {
                if(child.gameObject.name == defaultObject)
                {
                    child.gameObject.SetActive(true);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
            
            _currentObject = defaultObject;
            
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
        
        public void SwitchTo(string objectName)
        {
            if(!_objects.ContainsKey(objectName))
            {
                return;
            }
            
            foreach(string childName in _objects.Keys)
            {
                if(childName == objectName)
                {
                    _objects[childName].SetActive(true);
                }
                else
                {
                    _objects[childName].SetActive(false);
                }
            }
            
            if(!String.IsNullOrEmpty(_currentObject))
            {
                _switchHistory.Push(_currentObject);
            }
            
            _currentObject = objectName;
        }
        
        public void SwitchBack()
        {
            if(_switchHistory.Count > 0)
            {
                string previousObject = _switchHistory.Pop();
                foreach(string childName in _objects.Keys)
                {
                    if(childName == previousObject)
                    {
                        _objects[childName].SetActive(true);
                    }
                    else
                    {
                        _objects[childName].SetActive(false);
                    }
                }
                
                _currentObject = previousObject;
            }
            else
            {
                _noHistoryAction?.Invoke();
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
        public void SwitchBack();
    }
}