using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Config;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class ModelSelector : MonoBehaviour, IValueProvider
    {
        [Title("Models")]
        [SerializeField] private ModelWithKey[] _modelPrefabs;
        [Space]
        [Title("Subscriptions")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Title("Transformation")]
        [SerializeField] private ConditionalTransformation[] _transformations;
        [Space]
        [Title("Settings")]
        [SerializeField] private Transform _centrePoint;
        [SerializeField] private float _distFromCentre = 1;
        [SerializeField] private float _rotationTime = 1;
        
        private IValueProvider _valueProvider => valueProvider as IValueProvider;
        
        // Events
        public event Action<object> OnModelChanged;
        
        // State
        private bool _isSubscribed = false;
        private Dictionary<string, Transform> _models = new Dictionary<string, Transform>();
        private Dictionary<string, float> _modelAngles = new Dictionary<string, float>();
        private Vector3 _initialForward;
        
        // Coroutines        
        private Coroutine _rotateRoutine = null;
        
        #region Lifecycle
        
        private void Awake()
        {
            InitialiseModels();
        }
        
        private void OnEnable()
        {
            SubscribeToModelKeyChanged();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromModelKeyChanged();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromModelKeyChanged();
        }
        
        #endregion
        
        #region Initialisation
        
        private void InitialiseModels()
        {
            // Initial forward is pointing towards camera
            _initialForward = -Camera.main.transform.forward;
            
            int numModels = _modelPrefabs.Length;
            
            float angleBetween = 360f / numModels;
            for(int i = 0; i < numModels; i++)
            {
                string key = _modelPrefabs[i].Key;
                GameObject prefab = _modelPrefabs[i].Model;
                _models[key] = Instantiate(prefab, _centrePoint).transform;
                
                // Space evenly around centre point
                float angle = angleBetween * i;
                _modelAngles[key] = angle;
                
                Vector3 directionFromCentre = Quaternion.AngleAxis(angle, Vector3.up) * _initialForward;
                
                _models[key].position = _centrePoint.position + (directionFromCentre * _distFromCentre);
            }
            
        }
        
        #endregion
        
        #region Model change
        
        private void OnModelKeyChanged(object value)
        {
            object finalValue = value;

            foreach(ConditionalTransformation transformation in _transformations)
            {
                finalValue = transformation.TransformObject(finalValue);
            }
            
            if(finalValue.GetType() == typeof(string))
            {
                string newKey = finalValue.ToString();
                
                // Rotate centre point to bring model to front of camera
                
                if(_rotateRoutine != null)
                {
                    StopCoroutine(_rotateRoutine);
                }
                
                _rotateRoutine = StartCoroutine(rotateModelToFront(_modelAngles[newKey]));
                
                OnModelChanged?.Invoke(_models[newKey]);
            }
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator rotateModelToFront(float angle)
        {
            float rotateLerp = 0;
            
            Vector3 targetDirection = Quaternion.AngleAxis(-angle, Vector3.up) * _initialForward;
            Quaternion targetRotation = Quaternion.LookRotation(-targetDirection,Vector3.up);
            Quaternion currentRotation = _centrePoint.rotation;
            while(rotateLerp < _rotationTime)
            {
                _centrePoint.rotation = Quaternion.Lerp(currentRotation, targetRotation, rotateLerp / _rotationTime);
                rotateLerp += Time.deltaTime;
                yield return null;
            }
            _centrePoint.rotation = targetRotation;
            _rotateRoutine = null;
        }
        
        #endregion
        
        #region Value provider
        
        public void SubscribeToValue(string key, Action<object> callback)
        {
            switch(key)
            {
                case Globals.MODEL_CHANGED_KEY:
                    OnModelChanged += callback;
                    break;
            }
        }
        
        public void UnsubscribeFromValue(string key, Action<object> callback)
        {
            switch(key)
            {
                case Globals.MODEL_CHANGED_KEY:
                    OnModelChanged -= callback;
                    break;
            }
        }
        
        #endregion
        
        #region Subscriptions
        
        private void SubscribeToModelKeyChanged()
        {
            if(!_isSubscribed)
            {
                _valueProvider.SubscribeToValue(_valueKey, OnModelKeyChanged);
                _isSubscribed = true;
            }
        }


        private void UnsubscribeFromModelKeyChanged()
        {
            if (_isSubscribed)
            {
                _valueProvider.UnsubscribeFromValue(_valueKey, OnModelKeyChanged);
                _isSubscribed = false;
            }
        }

        
        #endregion
        
        [Serializable]
        private class ModelWithKey
        {
            public string Key;
            public GameObject Model;
        }
    }
    
}