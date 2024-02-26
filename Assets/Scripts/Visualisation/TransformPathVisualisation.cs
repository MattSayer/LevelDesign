using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Visualisation
{
    public class TransformPathVisualisation : MonoBehaviour
    {
        [Title("Path")]
        [SerializeField] private Transform _pathParent;
        [SerializeField] private bool _isClosedPath = false;
        [Space]
        [Title("Line")]
        [SerializeField] private LineRenderer _lineRenderer;
        [Space]
        [Title("Editor")]
        [SerializeField] private float _sphereRadius = 1;

        #region Lifecycle

        private void Start()
        {
            DrawPath();
        }

        #endregion

        #region Visualisation

        private void DrawPath()
        {
            int pathSize = _isClosedPath ? _pathParent.childCount + 1 : _pathParent.transform.childCount;

            _lineRenderer.positionCount = pathSize;

            float pathLength = 0;

            // Calculate total path distance and set line renderer positions
            for (int i = 0; i < _pathParent.childCount; i++)
            {
                if (i > 0)
                {
                    pathLength += Vector3.Distance(_pathParent.GetChild(i - 1).position, _pathParent.GetChild(i).position);
                }
                _lineRenderer.SetPosition(i, _pathParent.GetChild(i).position);
            }

            // Add first point again if it's a closed loop path
            if (_isClosedPath)
            {
                _lineRenderer.SetPosition(pathSize - 1, _pathParent.GetChild(0).position);
                pathLength += Vector3.Distance(_pathParent.GetChild(pathSize - 1).position, _pathParent.GetChild(0).position);
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

        #region Editor

        private void OnDrawGizmos()
        {
            for (int i = 0; i < _pathParent.childCount; i++)
            {
                Gizmos.DrawSphere(_pathParent.GetChild(i).position, _sphereRadius);
                if(i > 0)
                {
                    Gizmos.DrawLine(_pathParent.GetChild(i).position, _pathParent.GetChild(i - 1).position);
                }
            }

            if(_isClosedPath)
            {
                Gizmos.DrawLine(_pathParent.GetChild(_pathParent.childCount - 1).position, _pathParent.GetChild(0).position);
            }
        }

        #endregion

    }
}