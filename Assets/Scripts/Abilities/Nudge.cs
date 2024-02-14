using AmalgamGames.Core;
using AmalgamGames.UpdateLoop;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace AmalgamGames.Abilities
{
    public class Nudge : ManagedFixedBehaviour, IRespawnable
    {

        [Title("Settings")]
        [SerializeField] private float _nudgeForce = 1;


        // STATE

        // Subscriptions
        private bool _isSubscribedToInput = false;
        private bool _isSubscribedToRocket = false;

        // Nudging
        private bool _canNudge = false;
        private Vector2 _nudgeDirection = Vector2.zero;

        // COMPONENTS
        private IInputProcessor _inputProcessor;
        private IRocketController _rocketController;
        private Rigidbody _rb;


        #region Lifecycle

        private void Start()
        {
            _inputProcessor = Tools.GetFirstComponentInHierarchy<IInputProcessor>(transform.parent);
            _rocketController = Tools.GetFirstComponentInHierarchy<IRocketController>(transform.parent);

            SubscribeToInput();
            SubscribeToRocket();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            SubscribeToInput();
            SubscribeToRocket();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UnsubscribeFromInput();
            UnsubscribeFromRocket();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            UnsubscribeFromInput();
            UnsubscribeFromRocket();
        }

        public override void ManagedFixedUpdate(float deltaTime)
        {
            if(_canNudge && _nudgeDirection != Vector2.zero)
            {
                Vector3 nudgeForce = (_nudgeDirection.x * transform.right) + (_nudgeDirection.y * Vector3.up);

                _rb.AddForce(nudgeForce * _nudgeForce * deltaTime, ForceMode.Force);
            }
        }

        #endregion

        #region Nudging

        private void OnNudge(Vector2 nudgeDelta)
        {
            _nudgeDirection = nudgeDelta;
        }

        #endregion

        #region Charging

        private void OnChargingStart(ChargingType chargingType)
        {
            _canNudge = false;
        }

        private void OnBurnComplete()
        {
            _canNudge = true;
        }

        #endregion

        #region Respawning/Restarting

        public void OnRespawnEvent(RespawnEvent evt)
        {
            switch(evt)
            {
                case RespawnEvent.OnCollision:
                case RespawnEvent.OnRespawnStart:
                    _canNudge = false;
                    break;
                case RespawnEvent.OnRespawnEnd:
                    _canNudge = true;
                    break;
            }
        }

        #endregion

        #region Subscriptions

        private void UnsubscribeFromInput()
        {
            if (_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnNudgeInputChange -= OnNudge;
                _isSubscribedToInput = false;
            }
        }

        private void SubscribeToInput()
        {
            if (!_isSubscribedToInput && _inputProcessor != null)
            {
                _inputProcessor.OnNudgeInputChange += OnNudge;
                _isSubscribedToInput = true;
            }
        }

        private void SubscribeToRocket()
        {
            if(!_isSubscribedToRocket && _rocketController != null)
            {
                _rocketController.OnChargingStart += OnChargingStart;
                _rocketController.OnBurnComplete += OnBurnComplete;
            }
        }

        private void UnsubscribeFromRocket()
        {
            if (!_isSubscribedToRocket && _rocketController != null)
            {
                _rocketController.OnChargingStart -= OnChargingStart;
                _rocketController.OnBurnComplete -= OnBurnComplete;
            }
        }

        #endregion
    }
}