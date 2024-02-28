using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Config
{
    [CreateAssetMenu(menuName ="Config/RocketConfig", fileName ="RocketConfig")]
    public class RocketConfig : ScriptableObject
    {
        [Title("Charging")]
        [SerializeField] private float _chargeDeltaThreshold = 0.1f;
        [SerializeField] private float _playerChargeForce;
        [SerializeField] private float _minChargeForce;
        [Space]
        [Title("Engine Burn")]
        [SerializeField] private float _engineBurnTime = 2f;
        [SerializeField] private float _minEngineBurnTime = 1f;
        [SerializeField] private float _engineBurnForce = 10f;

        public float ChargeDeltaThreshold { get { return _chargeDeltaThreshold; } }
        public float PlayerChargeForce {  get { return _playerChargeForce; } }  
        public float MinChargeForce {  get { return _minChargeForce; } }
        public float EngineBurnTime {  get { return _engineBurnTime; } }    
        public float EngineBurnForce {  get { return _engineBurnForce; } }

        public float MinEngineBurnTime { get { return _minEngineBurnTime; } }
    }
}