using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    public interface ILevelStateListener
    {
        public void OnLevelStateChanged(LevelState levelState);
    }
    public enum LevelState
    {
        NotStarted,
        Started,
        CompletedSuccessfully,
        CompletedUnsuccessfully
    }
}