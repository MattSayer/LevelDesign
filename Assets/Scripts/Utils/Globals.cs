using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Utils
{
    public static class Globals
    {
        // CONSTANTS

        // Rocket Controller Events
        public const string VELOCITY_CHANGED_KEY = "velocity";
        public const string CHARGE_LEVEL_CHANGED_KEY = "charge_level";

        // Juice events
        public const string JUICE_LEVEL_CHANGED_KEY = "juice_level";

        // Nudge events
        public const string NUDGE_DIRECTION_CHANGED_KEY = "nudge_direction";

        // Shaders
        public const string MAIN_COLOUR_KEY = "_MainColour";
        public const string USE_EXTERNAL_TIME_KEY = "_UseExternalTime";
        public const string EXTERNAL_TIME_KEY = "_ExternalTime";

        // FPS
        public const string FPS_KEY = "fps";

        // Tags
        public const string FATAL_COLLIDER_TAG = "FatalCollider";

        // Layers
        public const int CHECKPOINT_LAYER = 8;
    }
}