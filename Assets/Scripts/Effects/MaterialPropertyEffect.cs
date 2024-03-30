using AmalgamGames.Conditionals;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class MaterialPropertyEffect : MonoBehaviour
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventWithTransformations[] _transformationEvents;
        [Space]
        [Title("Material settings")]
        [SerializeField] private string _materialPropertyID;
        [SerializeField] private MaterialPropertyType _materialPropertyType;
        [SerializeField] private Renderer _materialRenderer;
        [SerializeField] private bool _createNewInstancedMaterial = false;

        // Material
        private Material _material;
        private int _materialPropertyHash;

        // Coroutines
        private Coroutine _lerpRoutine = null;

        // State
        private bool _isSubscribedToEvents = false;

        #region Lifecycle

        private void Awake()
        {
            _materialPropertyHash = Shader.PropertyToID(_materialPropertyID);

            if (_createNewInstancedMaterial)
            {
                // Make a copy of the renderer's material and set it, so we can modify it uniquely
                _material = new Material(_materialRenderer.sharedMaterial);
                _materialRenderer.material = _material;
            }
            else
            {
                _material = _materialRenderer.sharedMaterial;
            }

            SubscribeToEvents();
        }

        private void OnEnable()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        #endregion
        
        #region Material properties

        private void ApplyTransformationToMaterialProperty(ConditionalTransformationGroup[] transformations, float lerpTime, object paramValue = null)
        {
            // Get current material property value
            object currentValue = paramValue ?? GetCurrentPropertyValue();

            // Apply transformations
            for(int i = 0; i < transformations.Length; i++)
            {
                ConditionalTransformationGroup t = transformations[i];
                currentValue = t.TransformObject(currentValue);
            }

            // Set new material property value
            if (lerpTime > 0)
            {
                if(_lerpRoutine != null)
                {
                    StopCoroutine(_lerpRoutine);
                }

                _lerpRoutine = StartCoroutine(lerpValueOverTime(GetCurrentPropertyValue(), currentValue, lerpTime));
            }
            else
            {
                SetPropertyValue(currentValue);
            }
        }

        private object GetCurrentPropertyValue()
        {
            switch (_materialPropertyType)
            {
                case MaterialPropertyType.Int:
                    return _material.GetInt(_materialPropertyHash);
                case MaterialPropertyType.Float:
                    return _material.GetFloat(_materialPropertyHash);
                case MaterialPropertyType.Vector2:
                    Vector4 vec = _material.GetVector(_materialPropertyHash);
                    return new Vector2(vec.x, vec.y);
                case MaterialPropertyType.Vector3:
                    vec = _material.GetVector(_materialPropertyHash);
                    return new Vector3(vec.x, vec.y, vec.z);
                case MaterialPropertyType.Colour:
                    vec = _material.GetVector(_materialPropertyHash);
                    return new Color(vec.x, vec.y, vec.z);
            }
            return null;
        }

        private void SetPropertyValue(object newValue)
        {
            switch (_materialPropertyType)
            {
                case MaterialPropertyType.Int:
                    _material.SetInt(_materialPropertyHash,(int)newValue);
                    break;
                case MaterialPropertyType.Float:
                    _material.SetFloat(_materialPropertyHash, (float)newValue);
                    break;
                case MaterialPropertyType.Vector2:
                    _material.SetVector(_materialPropertyHash, (Vector2)newValue);
                    break;
                case MaterialPropertyType.Vector3:
                    _material.SetVector(_materialPropertyHash, (Vector3)newValue);
                    break;
                case MaterialPropertyType.Colour:
                    _material.SetColor(_materialPropertyHash, (Color)newValue);
                    break;
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator lerpValueOverTime(object startValue, object endValue, float duration)
        {
            float objLerp = 0;

            object newValue = null;

            while(objLerp < duration)
            {
                float lerpVal = objLerp / duration;
                switch(_materialPropertyType)
                {
                    case MaterialPropertyType.Int:
                        newValue = Mathf.Lerp((int)startValue, (int)endValue, lerpVal);
                        break;
                    case MaterialPropertyType.Float:
                        newValue = Mathf.Lerp((float)startValue, (float)endValue, lerpVal);
                        break;
                    case MaterialPropertyType.Vector2:
                        newValue = Vector2.Lerp((Vector2)startValue, (Vector2)endValue, lerpVal);
                        break;
                    case MaterialPropertyType.Vector3:
                        newValue = Vector3.Lerp((Vector3)startValue, (Vector3)endValue, lerpVal);
                        break;
                    case MaterialPropertyType.Colour:
                        newValue = Color.Lerp((Color)startValue, (Color)endValue, lerpVal);
                        break;
                }
                if (newValue != null)
                {
                    SetPropertyValue(newValue);
                }
                objLerp += Time.deltaTime;
                yield return null;
            }

            SetPropertyValue(endValue);
        }

        #endregion

        #region Event callbacks

        private void TriggerEvent(DynamicEventWithTransformations sourceEvent)
        {
            ApplyTransformationToMaterialProperty(sourceEvent.Transformations, sourceEvent.LerpValueDuration);
        }

        private void TriggerEventWithParam(DynamicEventWithTransformations sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }
            
            ApplyTransformationToMaterialProperty(sourceTransformationEvent.Transformations, sourceTransformationEvent.LerpValueDuration, sourceTransformationEvent.UseEventParameter ? param : null);
        }

        #endregion

        #region Subscriptions

        private void SubscribeToEvents()
        {
            if (!_isSubscribedToEvents)
            {
                foreach(DynamicEventWithTransformations evt in _transformationEvents)
                {
                    Tools.SubscribeToDynamicEvents(evt.DynamicEvents, () => TriggerEvent(evt), (DynamicEvent dynEvent, object param) => TriggerEventWithParam(evt, dynEvent, param));
                }

                _isSubscribedToEvents = true;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (_isSubscribedToEvents)
            {
                foreach(DynamicEventWithTransformations evt in _transformationEvents)
                {
                    Tools.UnsubscribeFromDynamicEvents(evt.DynamicEvents);
                }

                _isSubscribedToEvents = false;
            }
        }

        #endregion
    }

    public enum MaterialPropertyType 
    { 
        Float,
        Int,
        Vector2,
        Vector3,
        Colour
    }

    [Serializable]
    public class DynamicEventWithTransformations
    {
        public DynamicEvent[] DynamicEvents;
        public ConditionalTransformationGroup[] Transformations;
        public float LerpValueDuration = 0;
        public bool UseEventParameter = false;
    }
}
