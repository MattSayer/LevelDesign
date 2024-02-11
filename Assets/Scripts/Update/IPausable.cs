using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.UpdateLoop
{
    public interface IPausable
    {
        public void Pause();
        public void Resume();
    }
}