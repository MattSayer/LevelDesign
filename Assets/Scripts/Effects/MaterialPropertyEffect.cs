using AmalgamGames.Conditionals;
using AmalgamGames.Core;
using AmalgamGames.Transformation;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using Technie.PhysicsCreator;
using Unity.VisualScripting;
using UnityEngine;

namespace AmalgamGames.Effects
{
    public class MaterialPropertyEffect : DynamicEventsEffect, IRespawnable
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithTransformations[] _transformationEvents;
        [Space]
        [Title("Material settings")]
        [SerializeField] private string _materialPropertyID;
        [SerializeField] private MaterialPropertyType _materialPropertyType;
        [SerializeField] private Renderer[] _materialRenderers;
        [SerializeField] private bool _createNewInstancedMaterial = false;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _transformationEvents;

        // Material
        private Material[] _materials;
        private int _materialPropertyHash;
        private object _defaultPropertyValue;

        // Coroutines
        private Coroutine _lerpRoutine = null;

        #region Lifecycle

        protected override void Awake()
        {
            base.Awake();

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

            // Set default property value to initial value of first material
            // Default property value is used when respawning
            if (_materials.Length > 0)
            {
                _defaultPropertyValue = GetCurrentPropertyValue(_materials[0]);
            }
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
            if (_lerpRoutine != null)
            {
                StopCoroutine(_lerpRoutine);
            }
        }

        #endregion

        #region Material properties

        private void ApplyTransformationToMaterialProperty(ConditionalTransformationGroup[] transformations, float lerpTime, EasingFunction.Ease lerpEasing, object paramValue = null)
        {
            KillAllCoroutines();

            // Get current material property value for first material
            object transformedValue = paramValue ?? GetCurrentPropertyValue(_materials[0]);

            // Apply transformations
            for (int j = 0; j < transformations.Length; j++)
            {
                ConditionalTransformationGroup t = transformations[j];
                transformedValue = t.TransformObject(transformedValue);
            }

            if(lerpTime > 0)
            {
                if(_lerpRoutine != null)
                {
                    StopCoroutine(_lerpRoutine);
                }
                _lerpRoutine = StartCoroutine(lerpValueOverTime(_materials, transformedValue, lerpTime, lerpEasing));
            }
            else
            {
                for (int i = 0; i < _materials.Length; i++)
                {
                    Material mat = _materials[i];
                    SetPropertyValue(mat, transformedValue);
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

        private IEnumerator lerpValueOverTime(Material[] materials, object endValue, float duration, EasingFunction.Ease easing)
        {
            if(materials.Length == 0)
            {
                yield break;
            }

            float objLerp = 0;

            object newValue = null;

            EasingFunction.Function func = EasingFunction.GetEasingFunction(easing);

            object startValue = GetCurrentPropertyValue(materials[0]);

            while (objLerp < duration)
            {
                float lerpVal = objLerp / duration;

                switch (_materialPropertyType)
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
                    foreach (Material mat in materials)
                    {
                        SetPropertyValue(mat, newValue);
                    }
                }
                objLerp += Time.deltaTime;
                yield return null;
            }

            foreach (Material mat in materials)
            {
                SetPropertyValue(mat, endValue);
            }
        }

        #endregion

        #region Event callbacks

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceEvent;
            ApplyTransformationToMaterialProperty(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTransformationEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithTransformations evt = (DynamicEventsWithTransformations)sourceTransformationEvent;
            ApplyTransformationToMaterialProperty(evt.Transformations, evt.LerpValueDuration, evt.LerpEasing, evt.UseEventParameter ? param : null);
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
    public class DynamicEventsWithTransformations : DynamicEventsContainer
    {
        public ConditionalTransformationGroup[] Transformations;
        public float LerpValueDuration = 0;
        public EasingFunction.Ease LerpEasing = EasingFunction.Ease.Linear;
    }

    
}
