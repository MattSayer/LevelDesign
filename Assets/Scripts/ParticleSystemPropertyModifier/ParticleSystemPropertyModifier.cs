using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.ParticleSystems
{
    public abstract class ParticleSystemPropertyModifier : ScriptableObject
    {
        public abstract object GetPropertyValue(ParticleSystem particleSystem);
        public abstract void SetPropertyValue(ParticleSystem particleSystem, object value);
    }
}