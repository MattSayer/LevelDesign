using AmalgamGames.Core;
using AmalgamGames.Effects;
using AmalgamGames.Utils;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Audio
{
    public class DynamicAudioTrigger : DynamicEventsEffect
    {
        [FoldoutGroup("Events")]
        [SerializeField] private DynamicEventsWithAudioRequest[] _dynamicEvents;
        [Space]
        [Title("Audio Manager")]
        [SerializeField] private DependencyRequest _getAudioManager;

        protected override DynamicEventsContainer[] DynamicEventsContainers => _dynamicEvents;

        private IAudioManager _audioManager;

        #region Lifecycle

        private void Start()
        {
            _getAudioManager.RequestDependency(ReceiveAudioManager);
        }

        #endregion

        #region Dependencies

        private void ReceiveAudioManager(object rawObj)
        {
            _audioManager = rawObj as IAudioManager;
        }

        #endregion

        #region Triggers

        protected override void OnTriggerEvent(DynamicEventsContainer sourceEvent)
        {
            DynamicEventsWithAudioRequest evt = (DynamicEventsWithAudioRequest)sourceEvent;

            PlayAudio(evt.AudioPlayRequest);
        }

        protected override void OnTriggerEventWithParam(DynamicEventsContainer sourceTimedEvent, DynamicEvent sourceEvent, object param)
        {
            DynamicEventsWithAudioRequest evt = (DynamicEventsWithAudioRequest)sourceTimedEvent;

            bool conditionalCheck = Tools.ApplyConditionals(param, sourceEvent.Conditionals);
            if (!conditionalCheck)
            {
                return;
            }

            if (!evt.UseEventParameter)
            {
                OnTriggerEvent(evt);
            }
            else
            {
                if (param.GetType() == typeof(AudioPlayRequest))
                {
                    evt.AudioPlayRequest = (AudioPlayRequest)param;
                    PlayAudio(evt.AudioPlayRequest);
                }
            }

        }

        #endregion

        #region Audio

        private void PlayAudio(AudioPlayRequest request)
        {
            if(_audioManager != null)
            {
                _audioManager.PlayAudioClip(request);
            }
        }

        #endregion

        [Serializable]
        private class DynamicEventsWithAudioRequest : DynamicEventsContainer
        {
            public AudioPlayRequest AudioPlayRequest;
        }
    }
    
}