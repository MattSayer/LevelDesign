using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class ComponentTypeToggler : ToggleEffect
    {
        [Title("Component type")]
        [SerializeField] private Component _targetComponentType;
        [Space]
        [Title("Settings")]
        [SerializeField] private bool _startDisabled = false;
        [Space]
        [Title("Target objects")]
        [SerializeField] private GameObject[] _targetObjects;

        private List<GameObject> _objectsWithComponent = new List<GameObject>();

        #region Lifecycle

        protected override void Start()
        {
            Type targetType = _targetComponentType.GetType();

            foreach(GameObject target in _targetObjects)
            {
                List<Component> allComponents = target.GetComponentsInChildrenRecursive(targetType);
                foreach(Component component in allComponents)
                {
                    _objectsWithComponent.Add(component.gameObject);
                }
            }

            if(_startDisabled)
            {
                foreach(GameObject obj in _objectsWithComponent)
                {
                    obj.SetActive(false);
                }
            }

            base.Start();
        }

        #endregion

        #region Respawning

        public override void OnRespawnEvent(RespawnEvent evt)
        {
            base.OnRespawnEvent(evt);

            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    ResetEffect();
                    break;
            }
        }

        #endregion

        #region Toggling

        protected override void ActivateEffect()
        {
            foreach(GameObject obj in _objectsWithComponent)
            {
                obj.SetActive(true);
            }
        }

        protected override void DeactivateEffect()
        {
            foreach (GameObject obj in _objectsWithComponent)
            {
                obj.SetActive(false);
            }
        }

        private void ResetEffect()
        {
            bool toSetActive = !_startDisabled;
            foreach (GameObject obj in _objectsWithComponent)
            {
                obj.SetActive(toSetActive);
            }
        }

        #endregion
    }
}