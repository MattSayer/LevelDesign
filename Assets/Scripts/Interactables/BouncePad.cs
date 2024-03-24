using AmalgamGames.Control;
using AmalgamGames.Core;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEngine;

namespace AmalgamGames.Interactables
{
    [RequireComponent(typeof(Collider))]
    public class BouncePad : Interactable
    {
        [Title("Bounce")]
        [SerializeField] private float _bounceForce = 50f;
        [Space]
        [Title("Animation")]
        [SerializeField] private Animator _animator;
        [SerializeField] private string _animationID;

        // Components
        private Transform _playerRoot;
        private Rigidbody _rb;

        private int ANIM_HASH;

        #region Lifecycle

        private void Start()
        {
            ANIM_HASH = Animator.StringToHash(_animationID);
        }

        #endregion

        #region Interaction

        protected override void OnInteract(GameObject other)
        {
            _playerRoot = other.transform;

            _rb = other.GetComponent<Rigidbody>();

            // Cache then kill rocket velocity

            float velocityMagnitude = _rb.velocity.magnitude;

            KillRocketVelocity();

            // Realign rocket to bounce pad forward

            _playerRoot.forward = transform.forward;

            // Set rocket velocity to cached magnitude in new forward direction

            _rb.velocity = transform.forward * velocityMagnitude;

            // Apply impulse force 

            _rb.AddForce(transform.forward * _bounceForce, ForceMode.Impulse);

            // Play animation if present
            if(_animator != null && _animationID != "")
            {
                _animator.Play(ANIM_HASH);
            }

        }

        #endregion

        #region Helpers

        private void KillRocketVelocity()
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        #endregion


    }
}