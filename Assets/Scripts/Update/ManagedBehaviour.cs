using UnityEngine;

namespace AmalgamGames.UpdateLoop
{
    public abstract class ManagedBehaviour : MonoBehaviour, IUpdateable
    {
        public abstract void ManagedUpdate(float deltaTime);

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