using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace AmalgamGames.UpdateLoop
{
    public class ObjectInitialiser : MonoBehaviour
    {
        private static ObjectInitialiser _instance;
        
        #region Lifecyle
        
        List<IInitialisable> _initialisables = new List<IInitialisable>();
        
        private void Awake()
        {
            if(_instance != null)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(this);
            
            var initialisables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None).OfType<IInitialisable>();
            foreach(IInitialisable initialisable in initialisables)
            {
                _initialisables.Add(initialisable);
                initialisable.OnInitialisation(InitialisationPhase.Awake);
            }
        }
        
        private void Start()
        {
            foreach(IInitialisable initialisable in _initialisables)
            {
                initialisable.OnInitialisation(InitialisationPhase.Start);
            }
        }
        
        
        #endregion
    }
    
    
    public interface IInitialisable
    {
        public void OnInitialisation(InitialisationPhase phase);
    }
    
    public enum InitialisationPhase
    {
        Awake,
        Start
    }
}