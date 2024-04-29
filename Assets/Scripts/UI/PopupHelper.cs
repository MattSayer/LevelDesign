using System;
using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AmalgamGames.UI
{
    public class PopupHelper : MonoBehaviour, IPopupHelper
    {
        [Title("Dependency provider")]
        [SerializeField] private DependencyRequest _getPopupHelper;
        [Space]
        [Title("Common components")]
        [SerializeField] private GameObject _popupContainer;
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private TextMeshProUGUI _titleText;
        [Space]
        [Title("Single Button components")]
        [SerializeField] private GameObject _singleButtonContainer;
        [SerializeField] private Button _singleButton;
        [SerializeField] private TextMeshProUGUI _singleButtonText;
        [Space]
        [Title("Two Button components")]
        [SerializeField] private GameObject _twoButtonContainer;
        [SerializeField] private Button _firstButton;
        [SerializeField] private TextMeshProUGUI _firstButtonText;
        [SerializeField] private Button _secondButton;
        [SerializeField] private TextMeshProUGUI _secondButtonText;
        
        // State
        private bool _isSubscribedToDependencyRequests = false;
        private bool _isPopupActive = false;
        
        #region Lifecycle
        
        private void Awake()
        {
            SubscribeToDependencyRequests();
            
            // Hide popup by default
            _popupContainer.SetActive(false);
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
        
        #region Messages
        
        public void DisplayMessage(string message, string title = "", string buttonText = "Okay", Action buttonCallback = null)
        {
            if(_isPopupActive)
            {
                Debug.LogError("Cannot display a new popup while another is active.");
                return;
            }
            
            _messageText.text = message;
            _titleText.text = title;
            _singleButtonText.text = buttonText;
            _singleButton.onClick.AddListener(() => OnSingleButtonClick(buttonCallback));
            
            _singleButtonContainer.SetActive(true);
            _twoButtonContainer.SetActive(false);
            
            _popupContainer.SetActive(true);
            _isPopupActive = true;
            
            // Set currently selected UI element to the button
            _singleButton.Select();
        }
        
        public void DisplayPrompt(string message, string title = "", string firstButtonText = "Okay", string secondButtonText = "Cancel", Action firstButtonCallback = null, Action secondButtonCallback = null)
        {
            if(_isPopupActive)
            {
                Debug.LogError("Cannot display a new popup while another is active.");
                return;
            }
            
            _messageText.text = message;
            _titleText.text = title;
            _firstButtonText.text = firstButtonText;
            _firstButton.onClick.AddListener(() => OnTwoButtonClick(firstButtonCallback));
            _secondButtonText.text = secondButtonText;
            _secondButton.onClick.AddListener(() => OnTwoButtonClick(secondButtonCallback));
            
            _singleButtonContainer.SetActive(false);
            _twoButtonContainer.SetActive(true);
            
            _popupContainer.SetActive(true);
            _isPopupActive = true;
            
            // Set currently selected UI element to the first button
            _firstButton.Select();
        }
        
        #endregion
        
        #region Callbacks
        
        private void OnSingleButtonClick(Action callback)
        {
            _singleButton.onClick.RemoveAllListeners(); 
            callback();
            _popupContainer.SetActive(false);
        }
        
        private void OnTwoButtonClick(Action callback)
        {
            _firstButton.onClick.RemoveAllListeners();
            _secondButton.onClick.RemoveAllListeners();
            callback();
            _popupContainer.SetActive(false);
        }
        
        #endregion
        
        #region Dependency requests
        
        private void ProvideDependency(Action<object> callback)
        {
            callback?.Invoke((IPopupHelper)this);
        }

        private void SubscribeToDependencyRequests()
        {
            if (!_isSubscribedToDependencyRequests)
            {
                _getPopupHelper.OnDependencyRequest += ProvideDependency;
                _isSubscribedToDependencyRequests = true;
            }
        }

        private void UnsubscribeFromDependencyRequests()
        {
            if (_isSubscribedToDependencyRequests)
            {
                _getPopupHelper.OnDependencyRequest -= ProvideDependency;
                _isSubscribedToDependencyRequests = false;
            }
        }
        
        #endregion
        
    }
    
    public interface IPopupHelper
    {
        public void DisplayMessage(string message, string title = "", string buttonText = "Okay", Action buttonCallback = null);
        public void DisplayPrompt(string message, string title = "", string firstButtonText = "Okay", string secondButtonText = "Cancel", Action firstButtonCallback = null, Action secondButtonCallback = null);
    }
    
}