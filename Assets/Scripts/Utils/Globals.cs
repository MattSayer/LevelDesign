using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace AmalgamGames.Utils
{
    public static class Globals
    {
        // CONSTANTS

        #region Events

        // Rocket Controller Events
        public const string VELOCITY_CHANGED_KEY = "velocity";
        public const string CHARGE_LEVEL_CHANGED_KEY = "charge_level";

        // Juice events
        public const string JUICE_LEVEL_CHANGED_KEY = "juice_level";

        // Nudge events
        public const string NUDGE_DIRECTION_CHANGED_KEY = "nudge_direction";

        // Countdown events
        public const string COUNTDOWN_CHANGED_KEY = "countdown";

        #endregion

        #region Shaders

        // Shaders
        public const string MAIN_COLOUR_KEY = "_MainColour";
        public const string USE_EXTERNAL_TIME_KEY = "_UseExternalTime";
        public const string EXTERNAL_TIME_KEY = "_ExternalTime";

        #endregion

        #region PlayerPrefs

        // Audio
        public const string MASTER_VOLUME_KEY = "master_volume";
        public const string EFFECTS_VOLUME_KEY = "effects_volume";
        public const string UI_VOLUME_KEY = "ui_volume";
        public const string MUSIC_VOLUME_KEY = "music_volume";

        // Visuals
        public const string SCREEN_SHAKE_AMOUNT_KEY = "screen_shake_amount";


        #endregion

        #region Miscellaneous

        // FPS
        public const string FPS_KEY = "fps";

        #endregion

        #region Collision

        // Tags
        public const string FATAL_COLLIDER_TAG = "FatalCollider";

        // Layers
        public const int CHECKPOINT_LAYER = 8;

        #endregion

        #region Animation

        // Animation
        public const string ANIMATION_START_TRIGGER = "Start";
        public const string ANIMATION_STOP_TRIGGER = "Stop";

        #endregion
    }
}