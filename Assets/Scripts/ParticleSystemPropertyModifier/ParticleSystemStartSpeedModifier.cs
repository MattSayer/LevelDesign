using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.ParticleSystems
{
    [CreateAssetMenu(menuName ="ParticleSystemPropertyModifiers/StartSpeedModifier", fileName ="ParticleSystemStartSpeedModifier")]
    public class ParticleSystemStartSpeedModifier : ParticleSystemPropertyModifier
    {
        public override object GetPropertyValue(ParticleSystem particleSystem)
        {
            return particleSystem.main.startSpeed.constant;
        }

        public override void SetPropertyValue(ParticleSystem particleSystem, object value)
        {
            if(value.GetType() == typeof(float) || value.GetType() == typeof(int))
            {
                ParticleSystem.MainModule main = particleSystem.main;
                ParticleSystem.MinMaxCurve startSpeedCurve = main.startSpeed;
                startSpeedCurve.constant = (float)value;
                main.startSpeed = startSpeedCurve;
            }
        }
    }
}