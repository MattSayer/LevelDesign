using AmalgamGames.Core;
using AmalgamGames.Scoring;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Interactables
{
    public class ScoreTrigger : Interactable
    {
        [Title("Score")]
        [SerializeField] private int _scoreValue;

        #region Interacting

        protected override void OnInteract(GameObject other)
        {
            IScoreTracker scoreTracker = Tools.GetFirstComponentInHierarchy<IScoreTracker>(other.transform);
            
            if(scoreTracker != default(IScoreTracker))
            {
                scoreTracker.AddScore(_scoreValue);
            }
        }

        #endregion

        
    }
}