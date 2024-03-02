using AmalgamGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;

namespace AmalgamGames.DebugTools
{
    public class JuiceRefiller : MonoBehaviour
    {
        [Title("Components")]
        [SerializeField] private GameObject _rocketObject;
        [SerializeField] private SharedFloatValue _juice;

        // Subscriptions
        private bool _isSubscribedToInput = false;

        // Components
        private IInputProcessor _inputProcessor;

        #region Lifecyle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(_rocketObject.transform);

            SubscribeToInput();
        }

        private void OnEnable()
        {
            SubscribeToInput();
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        #endregion

        #region Juice

        private void RefillJuice()
        {
            _juice.SetValue(_juice.MaxValue.Value);
        }

        #endregion

        #region Subscriptions

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnRefillJuice -= RefillJuice;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnRefillJuice += RefillJuice;
                _isSubscribedToInput = true;
            }
        }

        #endregion

    }
}