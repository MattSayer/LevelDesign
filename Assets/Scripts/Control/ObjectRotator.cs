using AmalgamGames.Core;
using AmalgamGames.Editor;
using AmalgamGames.UpdateLoop;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.Transformation;

namespace AmalgamGames.Control
{
    public class ObjectRotator : ManagedBehaviour
    {
        [Title("Settings")]
        [SerializeField] private InputCoordinateSpace _inputSpace;
        [SerializeField] private float _rotateSpeed = 1;
        [Space]
        [Title("Vector source")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("Transformations")]
        [SerializeField] private ConditionalTransformationGroup[] _transformations;

        private IValueProvider _valueProvider => valueProvider as IValueProvider;

        // STATE
        private bool _isSubscribed = false;
        private Vector3 _cachedDirection = Vector3.zero;

        #region Lifecycle

        private void Start()
        {
            SubscribeToValue();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToValue();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromValue();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromValue();
        }

        public override void ManagedUpdate(float deltaTime)
        {
            if (_cachedDirection != Vector3.zero)
            {
                Vector3 newForward = transform.forward;
                switch (_inputSpace)
                {
                    case InputCoordinateSpace.Global:
                        newForward = _cachedDirection;
                        break;
                    case InputCoordinateSpace.Parent:
                        newForward = transform.parent.TransformDirection(_cachedDirection);
                        break;
                }

                transform.forward = Vector3.MoveTowards(transform.forward, newForward, deltaTime * _rotateSpeed);
            }
        }


        #endregion

        #region Dynamic value

        private void OnValueChanged(object rawValue)
        {
            Vector3 inputDirection;
            if (rawValue.GetType() == typeof(Vector2))
            {
                inputDirection = (Vector2)rawValue;
            }
            else if (rawValue.GetType() == typeof(Vector3))
            {
                inputDirection = (Vector3)rawValue;
            }
            else
            {
                return;
            }

            for (int i = 0; i < _transformations.Length; i++)
            {
                ConditionalTransformationGroup t = _transformations[i];
                inputDirection = (Vector3)t.TransformObject(inputDirection);
            }

            _cachedDirection = inputDirection;

        }

        #endregion

        #region Subscriptions

        private void SubscribeToValue()
        {
            if (!_isSubscribed && _valueProvider != null)
            {
                _valueProvider.SubscribeToValue(_valueKey, OnValueChanged);
                _isSubscribed = true;
            }
        }

        private void UnsubscribeFromValue()
        {
            if (_isSubscribed && _valueProvider != null)
            {
                _valueProvider.UnsubscribeFromValue(_valueKey, OnValueChanged);
                _isSubscribed = false;
            }
        }

        #endregion

        // Can't use local space since we're manipulating local space by rotating, and would therefore create recursion
        private enum InputCoordinateSpace
        {
            Global,
            Parent
        }
    }
}