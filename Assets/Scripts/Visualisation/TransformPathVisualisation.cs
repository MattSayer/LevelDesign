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

        #region Lifecycle

        private void Start()
        {
            int _pathSize = _isClosedPath ? _pathParent.childCount + 1 : _pathParent.transform.childCount;

            _lineRenderer.positionCount = _pathSize;

            float pathLength = 0;

            for(int i = 0; i < _pathParent.childCount; i++)
            {
                if(i > 0)
                {
                    pathLength += Vector3.Distance(_pathParent.GetChild(i - 1).position, _pathParent.GetChild(i).position);
                }
                _lineRenderer.SetPosition(i, _pathParent.GetChild(i).position);
            }

            if(_isClosedPath)
            {
                _lineRenderer.SetPosition(_pathSize - 1, _pathParent.GetChild(0).position);
                pathLength += Vector3.Distance(_pathParent.GetChild(_pathSize - 1).position, _pathParent.GetChild(0).position);
            }

            float xTiling = pathLength / _lineRenderer.startWidth;

            Vector4 tiling = Vector4.zero;
            tiling.x = xTiling;
            tiling.y = 1;
            _lineRenderer.material.SetVector(Shader.PropertyToID("_Tiling"), tiling);
        }

        #endregion


    }
}