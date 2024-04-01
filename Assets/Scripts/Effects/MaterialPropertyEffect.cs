using AmalgamGames.Conditionals;
using AmalgamGames.Core;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Technie.PhysicsCreator;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class MaterialPropertyEffect : MonoBehaviour, IRespawnable
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventWithTransformations[] _transformationEvents;
        [Space]
        [Title("Material settings")]
        [SerializeField] private string _materialPropertyID;
        [SerializeField] private MaterialPropertyType _materialPropertyType;
        [SerializeField] private Renderer[] _materialRenderers;
        [SerializeField] private bool _createNewInstancedMaterial = false;

        // Material
        private Material[] _materials;
        private int _materialPropertyHash;
        private object _defaultPropertyValue;

        // Coroutines
        private Coroutine[] _lerpRoutines = null;

        // State
        private bool _isSubscribedToEvents = false;

        #region Lifecycle

        private void Awake()
        {
            _materialPropertyHash = Shader.PropertyToID(_materialPropertyID);

            _materials = new Material[_materialRenderers.Length];

            if (_createNewInstancedMaterial)
            {
                // Make a copy of each of the renderer's material and set it, so we can modify it uniquely
                for (int i = 0; i < _materialRenderers.Length; i++)
                {
                    _materials[i] = new Material(_materialRenderers[i].sharedMaterial);
                    _materialRenderers[i].material = _materials[i];
                }
            }
            else
            {
                for (int i = 0; i < _materialRenderers.Length; i++)
                {
                    _materials[i] = _materialRenderers[i].material;
                }
            }

            // Initialise coroutine array
            _lerpRoutines = new Coroutine[_materials.Length];
            for(int i = 0; i < _materials.Length; i++)
            {
                _lerpRoutines[i] = null;
            }

            // Set default property value to initial value of first material
            // Default property value is used when respawning
            if (_materials.Length > 0)
            {
                _defaultPropertyValue = GetCurrentPropertyValue(_materials[0]);
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

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch (evt)
            {
                case RespawnEvent.OnRespawnStart:
                    KillAllCoroutines();
                    ResetPropertyToDefaultValue();
                    break;
            }
        }

        private void ResetPropertyToDefaultValue()
        {
            foreach(Material mat in _materials)
            {
                SetPropertyValue(mat, _defaultPropertyValue);
            }
        }

        private void KillAllCoroutines()
        {
            foreach(Coroutine coroutine in _lerpRoutines)
            {
                if (coroutine != null)
                {
                    StopCoroutine(coroutine);
                }
            }
        }

        #endregion

        #region Material properties

        private void ApplyTransformationToMaterialProperty(ConditionalTransformationGroup[] transformations, float lerpTime, EasingFunction.Ease lerpEasing, object paramValue = null)
        {
            KillAllCoroutines();

            for (int i = 0; i < _materials.Length; i++)
            {
                Material mat = _materials[i];
                // Get current material property value
                object currentValue = paramValue ?? GetCurrentPropertyValue(mat);

                // Apply transformations
                for (int j = 0; j < transformations.Length; j++)
                {
                    ConditionalTransformationGroup t = transformations[j];
                    currentValue = t.TransformObject(currentValue);
                }
                
                // Set new material property value
                if (lerpTime > 0)
                {
                    if (_lerpRoutines[i] != null)
                    {
                        StopCoroutine(_lerpRoutines[i]);
                    }

                    _lerpRoutines[i] = StartCoroutine(lerpValueOverTime(mat, GetCurrentPropertyValue(mat), currentValue, lerpTime, lerpEasing));
                }
                else
                {
                    SetPropertyValue(mat, currentValue);
                }
            }
        }

        private object GetCurrentPropertyValue(Material mat)
        {
            switch (_materialPropertyType)
            {
                case MaterialPropertyType.Int:
                    return mat.GetInt(_materialPropertyHash);
                case MaterialPropertyType.Float:
                    return mat.GetFloat(_materialPropertyHash);
                case MaterialPropertyType.Vector2:
                    Vector4 vec = mat.GetVector(_materialPropertyHash);
                    return new Vector2(vec.x, vec.y);
                case MaterialPropertyType.Vector3:
                    vec = mat.GetVector(_materialPropertyHash);
                    return new Vector3(vec.x, vec.y, vec.z);
                case MaterialPropertyType.Colour:
                    vec = mat.GetVector(_materialPropertyHash);
                    return new Color(vec.x, vec.y, vec.z);
            }
            return null;
        }

        private void SetPropertyValue(Material mat, object newValue)
        {
            switch (_materialPropertyType)
            {
                case MaterialPropertyType.Int:
                    mat.SetInt(_materialPropertyHash,(int)newValue);
                    break;
                case MaterialPropertyType.Float:
                    mat.SetFloat(_materialPropertyHash, (float)newValue);
                    break;
                case MaterialPropertyType.Vector2:
                    mat.SetVector(_materialPropertyHash, (Vector2)newValue);
                    break;
                case MaterialPropertyType.Vector3:
                    mat.SetVector(_materialPropertyHash, (Vector3)newValue);
                    break;
                case MaterialPropertyType.Colour:
                    mat.SetColor(_materialPropertyHash, (Color)newValue);
                    break;
            }
        }

        #endregion

        #region Coroutines

        private IEnumerator lerpValueOverTime(Material mat, object startValue, object endValue, float duration, EasingFunction.Ease easing)
        {
            float objLerp = 0;

            object newValue = null;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            while(objLerp < duration)
            {
                float lerpVal = objLerp / duration;
                switch(_materialPropertyType)
                {
                    case MaterialPropertyType.Int:
                        newValue = func((int)startValue, (int)endValue, lerpVal);
                        break;
                    case MaterialPropertyType.Float:
                        newValue = func((float)startValue, (float)endValue, lerpVal);
                        break;
                    case MaterialPropertyType.Vector2:
                        newValue = Tools.LerpWithEasing((Vector2)startValue, (Vector2)endValue, lerpVal, func);
                        break;
                    case MaterialPropertyType.Vector3:
                        newValue = Tools.LerpWithEasing((Vector3)startValue, (Vector3)endValue, lerpVal, func);
                        break;
                    case MaterialPropertyType.Colour:
                        newValue = Tools.LerpWithEasing((Color)startValue, (Color)endValue, lerpVal, func);
                        break;
                }
                if (newValue != null)
                {
                    SetPropertyValue(mat, newValue);
                }
                objLerp += Time.deltaTime;
                yield return null;
            }

            SetPropertyValue(mat, endValue);
        }

        #endregion

        #region Event callbacks

        private void TriggerEvent(DynamicEventWithTransformations sourceEvent)
        {
            ApplyTransformationToMaterialProperty(sourceEvent.Transformations, sourceEvent.LerpValueDuration, sourceEvent.LerpEasing);
        }

        private void TriggerEventWithParam(DynamicEventWithTransformations sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }
            
            ApplyTransformationToMaterialProperty(sourceTransformationEvent.Transformations, sourceTransformationEvent.LerpValueDuration, sourceTransformationEvent.LerpEasing, sourceTransformationEvent.UseEventParameter ? param : null);
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
        public EasingFunction.Ease LerpEasing = EasingFunction.Ease.Linear;
        public bool UseEventParameter = false;
    }
}
