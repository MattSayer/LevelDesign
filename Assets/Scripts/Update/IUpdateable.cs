using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.UpdateLoop
{
    public interface IUpdateable
    {
        int ExecutionPriority();
        void ManagedUpdate(float deltaTime);
    }

    public interface IFixedUpdateable
    {
        int ExecutionPriority();
        void ManagedFixedUpdate(float deltaTime);
    }

    public interface ILateUpdateable
    {
        int ExecutionPriority();
        void ManagedLateUpdate(float deltaTime);
    }

}