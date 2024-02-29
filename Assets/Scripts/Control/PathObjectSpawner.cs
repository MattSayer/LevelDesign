using PathCreation;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmalgamGames.Control
{
    public class PathObjectSpawner : ObjectSpawner
    {
        [Space]
        [Title("Path")]
        [SerializeField] private PathType _pathType;
        [ShowIf("@this._pathType == PathType.Transform")]
        [SerializeField] private Transform _pathParent;
        [ShowIf("@this._pathType == PathType.Bezier")]
        [SerializeField] private PathCreator _path;

        protected override void InitialiseSpawnable(GameObject spawnableObj)
        {
            if(spawnableObj.TryGetComponent(out IPathFollower pathFollower))
            {
                switch(_pathType)
                {
                    case PathType.Transform:
                        pathFollower.SetPath(_pathParent.gameObject);
                        break;
                    case PathType.Bezier:
                        pathFollower.SetPath(_path.gameObject);
                        break;
                }
            }
        }
    }

    public enum PathType
    {
        Transform,
        Bezier
    }
}