using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmalgamGames.UpdateLoop
{

    public class ManagedUpdater : MonoBehaviour
    {
        private static ManagedUpdater instance;

        public static ManagedUpdater Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<ManagedUpdater>();
                }
                return instance;
            }
        }

        // Update queues
        private List<IUpdateable> _updateQueue = new List<IUpdateable>();
        private List<IFixedUpdateable> _fixedUpdateQueue = new List<IFixedUpdateable>();
        private List<ILateUpdateable> _lateUpdateQueue = new List<ILateUpdateable>();

        // Removal queues
        private List<IUpdateable> _updateRemovalQueue = new List<IUpdateable>();
        private List<IFixedUpdateable> _fixedUpdateRemovalQueue = new List<IFixedUpdateable>();
        private List<ILateUpdateable> _lateUpdateRemovalQueue = new List<ILateUpdateable>();

        // Add queues
        private List<IUpdateable> _updateAddQueue = new List<IUpdateable>();
        private List<IFixedUpdateable> _fixedUpdateAddQueue = new List<IFixedUpdateable>();
        private List<ILateUpdateable> _lateUpdateAddQueue = new List<ILateUpdateable>();

        #region Lifecycle

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }

            DontDestroyOnLoad(this);
        }

        void Update()
        {
            int addQueueSize = _updateAddQueue.Count;
            bool hasChanged = false;
            if (addQueueSize > 0)
            {
                // Reverse iteration to allow for removal of items from add queue
                for (int i = addQueueSize - 1; i >= 0; i--)
                {
                    _updateQueue.Add(_updateAddQueue[i]);
                    _updateAddQueue.Remove(_updateAddQueue[i]);
                }
                hasChanged = true;
            }
            int removeQueueSize = _updateRemovalQueue.Count;
            if (removeQueueSize > 0)
            {
                for (int i = removeQueueSize - 1; i >= 0; i--)
                {
                    _updateQueue.Remove(_updateRemovalQueue[i]);
                    _updateRemovalQueue.Remove(_updateRemovalQueue[i]);
                }
                hasChanged = true;
            }

            if (hasChanged)
            {
                PrioritySort(_updateQueue);
            }

            float deltaTime = Time.deltaTime;
            foreach (IUpdateable script in _updateQueue)
            {
                script.ManagedUpdate(deltaTime);
            }

        }

        private void FixedUpdate()
        {
            int addQueueSize = _fixedUpdateAddQueue.Count;
            bool hasChanged = false;
            if (addQueueSize > 0)
            {
                // Reverse iteration to allow for removal of items from add queue
                for (int i = addQueueSize - 1; i >= 0; i--)
                {
                    _fixedUpdateQueue.Add(_fixedUpdateAddQueue[i]);
                    _fixedUpdateAddQueue.Remove(_fixedUpdateAddQueue[i]);
                }
                hasChanged = true;
            }
            int removeQueueSize = _fixedUpdateRemovalQueue.Count;
            if (removeQueueSize > 0)
            {
                for (int i = removeQueueSize - 1; i >= 0; i--)
                {
                    _fixedUpdateQueue.Remove(_fixedUpdateRemovalQueue[i]);
                    _fixedUpdateRemovalQueue.Remove(_fixedUpdateRemovalQueue[i]);
                }
                hasChanged = true;
            }

            if (hasChanged)
            {
                PrioritySort(_fixedUpdateQueue);
            }

            float deltaTime = Time.deltaTime;
            foreach (IFixedUpdateable script in _fixedUpdateQueue)
            {
                script.ManagedFixedUpdate(deltaTime);
            }
        }

        private void LateUpdate()
        {
            int addQueueSize = _lateUpdateAddQueue.Count;
            bool hasChanged = false;
            if (addQueueSize > 0)
            {
                // Reverse iteration to allow for removal of items from add queue
                for (int i = addQueueSize - 1; i >= 0; i--)
                {
                    _lateUpdateQueue.Add(_lateUpdateAddQueue[i]);
                    _lateUpdateAddQueue.Remove(_lateUpdateAddQueue[i]);
                }
                hasChanged = true;
            }
            int removeQueueSize = _lateUpdateRemovalQueue.Count;
            if (removeQueueSize > 0)
            {
                for (int i = removeQueueSize - 1; i >= 0; i--)
                {
                    _lateUpdateQueue.Remove(_lateUpdateRemovalQueue[i]);
                    _lateUpdateRemovalQueue.Remove(_lateUpdateRemovalQueue[i]);
                }
                hasChanged = true;
            }

            if (hasChanged)
            {
                PrioritySort(_lateUpdateQueue);
            }

            float deltaTime = Time.deltaTime;
            foreach (ILateUpdateable script in _lateUpdateQueue)
            {
                script.ManagedLateUpdate(deltaTime);
            }
        }

        #endregion

        #region Utility

        private void PrioritySort(List<IUpdateable> scripts)
        {
            scripts.Sort((x, y) => x.ExecutionPriority().CompareTo(y.ExecutionPriority()));
        }
        private void PrioritySort(List<IFixedUpdateable> scripts)
        {
            scripts.Sort((x, y) => x.ExecutionPriority().CompareTo(y.ExecutionPriority()));
        }
        private void PrioritySort(List<ILateUpdateable> scripts)
        {
            scripts.Sort((x, y) => x.ExecutionPriority().CompareTo(y.ExecutionPriority()));
        }

        #endregion

        #region Public

        public void RegisterUpdate(IUpdateable script)
        {
            // In case the script is still queued up for removal
            if (_updateRemovalQueue.Contains(script))
            {
                _updateRemovalQueue.Remove(script);
            }
            else
            {
                // Make sure script hasn't already been registered
                if (!_updateQueue.Contains(script) && !_updateAddQueue.Contains(script))
                {
                    _updateAddQueue.Add(script);
                }
            }
        }

        public void RegisterUpdate(IFixedUpdateable script)
        {
            // In case the script is still queued up for removal
            if (_fixedUpdateRemovalQueue.Contains(script))
            {
                _fixedUpdateRemovalQueue.Remove(script);
            }
            else
            {
                // Make sure script hasn't already been registered
                if (!_fixedUpdateQueue.Contains(script) && !_fixedUpdateAddQueue.Contains(script))
                {
                    _fixedUpdateAddQueue.Add(script);
                }
            }
        }

        public void RegisterUpdate(ILateUpdateable script)
        {
            // In case the script is still queued up for removal
            if (_lateUpdateRemovalQueue.Contains(script))
            {
                _lateUpdateRemovalQueue.Remove(script);
            }
            else
            {
                // Make sure script hasn't already been registered
                if (!_lateUpdateQueue.Contains(script) && !_lateUpdateAddQueue.Contains(script))
                {
                    _lateUpdateAddQueue.Add(script);
                }
            }
        }

        public void UnregisterUpdate(IUpdateable script)
        {
            // In case script hasn't been added yet (e.g. when register and unregister are called in the same frame)
            if (_updateAddQueue.Contains(script))
            {
                _updateAddQueue.Remove(script);
            }
            else
            {
                _updateRemovalQueue.Add(script);
            }
        }

        public void UnregisterUpdate(IFixedUpdateable script)
        {
            if (_fixedUpdateAddQueue.Contains(script))
            {
                _fixedUpdateAddQueue.Remove(script);
            }
            else
            {
                _fixedUpdateRemovalQueue.Add(script);
            }
        }

        public void UnregisterUpdate(ILateUpdateable script)
        {
            if (_lateUpdateAddQueue.Contains(script))
            {
                _lateUpdateAddQueue.Remove(script);
            }
            else
            {
                _lateUpdateRemovalQueue.Add(script);
            }
        }

        #endregion
    }
}