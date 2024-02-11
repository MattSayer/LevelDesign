using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Core
{
    public static class Tools
    {
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
    }
}