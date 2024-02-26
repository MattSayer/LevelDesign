using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Scoring
{
    public class ScoreTracker : MonoBehaviour, IRespawnable
    {
        [Title("Settings")]
        [SerializeField] private LayerMask _checkPointLayer;
        [Space]
        [Title("Components")]
        [SerializeField] private SharedIntValue _scoreValue;

        // STATE
        private int _scoreAtLastCheckpoint = 0;
        private int _currentScore;
        
        private bool _isSubscribedToScore = false;

        #region Lifecycle

        private void Start()
        {
            SubscribeToScore();
        }

        private void OnEnable()
        {
            SubscribeToScore();
        }

        private void OnDisable()
        {
            UnsubscribeFromScore();
        }

        private void OnDestroy()
        {
            UnsubscribeFromScore();
        }

        #endregion

        #region Scoring 

        private void OnScoreUpdated(int newScore)
        {
            _currentScore = newScore;
        }

        #endregion

        #region Triggers

        private void OnTriggerEnter(Collider other)
        {
            // Check for checkpoint triggers
            if(Tools.IsInLayerMask(other.gameObject.layer,_checkPointLayer))
            {
                _scoreAtLastCheckpoint = _currentScore;
            }

            // Check for score triggers
            if(other.TryGetComponent(out IScoreTrigger scoreTrigger))
            {
                _scoreValue.AddValue(scoreTrigger.ScoreValue);
            }
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    _scoreValue.SetValue(_scoreAtLastCheckpoint);
                    break;
            }
        }

        #endregion

        #region Subscriptions

        private void SubscribeToScore()
        {
            if (!_isSubscribedToScore && _scoreValue != null)
            {
                _scoreValue.SubscribeToValueChanged(OnScoreUpdated);
                _isSubscribedToScore = true;
            }
        }

        private void UnsubscribeFromScore()
        {
            if (_isSubscribedToScore && _scoreValue != null)
            {
                _scoreValue.UnsubscribeFromValueChanged(OnScoreUpdated);
                _isSubscribedToScore = false;
            }
        }

        #endregion
    }
}