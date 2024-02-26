using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Scoring
{
    public class ScoreTrigger : MonoBehaviour, IScoreTrigger
    {
        [SerializeField] private int _scoreValue;

        public int ScoreValue {  get { return _scoreValue; } }
    }

    public interface IScoreTrigger
    {
        public int ScoreValue { get; }
    }
}