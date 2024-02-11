using System.Collections;
using System.Collections.Generic;
using AmalgamGames.Visuals;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Core
{
    [RequireComponent(typeof(Rigidbody))]
    public class Respawner : MonoBehaviour
    {

        [Title("Collision")]
        [SerializeField] private float _alwaysCollideVelocity = 500f;
        [SerializeField] private float _minCollisionVelocity = 10f;
        [MinMaxSlider(0, 90)]
        [SerializeField] private Vector2 _minMaxCollisionAngle;
        [Space]
        [Title("Respawning")]
        [SerializeField] private float _respawnDelay = 2;
        [Space]
        [Title("Components")]
        [SerializeField] private GameObject _meshObject;
        [Title("Dependencies")]
        [SerializeField] private DependencyRequest _getVFXPlayer;
        [Space]
        [Title("DEBUG")]
        [SerializeField] private Transform _respawnPoint;

        // COMPONENTS
        private IInputProcessor _inputProcessor;
        private IRocketController _rocketController;
        private IVFXPlayer _vfxPlayer;

        // STATE
        private bool _isSubscribedToInput = false;
        private bool _canCollide = true;
        private Transform _lastCheckpoint;

        // COROUTINES
        private Coroutine _explodeRoutine = null;

        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(transform.parent);
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);
            SubscribeToInput();
            _lastCheckpoint = _respawnPoint;
            _getVFXPlayer.RequestDependency(ReceiveVFXPlayer);
        }

        private void OnDisable()
        {
            UnsubscribeFromInput();
        }

        private void OnDestroy()
        {
            UnsubscribeFromInput();
        }

        #endregion

        #region Dependencies

        private void ReceiveVFXPlayer(object rawObj)
        {
            _vfxPlayer = rawObj as IVFXPlayer;
        }

        #endregion

        #region Respawning

        private void OnRespawnInput()
        {
            Respawn();
        }

        private void Respawn()
        {
            // Fade screen down


            transform.position = _lastCheckpoint.position;
            transform.rotation = _lastCheckpoint.rotation;
            _canCollide = true;
            _rocketController.ToggleEnabled(true);

            // Fade screen back up


        }

        #endregion

        #region Input

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnRespawn -= OnRespawnInput;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnRespawn += OnRespawnInput;
                _isSubscribedToInput = true;
            }
        }



        #endregion

        #region Collision

        private void OnCollisionEnter(Collision collision)
        {
            if (_canCollide)
            {
                float currentVelocity = collision.relativeVelocity.magnitude;

                // If rocket is moving slower than min collision velocity, don't explode
                if(currentVelocity < _minCollisionVelocity)
                {
                    Debug.Log("Collision too slow: " + currentVelocity);
                    return;
                }
                // If rocket is moving faster than a maximum ceiling, any collision is fatal
                else if (currentVelocity >= _alwaysCollideVelocity)
                {
                    if (_explodeRoutine == null)
                    {
                        _explodeRoutine = StartCoroutine(explodeThenRespawn());
                    }
                }
                else
                {
                    float normalizedVelocity = currentVelocity / _alwaysCollideVelocity;

                    float angleThreshold = Mathf.Lerp(_minMaxCollisionAngle.x, _minMaxCollisionAngle.y, normalizedVelocity);
                    // Normalize
                    angleThreshold /= 90f;

                    // Check angle between collision normal and rocket forward
                    float collisionAngle = Mathf.Abs(Vector3.Dot(transform.forward, collision.contacts[0].normal));

                    // Reciprocal to align with angle threshold (higher dot product means perpendicular so should be lower value to compare against threshold
                    collisionAngle = 1 - collisionAngle;

                    // if collision angle reciprocal falls below angle threshold, fatal collision
                    if (collisionAngle < angleThreshold)
                    {
                        if(_explodeRoutine == null)
                        {
                            _explodeRoutine = StartCoroutine(explodeThenRespawn());
                        }
                    }
                    else
                    {
                        Debug.Log("Shallow collision");
                    }
                }
            }
            
        }

        private void Explode()
        {
            // Hide rocket mesh object
            _meshObject.SetActive(false);

            // Play explosion vfx

            Debug.Log("Boom!");

            _rocketController.ToggleEnabled(false);

            _canCollide = false;
        }

        #endregion

        #region Coroutines

        private IEnumerator explodeThenRespawn()
        {
            Explode();

            yield return new WaitForSeconds(_respawnDelay);

            // Re-enable mesh object
            _meshObject.SetActive(true);

            Respawn();

            _explodeRoutine = null;
        }

        #endregion
    }
}