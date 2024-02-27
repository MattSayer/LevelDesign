using AmalgamGames.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Scoring
{
    public class ScoreTrigger : MonoBehaviour, IScoreTrigger, IRespawnable
    {
        [SerializeField] private int _scoreValue;

        private bool _hasTriggered = false;
        private bool _bankedHasTriggered = false;

        #region Scoring

        public int TakeScore()
        {
            if (_hasTriggered)
            {
                return 0;
            }

            _hasTriggered = true;
            return _scoreValue;
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    _hasTriggered = _bankedHasTriggered;
                    break;
                case RespawnEvent.OnCheckpoint:
                    _bankedHasTriggered = _hasTriggered;
                    break;
            }
        }

        #endregion
    }

    public interface IScoreTrigger
    {
        public int TakeScore();
    }
}