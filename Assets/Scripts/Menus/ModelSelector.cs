using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Config;
using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.Transformation;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace AmalgamGames.Menus
{
    public class ModelSelector : MonoBehaviour
    {
        [Title("Models")]
        [SerializeField] private GameObject[] _modelPrefabs;
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
        private Transform[] _models;
        private float[] _modelAngles;
        private Vector3 _initialForward;
        
        // Coroutines        
        private Coroutine _rotateRoutine = null;
        
        #region Lifecycle
        
        private void Start()
        {
            InitialiseModels();
        }
        
        private void OnEnable()
        {
            SubscribeToValue();
        }
        
        private void OnDisable()
        {
            UnsubscribeFromValue();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromValue();
        }
        
        #endregion
        
        #region Initialisation
        
        private void InitialiseModels()
        {
            // Initial forward is pointing towards camera
            _initialForward = -Camera.main.transform.forward;
            
            int numModels = _modelPrefabs.Length;
            _models = new Transform[numModels];
            _modelAngles = new float[numModels];
            
            float angleBetween = 360f / numModels;
            for(int i = 0; i < numModels; i++)
            {
                _models[i] = Instantiate(_modelPrefabs[i], _centrePoint).transform;
                
                // Space evenly around centre point
                float angle = angleBetween * i;
                _modelAngles[i] = angle;
                
                Vector3 directionFromCentre = Quaternion.AngleAxis(angle, Vector3.up) * _initialForward;
                
                _models[i].position = _centrePoint.position + (directionFromCentre * _distFromCentre);
            }
        }
        
        #endregion
        
        #region Model change
        
        private void OnModelIndexChanged(object value)
        {
            object finalValue = value;

            foreach(ConditionalTransformation transformation in _transformations)
            {
                finalValue = transformation.TransformObject(finalValue);
            }
            
            if(finalValue.GetType() == typeof(int))
            {
                int newIndex = Convert.ToInt32(finalValue);
                
                // Rotate centre point to bring model to front of camera
                
                if(_rotateRoutine != null)
                {
                    StopCoroutine(_rotateRoutine);
                }
                
                _rotateRoutine = StartCoroutine(rotateModelToFront(_modelAngles[newIndex]));
                
                OnModelChanged?.Invoke(_models[newIndex]);
            }
        }
        
        #endregion
        
        #region Coroutines
        
        private IEnumerator rotateModelToFront(float angle)
        {
            float rotateLerp = 0;
            
            Vector3 targetDirection = Quaternion.AngleAxis(angle, Vector3.up) * _initialForward;
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection,Vector3.up);
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
        
        #region Subscriptions
        
        private void SubscribeToValue()
        {
            if(!_isSubscribed)
            {
                _valueProvider.SubscribeToValue(_valueKey, OnModelIndexChanged);
                _isSubscribed = true;
            }
        }


        private void UnsubscribeFromValue()
        {
            if (_isSubscribed)
            {
                _valueProvider.UnsubscribeFromValue(_valueKey, OnModelIndexChanged);
                _isSubscribed = false;
            }
        }

        
        #endregion
    }
    
}