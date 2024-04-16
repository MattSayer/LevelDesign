using AmalgamGames.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Transformation
{
    [CreateAssetMenu(menuName = "Transformations/Extractors/LaunchInfoComponent", fileName = "ExtractLaunchInfoComponentTransformation")]
    public class ExtractLaunchInfoComponentTransformation : Transformation
    {
        [SerializeField] private LaunchInfoComponent _component;

        public override object TransformInput(object input)
        {
            if(input.GetType() == typeof(LaunchInfo))
            {
                LaunchInfo launchInfo = (LaunchInfo)input;
                switch(_component)
                {
                    case LaunchInfoComponent.BurnDuration:
                        return launchInfo.BurnDuration;
                    case LaunchInfoComponent.ChargeLevel:
                        return launchInfo.ChargeLevel;
                }
            }

            return input;
        }

        [Serializable]
        private enum LaunchInfoComponent
        {
            ChargeLevel,
            BurnDuration
        }
    }
}