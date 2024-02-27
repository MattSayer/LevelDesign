using AmalgamGames.Conditionals;
using AmalgamGames.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace AmalgamGames.Utils
{
    public static class Tools
    {

        #region Angles

        /// <summary>
        /// Lerps between two vectors, with each component of the vectors treated as a 360-degree angle. This prevents issues when wrapping around from 360 degrees back to 0
        /// </summary>
        /// <param name="startAngle">The start vector</param>
        /// <param name="endAngle">The target end vector</param>
        /// <param name="time">The current 0-1 value for lerping</param>
        /// <returns></returns>
        public static Vector3 AngleLerp(Vector3 startAngle, Vector3 endAngle, float time)
        {
            float xLerp = Mathf.LerpAngle(startAngle.x, endAngle.x, time);
            float yLerp = Mathf.LerpAngle(startAngle.y, endAngle.y, time);
            float zLerp = Mathf.LerpAngle(startAngle.z, endAngle.z, time);
            Vector3 lerpedAngle = new Vector3(xLerp, yLerp, zLerp);
            return lerpedAngle;
        }

        /// <summary>
        /// Moves one vector towards another by way of the provided delta. This method treats each vector component as an angle in order to handle wrapping from 360 degrees back to 0
        /// </summary>
        /// <param name="startAngle">The start vector</param>
        /// <param name="endAngle">The target end vector</param>
        /// <param name="maxDelta">The maximum delta of change</param>
        /// <returns></returns>
        public static Vector3 MoveTowardsAngle(Vector3 startAngle, Vector3 endAngle, float maxDelta)
        {
            float x = Mathf.MoveTowardsAngle(startAngle.x, endAngle.x, maxDelta);
            float y = Mathf.MoveTowardsAngle(startAngle.y, endAngle.y, maxDelta);
            float z = Mathf.MoveTowardsAngle(startAngle.z, endAngle.z, maxDelta);
            return new Vector3(x, y, z);
        }

        #endregion

        #region Coordinate space

        /// <summary>
        /// Converts the provided TransformAxis from the local space of the provided transform to world space
        /// </summary>
        /// <param name="transform">The transform representing the local space from which the TransformAxis will be converted</param>
        /// <param name="axis">The axis, interpreted as local space to the provided transform, which will be converted into world space</param>
        /// <returns></returns>
        public static Vector3 TranslateAxisInLocalSpace(Transform transform, TransformAxis axis)
        {
            return transform.TransformDirection(TranslateAxisInWorldSpace(axis));
        }

        /// <summary>
        /// Converts the provided TransformAxis to its world space representation (e.g. Z_pos will return Vector3.forward)
        /// </summary>
        /// <param name="axis">The axis to convert</param>
        /// <returns></returns>
        public static Vector3 TranslateAxisInWorldSpace(TransformAxis axis)
        {
            switch(axis)
            {
                case TransformAxis.X_pos:
                    return Vector3.right;
                case TransformAxis.Y_pos: 
                    return Vector3.up;
                case TransformAxis.Z_pos:
                    return Vector3.forward;
                case TransformAxis.X_neg:
                    return Vector3.left;
                case TransformAxis.Y_neg:
                    return Vector3.down;
                case TransformAxis.Z_neg:
                    return Vector3.back;
            }
            return Vector3.zero;
        }

        #endregion


        #region Layers and flags

        /// <summary>
        /// Checks whether the provided layer is contained within the provided layer mask
        /// </summary>
        /// <param name="layer">The layer to check</param>
        /// <param name="mask">The layer mask to search</param>
        /// <returns></returns>
        public static bool IsInLayerMask(int layer, LayerMask mask)
        {
            return mask == (mask | (1 << layer));
        }


        /// <summary>
        /// Checks whether the flag enum value is present in the flags enum bitmask
        /// Along with setting the [System.Flags] semantic above the enum declaration,
        /// you'll need to set the enum values to match the correct binary flags,
        /// e.g. 00 = 0, 01 = 1, 10 = 2, 100 = 4, etc...
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="flags"></param>
        /// <param name="flag"></param>
        /// <returns></returns>
        public static bool IsFlagSet<T>(T flags, T flag) where T : struct
        {
            // Convert T to int to allow for bitmask comparison
            int allFlags = (int)(object)flags;
            int flagToCheck = (int)(object)flag;

            // If the bitmask check doesn't resolve to 0, flags contains flag
            return (allFlags & flagToCheck) != 0;
        }

        #endregion

        #region Hierarchy traversal

        /// <summary>
        /// Gets the closest component T in the target transform's parent hierarcy
        /// </summary>
        /// <typeparam name="T">Component type to look for</typeparam>
        /// <param name="t">Target transform to search</param>
        /// <returns>Closest component T in parent hierarchy, or default(T) if not found</returns>
        public static T GetClosestParentComponent<T>(Transform t)
        {
            if (t != null)
            {
                T comp;
                t.TryGetComponent(out comp);

                if (comp != null)
                {
                    return comp;
                }
                else
                {
                    return GetClosestParentComponent<T>(t.parent);
                }
            }
            else
            {
                return default(T);
            }
        }


        /// <summary>
        /// Gets the first component of type T in the hierarchy starting with the specified root transform
        /// Uses a breadth-first search, so if there are multiple components of type T, the highest on in 
        /// the hierarchy will be returned
        /// </summary>
        /// <typeparam name="T">The component type to search for</typeparam>
        /// <param name="root">The root transform to start the search at</param>
        /// <returns>The first component of type T in the hierarchy, or default(T) if not found</returns>
        public static T GetFirstComponentInHierarchy<T>(Transform root)
        {
            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(root);

            while(queue.Count > 0) 
            {
                Transform t = queue.Dequeue();
                T comp = t.GetComponent<T>();
                if(comp != null)
                {
                    return comp;
                }
                else
                {
                    foreach(Transform child in t)
                    {
                        queue.Enqueue(child);
                    }
                }
            }

            return default(T);
        }

        #endregion

        #region Lerping

        /// <summary>
        /// Coroutine that lerps a float from the specified 'from' value to the 'to' value over 'duration'. 
        /// The returnSetter function is called on every frame passing in the current value. This function
        /// should be used to update the source value. 
        /// You can also provide a callback function for when the lerp has completed, as well as specify an easing
        /// function for the lerp.
        /// </summary>
        /// <param name="from">The value to lerp form</param>
        /// <param name="to">The value to lerp to</param>
        /// <param name="duration">The duration of the lerp</param>
        /// <param name="returnSetter">Function that gets passed the updated float value every frame</param>
        /// <param name="onComplete">Optional function that gets called once the lerp has completed</param>
        /// <param name="easingFunction">The easing to apply to the lerp</param>
        /// <returns></returns>
        public static IEnumerator lerpFloatOverTime(float from, float to, float duration, Action<float> returnSetter, Action onComplete = null, EasingFunction.Ease easingFunction = EasingFunction.Ease.Linear)
        {
            float time = 0;
            EasingFunction.Function func = EasingFunction.GetEasingFunction(easingFunction);
            float val;
            while(time < duration)
            {
                val = func(from, to, time / duration);
                returnSetter(val);
                time += Time.deltaTime;
                yield return null;
            }
            returnSetter(to);
            onComplete?.Invoke();
        }


        /// <summary>
        /// Coroutine that lerps a float from the specified 'from' value to the 'to' value over 'duration', using unscaled delta time. 
        /// The returnSetter function is called on every frame passing in the current value. This function
        /// should be used to update the source value. 
        /// You can also provide a callback function for when the lerp has completed, as well as specify an easing
        /// function for the lerp.
        /// </summary>
        /// <param name="from">The value to lerp form</param>
        /// <param name="to">The value to lerp to</param>
        /// <param name="duration">The duration of the lerp</param>
        /// <param name="returnSetter">Function that gets passed the updated float value every frame</param>
        /// <param name="onComplete">Optional function that gets called once the lerp has completed</param>
        /// <param name="easingFunction">The easing to apply to the lerp</param>
        /// <returns></returns>
        public static IEnumerator lerpFloatOverTimeUnscaled(float from, float to, float duration, Action<float> returnSetter, Action onComplete = null, EasingFunction.Ease easingFunction = EasingFunction.Ease.Linear)
        {
            float time = 0;
            EasingFunction.Function func = EasingFunction.GetEasingFunction(easingFunction);
            float val;
            while (time < duration)
            {
                val = func(from, to, time / duration);
                returnSetter(val);
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            returnSetter(to);
            onComplete?.Invoke();
        }


        /// <summary>
        /// Coroutine that lerps a Vector3 from the specified 'from' value to the 'to' value over 'duration'. 
        /// The returnSetter function is called on every frame passing in the current value. This function
        /// should be used to update the source value. 
        /// You can also provide a callback function for when the lerp has completed.
        /// </summary>
        /// <param name="from">The value to lerp form</param>
        /// <param name="to">The value to lerp to</param>
        /// <param name="duration">The duration of the lerp</param>
        /// <param name="returnSetter">Function that gets passed the updated float value every frame</param>
        /// <param name="onComplete">Optional function that gets called once the lerp has completed</param>
        /// <returns></returns>
        public static IEnumerator lerpVector3OverTime(Vector3 from, Vector3 to, float duration, Action<Vector3> returnSetter, Action onComplete = null)
        {
            float time = 0;
            Vector3 val;
            while (time < duration)
            {
                val = Vector3.Lerp(from, to, time / duration);
                returnSetter(val);
                time += Time.deltaTime;
                yield return null;
            }
            returnSetter(to);
            onComplete?.Invoke();
        }

        /// <summary>
        /// Coroutine that lerps a Vector3 from the specified 'from' value to the 'to' value over 'duration', using unscaled delta time. 
        /// The returnSetter function is called on every frame passing in the current value. This function
        /// should be used to update the source value. 
        /// You can also provide a callback function for when the lerp has completed.
        /// </summary>
        /// <param name="from">The value to lerp form</param>
        /// <param name="to">The value to lerp to</param>
        /// <param name="duration">The duration of the lerp</param>
        /// <param name="returnSetter">Function that gets passed the updated float value every frame</param>
        /// <param name="onComplete">Optional function that gets called once the lerp has completed</param>
        /// <returns></returns>
        public static IEnumerator lerpVector3OverTimeUnscaled(Vector3 from, Vector3 to, float duration, Action<Vector3> returnSetter, Action onComplete = null)
        {
            float time = 0;
            Vector3 val;
            while (time < duration)
            {
                val = Vector3.Lerp(from, to, time / duration);
                returnSetter(val);
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            returnSetter(to);
            onComplete?.Invoke();
        }

        #endregion

        #region Events



        /// <summary>
        /// Dynamically wires up the specified method on the caller object to execute when the 
        /// specified event is fired on the source object
        /// </summary>
        /// <param name="source">The object housing the event to subscribe to</param>
        /// <param name="sourceEventName">The name of the event to subscribe to</param>
        /// <param name="caller">The calling object containing the method to run when the event fires</param>
        /// <param name="callerMethodName">The name of the method to run</param>
        /// <returns>The delegate handling the event, for use in unsubscribing</returns>
        public static Delegate WireUpEvent(object source, string sourceEventName, object caller, string callerMethodName)
        {
#nullable enable
            EventInfo? eventInfo = source.GetType().GetEvent(sourceEventName);

            MethodInfo? methodInfo = caller.GetType().GetMethod(callerMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            Delegate handler = Delegate.CreateDelegate(eventInfo?.EventHandlerType, caller, methodInfo);
#nullable disable
            eventInfo.AddEventHandler(source, handler);

            return handler;
        }

        public static Delegate WireUpEvent(object source, string sourceEventName, object caller, MethodInfo methodInfo)
        {
#nullable enable
            EventInfo? eventInfo = source.GetType().GetEvent(sourceEventName);

            Delegate handler = Delegate.CreateDelegate(eventInfo?.EventHandlerType, caller, methodInfo);
#nullable disable
            eventInfo.AddEventHandler(source, handler);

            return handler;
        }



        /// <summary>
        /// Disconnects the provided delegate from the 'sourceEventName' event on the 'source' object
        /// </summary>
        /// <param name="source">The object housing the event to unsubscribe from</param>
        /// <param name="sourceEventName">The name of the event to unsubscribe from</param>
        /// <param name="handler">The delegate hooked up to the event</param>
        public static void DisconnectEvent(object source, string sourceEventName, Delegate handler)
        {
#nullable enable
            EventInfo? eventInfo = source.GetType().GetEvent(sourceEventName);
#nullable disable
            eventInfo.RemoveEventHandler(source, handler);
        }


        #endregion

        #region Coroutines

        public static IEnumerator delayThenAction(float duration, Action action)
        {
            yield return new WaitForSeconds(duration);
            action?.Invoke();
        }

        #endregion


    }

    public enum TransformAxis
    {
        X_pos,
        Y_pos,
        Z_pos,
        X_neg,
        Y_neg,
        Z_neg
    }

    [Serializable]
    public class DynamicEvent
    {
        public Component EventSource;
        public string EventName;
        public bool EventHasParam = false;
        public Delegate EventHandler;
        public ConditionalCheck[] Conditionals;
    }

    [Serializable]
    public class EventHookup
    {
        public DynamicEvent SourceEvent;
        public string TargetInternalMethod;
    }
}