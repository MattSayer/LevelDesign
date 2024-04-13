using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace AmalgamGames.Timing
{
    [CreateAssetMenu(menuName = "Timed Functions/FadeText", fileName ="FadeText_TimedFunction")]
    public class FadeTextTimedFunction : TimedFunction
    {
        #region Timed function

        public override async Task<System.Action> RunFunction(GameObject target, float duration)
        {
            TextMeshProUGUI text;
            target.TryGetComponent(out text);

            Color resetColour = text.color;
            System.Action undoFunction = null;
            
            if (text != null)
            {
                undoFunction = () => { text.color = resetColour; };
                await fadeText(text, duration);
            }
            return undoFunction;
        }

        private async Task fadeText(TextMeshProUGUI text, float duration)
        {
            float fadeLerp = 0;

            Color startColour = text.color;
            while(fadeLerp < duration)
            {
                float opacity = 1 - (fadeLerp / duration);
                startColour.a = opacity;
                text.color = startColour;
                fadeLerp += Time.deltaTime;
                await Task.Yield();
            }

            startColour.a = 0;
            text.color = startColour;
        }

        #endregion
    }
}