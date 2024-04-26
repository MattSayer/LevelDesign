using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.UI
{
    public class ClickProxy : MonoBehaviour
    {
        // Events
        public event Action OnButtonClicked;
        
        #region Click events
        
        public void OnClick()
        {
            OnButtonClicked?.Invoke();
        }
        
        #endregion
    }
}