using AmalgamGames.Core;
using AmalgamGames.Editor;
using Sirenix.OdinInspector;
using UnityEngine;
using AmalgamGames.UpdateLoop;

namespace AmalgamGames.Control
{
    public class ObjectPositioner : ManagedBehaviour
    {
        [Title("Settings")]
        [SerializeField] private OffsetMode _offsetMode;
        [SerializeField] private InputCoordinateSpace _inputSpace;
        [Space]
        [Title("Vector source")]
        [RequireInterface(typeof(IValueProvider))]
        [SerializeField] private UnityEngine.Object valueProvider;
        [SerializeField] private string _valueKey;
        [Space]
        [Title("Transformations")]
        [SerializeField] private Transformation.Transformation[] _transformations;

        private IValueProvider _valueProvider => valueProvider as IValueProvider;

        // STATE
        private bool _isSubscribed = false;
        private Vector3 _startOffset;
        private Vector3 _cachedOffset = Vector3.zero;

        #region Lifecycle

        private void Start()
        {
            _startOffset = transform.localPosition;

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
            switch (_offsetMode)
            {
                case OffsetMode.OffsetFromParent:
                    OffsetFromParent(_cachedOffset);
                    break;
                case OffsetMode.OffsetFromStartPosition:
                    OffsetFromStartPosition(_cachedOffset);
                    break;
                case OffsetMode.Absolute:
                    OffsetAbsolute(_cachedOffset);
                    break;
            }
        }


        #endregion

        #region Dynamic value

        private void OnValueChanged(object rawValue)
        {
            for(int i = 0; i < _transformations.Length; i++)
            {
                Transformation.Transformation t = _transformations[i];
                rawValue = t.TransformObject(rawValue);
            }

            Vector3 inputPosition;
            if (rawValue.GetType() == typeof(Vector2))
            {
                inputPosition = (Vector2)rawValue;
            }
            else if(rawValue.GetType() == typeof(Vector3))
            {
                inputPosition = (Vector3)rawValue;
            }
            else
            {
                return;
            }

            _cachedOffset = inputPosition;

        }

        #endregion

        #region

        private void OffsetAbsolute(Vector3 offset)
        {
            switch (_inputSpace)
            {
                case InputCoordinateSpace.Global:
                    transform.position = offset;
                    break;
                case InputCoordinateSpace.Parent:
                    transform.position = transform.parent.TransformPoint(offset);
                    break;
                case InputCoordinateSpace.Local:
                    transform.position = transform.TransformPoint(offset);
                    break;
            }
        }

        private void OffsetFromStartPosition(Vector3 offset)
        {
            transform.localPosition = _startOffset;
            switch(_inputSpace)
            {
                case InputCoordinateSpace.Global:
                    transform.position += offset;
                    break;
                case InputCoordinateSpace.Parent:
                    transform.position += transform.parent.TransformDirection(offset);
                    break;
                case InputCoordinateSpace.Local:
                    transform.position += transform.TransformDirection(offset);
                    break;
            }
        }

        private void OffsetFromParent(Vector3 offset)
        {
            switch(_inputSpace)
            {
                case InputCoordinateSpace.Global:
                    transform.position = transform.parent.position + offset;
                    break;
                case InputCoordinateSpace.Parent:
                    transform.localPosition = offset;
                    break;
                case InputCoordinateSpace.Local:
                    transform.position = transform.parent.position + transform.TransformDirection(offset);
                    break;
            }
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


        private enum OffsetMode
        {
            OffsetFromParent,
            OffsetFromStartPosition,
            Absolute
        }

        private enum InputCoordinateSpace
        {
            Global,
            Parent,
            Local
        }
    }

    
}