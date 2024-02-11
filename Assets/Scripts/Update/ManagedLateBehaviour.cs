using UnityEngine;

namespace AmalgamGames.UpdateLoop
{
    public abstract class ManagedLateBehaviour : MonoBehaviour, ILateUpdateable
    {
        public abstract void ManagedLateUpdate(float deltaTime);
        public virtual int ExecutionPriority() { return 0; }

        #region ManagedUpdater
        protected virtual void OnEnable()
        {
            ManagedUpdater.Instance.RegisterUpdate(this);
        }

        protected virtual void OnDisable()
        {
            if (ManagedUpdater.Instance != null)
            {
                ManagedUpdater.Instance.UnregisterUpdate(this);
            }
        }

        protected virtual void OnDestroy()
        {
            if (ManagedUpdater.Instance != null)
            {
                ManagedUpdater.Instance.UnregisterUpdate(this);
            }
        }
        #endregion
    }
}