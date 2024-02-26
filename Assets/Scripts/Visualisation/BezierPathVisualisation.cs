using PathCreation;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Visualisation
{
    public class BezierPathVisualisation : MonoBehaviour
    {
        [Title("Settings")]
        [SerializeField] private int _lineResolution;
        [Space]
        [Title("Components")]
        [SerializeField] private PathCreator _pathCreator;
        [SerializeField] private LineRenderer _lineRenderer;
        


        private VertexPath _path;

        #region Lifecycle

        private void Start()
        {
            _path = _pathCreator.path;

            _lineRenderer.positionCount = _lineResolution;

            float pointTime = 1.0f / _lineResolution;

            float pathLength = 0;

            for(int i = 0; i < _lineResolution; i++)
            {
                Vector3 currentPoint = _path.GetPointAtTime(pointTime * i);
                _lineRenderer.SetPosition(i,currentPoint);
                if(i > 0)
                {
                    pathLength += Vector3.Distance(currentPoint, _path.GetPointAtTime(pointTime * (i - 1)));
                }
            }

            // Calculate the length to height ratio
            float xTiling = pathLength / _lineRenderer.startWidth;

            // Round to floor to ensure the image doesn't get cut off 
            xTiling = Mathf.Floor(xTiling);

            // Calculate and apply new width to match xTiling
            float newWidth = pathLength / xTiling;
            _lineRenderer.startWidth = _lineRenderer.endWidth = newWidth;

            Vector4 tiling = Vector4.zero;
            tiling.x = xTiling;
            tiling.y = 1;
            _lineRenderer.material.SetVector(Shader.PropertyToID("_Tiling"), tiling);

        }

        #endregion
    }
}