using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class ObjectSpawner : MonoBehaviour, ISpawner, ITriggerable, IRespawnable
    {
        [Title("Spawning")]
        [SerializeField] private GameObject _spawnPrefab;
        [SerializeField] private float _spawnRatePerSecond = 1;
        [SerializeField] private bool _spawnOnStart = true;
        [SerializeField] private float _spawnDelay = 0;
        [SerializeField] private Transform _spawnPoint;
        [Title("Pooling")]
        [SerializeField] private int _poolSize = 5;
        [SerializeField] private EmptyPoolAction _emptyPoolAction = EmptyPoolAction.RecycleOldest;

        // Coroutines
        private Coroutine _spawnRoutine = null;
        private Coroutine _activationRoutine = null;

        private List<GameObject> _activeObjects;
        private List<GameObject> _inactiveObjects;

        // STATE

        #region Lifecycle

        private void Awake()
        {
            CreatePool();
        }

        private void Start()
        {
            if (_spawnOnStart && _activationRoutine == null && _spawnRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_spawnDelay,() =>
                {
                    _spawnRoutine = StartCoroutine(spawnNewObject());
                    _activationRoutine = null;
                }));
            }
        }

        #endregion

        #region Respawning

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnRespawnStart:
                    DespawnAllActiveObjects();
                    break;
            }
        }

        private void DespawnAllActiveObjects()
        {
            if (_spawnRoutine != null)
            {
                StopCoroutine(_spawnRoutine);
                _spawnRoutine = null;
            }

            if(_activationRoutine != null)
            {
                StopCoroutine(_activationRoutine);
                _activationRoutine = null;
            }

            foreach (GameObject obj in _activeObjects.ToList())
            {
                _activeObjects.Remove(obj);
                if (obj.TryGetComponent(out ISpawnable spawnable))
                {
                    spawnable.DeactivateAndReset();
                }
                obj.SetActive(false);
                _inactiveObjects.Add(obj);
            }

            if (_spawnOnStart && _activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_spawnDelay, () =>
                {
                    _spawnRoutine = StartCoroutine(spawnNewObject());
                    _activationRoutine = null;
                }));
            }
        }

        #endregion

        #region Pooling

        private void CreatePool()
        {
            _activeObjects = new List<GameObject>();
            _inactiveObjects = new List<GameObject>();

            for(int i = 0; i < _poolSize; i++)
            {
                GameObject newObj = Instantiate(_spawnPrefab);
                newObj.name += "_" + i;
                newObj.transform.SetParent(transform);
                newObj.SetActive(false);

                _inactiveObjects.Add(newObj);
            }

            

        }

        private GameObject GetNewObject()
        {
            if(_inactiveObjects.Count > 0)
            {
                GameObject newObj = _inactiveObjects[0];
                _inactiveObjects.RemoveAt(0);
                return newObj;
            }

            switch(_emptyPoolAction)
            {
                case EmptyPoolAction.RecycleOldest:
                    GameObject newObj = _activeObjects[0];
                    _activeObjects.RemoveAt(0);
                    return newObj;
                case EmptyPoolAction.InstantiateNew:
                    GameObject newObject = Instantiate(_spawnPrefab);
                    newObject.name += "_" + _poolSize;
                    newObject.transform.SetParent(transform);
                    _poolSize++;
                    return newObject;
                default:
                case EmptyPoolAction.StopSpawning:
                    return null;
            }
        }

        public void Despawn(GameObject spawnableObj)
        {
            if(_activeObjects.Contains(spawnableObj))
            {
                _activeObjects.Remove(spawnableObj);
                if(spawnableObj.TryGetComponent(out ISpawnable spawnable))
                {
                    spawnable.DeactivateAndReset();
                }
                spawnableObj.SetActive(false);
                _inactiveObjects.Add(spawnableObj);
            }
        }

        #endregion

        #region Triggers

        public void Trigger()
        {
            if(_spawnRoutine == null && _activationRoutine == null)
            {
                _activationRoutine = StartCoroutine(Tools.delayThenAction(_spawnDelay, () =>
                {
                    _spawnRoutine = StartCoroutine(spawnNewObject());
                    _activationRoutine = null;
                }));
            }
        }

        #endregion

        #region Spawnable initialisation

        protected virtual void InitialiseSpawnable(GameObject spawnableObj)
        {
            // For overriding in subclasses that need to perform extra steps in initialising a spawnable
        }

        #endregion

        #region Coroutines

        private IEnumerator spawnNewObject()
        {
            GameObject newObj = GetNewObject();

            if(newObj)
            {
                _activeObjects.Add(newObj);
                ISpawnable spawnable;
                if(newObj.TryGetComponent(out spawnable))
                {
                    spawnable.DeactivateAndReset();
                    spawnable.SetSpawner(this);
                }

                InitialiseSpawnable(newObj);

                newObj.transform.position = _spawnPoint.position;
                newObj.transform.rotation = _spawnPoint.rotation;
                newObj.SetActive(true);
                if (spawnable != null)
                {
                    spawnable.Activate();
                }
            }
            else
            {
                yield break;
            }
            yield return new WaitForSeconds(1f / _spawnRatePerSecond);

            _spawnRoutine = StartCoroutine(spawnNewObject());
        }

        #endregion

        
    }

    public enum EmptyPoolAction
    {
        InstantiateNew,
        RecycleOldest,
        StopSpawning
    }

    public interface ISpawnable
    {
        public void DeactivateAndReset();
        public void Activate();
        public void SetSpawner(ISpawner spawner);
    }

    public interface ISpawner
    {
        public void Despawn(GameObject spawnable);
    }


}