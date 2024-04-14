using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using AmalgamGames.Utils;

namespace AmalgamGames.Interactables
{
    public class DisplacementOutline : MonoBehaviour
    {
        [Header("Skinned Mesh Renderer")]
        [SerializeField] private SkinnedMeshRenderer _target;
        [Space]
        [Header("Outline")]
        [SerializeField] private Material _outlineMaterial;
        [SerializeField] private float _thickness;
        [SerializeField] private Color _outlineColour = Color.white;
        [Space]
        [Header("Process")]
        [SerializeField] private bool _duplicateMesh = false;

        private MeshFilter _meshFilter;

        private Mesh _thisMesh;
        private Mesh _newMesh;
        private GameObject _outlineObject;

        private int _thicknessHash;
        private int _outlineColourHash;
        private int _useCustomNormalsHash;
        private float _setThickness;
        private Color _setColour;

        private bool _isOutlineActive = false;

        private Material _tempMat;

        #region Unity methods

        private void Awake()
        {

            _thicknessHash = Shader.PropertyToID("_Thickness");
            _outlineColourHash = Shader.PropertyToID("_OutlineColour");
            _useCustomNormalsHash = Shader.PropertyToID("_UseCustomNormals");

            // Creates a copy of the base outline material
            _tempMat = new Material(_outlineMaterial);

            // If no skinned mesh target, apply outline process to the attached mesh
            if (_target == null)
            {
                if (_duplicateMesh)
                {
                    DuplicateStaticMesh();
                }
                else
                {
                    AddCustomNormals();
                }

            }
            else
            {
                if (_duplicateMesh)
                {
                    DuplicateSkinnedMesh();
                }
                else
                {
                    AddCustomSkinnedNormals();
                }
            }

        }

        private void Update()
        {
            if (_outlineObject != null)
            {
                // Updates outline material thickness if it's been changed at runtime
                if (_thickness != _setThickness)
                {
                    _tempMat.SetFloat(_thicknessHash, _thickness / 1000);
                    _setThickness = _thickness;
                }
                // Updates outline material colour if it's been changed at runtime
                if (_setColour != _outlineColour)
                {
                    _tempMat.SetColor(_outlineColourHash, _outlineColour);
                    _setColour = _outlineColour;
                }
            }
        }

        private void OnDestroy()
        {
            // Destroys duplicate object if this object is destroyed
            Destroy(_outlineObject);
        }

        #endregion

        #region Public methods

        public void ActivateOutline()
        {
            if (!_isOutlineActive)
            {
                _isOutlineActive = true;

                // If this outline uses a duplicate mesh, simply activate that game object
                if (_duplicateMesh)
                {
                    _outlineObject.SetActive(true);
                }
                else
                {
                    // If no skinned mesh target, use the attached mesh and add the outline material to the mesh renderer
                    if (_target == null)
                    {
                        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                        Material[] mats = new Material[meshRenderer.materials.Length + 1];
                        for (int i = 0; i < meshRenderer.materials.Length; i++)
                        {
                            mats[i] = meshRenderer.materials[i];
                        }
                        mats[mats.Length - 1] = _tempMat;
                        meshRenderer.materials = mats;
                    }
                    // Otherwise add the outline material to the target skinned mesh renderer
                    else
                    {
                        Material[] mats = new Material[_target.materials.Length + 1];
                        for (int i = 0; i < _target.materials.Length; i++)
                        {
                            mats[i] = _target.materials[i];
                        }
                        mats[mats.Length - 1] = _tempMat;
                        _target.materials = mats;
                    }
                }
            }
        }

        public void DeactivateOutline()
        {
            if (_isOutlineActive)
            {
                _isOutlineActive = false;

                // If this outline uses a duplicate mesh, simply deactivate that game object
                if (_duplicateMesh)
                {
                    _outlineObject.SetActive(false);
                }
                else
                {
                    // If no skinned mesh target, use the attached mesh and remove the outline material from the mesh renderer
                    if (_target == null)
                    {
                        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
                        Material[] mats = new Material[meshRenderer.materials.Length - 1];
                        for (int i = 0; i < meshRenderer.materials.Length; i++)
                        {
                            if (meshRenderer.materials[i] == _tempMat)
                            {
                                continue;
                            }
                            mats[i] = meshRenderer.materials[i];
                        }
                        meshRenderer.materials = mats;
                    }
                    // Otherwise remove the outline material from the target skinned mesh renderer
                    else
                    {
                        Material[] mats = new Material[_target.materials.Length - 1];
                        for (int i = 0; i < _target.materials.Length; i++)
                        {
                            if (_target.materials[i] == _tempMat)
                            {
                                continue;
                            }
                            mats[i] = _target.materials[i];
                        }
                        _target.materials = mats;
                    }
                }
            }
        }

        #endregion

        #region Utility

        private void AddCustomSkinnedNormals()
        {
            _thisMesh = _target.sharedMesh;

            // Creates a new duplicate mesh from the target skinned mesh
            _newMesh = new Mesh();
            _newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            CopyMesh(_thisMesh, _newMesh);

            _tempMat.SetFloat(_thicknessHash, _thickness / 1000);
            _setThickness = _thickness;

            _tempMat.SetColor(_outlineColourHash, _outlineColour);
            _setColour = _outlineColour;

            _tempMat.SetInt(_useCustomNormalsHash, 1);

            // Recalculate normals with max smoothing (180 degrees)
            Vector3[] verts = _newMesh.vertices;
            Vector3[] normals = _newMesh.GetRecalculatedNormals(180);

            Color32[] vertexColours = new Color32[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                vertexColours[i] = new Color(normals[i].x, normals[i].y, normals[i].z);
            }

            // Passes the normals data via vertex colours to the target mesh
            _thisMesh.colors32 = vertexColours;

            _outlineObject = _target.gameObject;
        }

        private void AddCustomNormals()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _thisMesh = _meshFilter.sharedMesh;

            // Creates a new mesh and copies data from the target mesh over
            _newMesh = new Mesh();
            _newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            CopyMesh(_thisMesh, _newMesh);

            _tempMat.SetFloat(_thicknessHash, _thickness / 1000);
            _setThickness = _thickness;

            _tempMat.SetColor(_outlineColourHash, _outlineColour);
            _setColour = _outlineColour;

            _tempMat.SetInt(_useCustomNormalsHash, 1);

            // Recalculate normals with max smoothing (180 degrees)
            Vector3[] verts = _newMesh.vertices;

            Vector3[] normals = _newMesh.GetRecalculatedNormals(180);

            Color32[] vertexColours = new Color32[verts.Length];

            for (int i = 0; i < verts.Length; i++)
            {
                vertexColours[i] = new Color(normals[i].x, normals[i].y, normals[i].z);
            }

            // Passes the normals data via vertex colours to the new mesh
            _thisMesh.colors32 = vertexColours;

            _outlineObject = gameObject;
        }

        private void DuplicateSkinnedMesh()
        {
            _thisMesh = _target.sharedMesh;

            // Creates a new skinned mesh and copies data over from target mesh
            _newMesh = new Mesh();
            _newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            CopySkinnedMesh(_thisMesh, _newMesh);

            // Instantiates a new gameobject to house the skinned mesh renderer
            GameObject outlineObj = Instantiate(_target.gameObject, _target.transform.parent);
            outlineObj.name = "Outline_" + _target.gameObject.name;
            SkinnedMeshRenderer skinnedMeshRenderer = outlineObj.GetComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.bones = _target.bones;
            skinnedMeshRenderer.rootBone = _target.rootBone;
            skinnedMeshRenderer.material = _tempMat;

            _tempMat.SetFloat(_thicknessHash, _thickness / 1000);
            _setThickness = _thickness;

            _tempMat.SetColor(_outlineColourHash, _outlineColour);
            _setColour = _outlineColour;

            // Recalculate normals with max smoothing (180 degrees)
            _newMesh.RecalculateNormals(180);


            _outlineObject = outlineObj;

            // Outline deactivated by default
            _outlineObject.SetActive(false);
        }

        private void DuplicateStaticMesh()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _thisMesh = _meshFilter.sharedMesh;

            if (_thisMesh != null && _tempMat != null)
            {
                // Creates a new mesh and copies data from the target mesh
                _newMesh = new Mesh();
                _newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                CopyMesh(_thisMesh, _newMesh);


                // Create duplicate gameobject at some location, rotation, etc...
                GameObject outlineObj = new GameObject();
                outlineObj.name = "Outline_" + gameObject.name;
                outlineObj.transform.SetParent(transform);
                outlineObj.transform.localPosition = Vector3.zero;
                outlineObj.transform.localEulerAngles = Vector3.zero;
                outlineObj.transform.localScale = Vector3.one;


                MeshFilter newMeshFilter = outlineObj.AddComponent<MeshFilter>();
                newMeshFilter.sharedMesh = _newMesh;

                MeshRenderer newMeshRenderer = outlineObj.AddComponent<MeshRenderer>();
                newMeshRenderer.material = _tempMat;


                _tempMat.SetFloat(_thicknessHash, _thickness / 1000);
                _setThickness = _thickness;

                _tempMat.SetColor(_outlineColourHash, _outlineColour);
                _setColour = _outlineColour;

                _tempMat.SetInt(_useCustomNormalsHash, 0);

                // Recalculate normals with max smoothing (180 degrees)
                _newMesh.RecalculateNormals(180);

                _outlineObject = outlineObj;

                // Outline deactivated by default
                _outlineObject.SetActive(false);
            }
        }

        /// <summary>
        /// Copies data from source mesh to destination mesh
        /// </summary>
        /// <param name="source">The source mesh to copy from</param>
        /// <param name="destination">The destination mesh to copy to</param>
        private void CopyMesh(Mesh source, Mesh destination)
        {
            Vector3[] verts = new Vector3[source.vertices.Length];
            int[][] tris = new int[source.subMeshCount][];
            Vector2[] uv = new Vector2[source.uv.Length];
            Vector2[] uv2 = new Vector2[source.uv2.Length];
            Vector4[] tangents = new Vector4[source.tangents.Length];
            Vector3[] normals = new Vector3[source.normals.Length];
            Color32[] colours = new Color32[source.colors32.Length];

            Array.Copy(source.vertices, verts, verts.Length);

            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = source.GetTriangles(i);
            }

            Array.Copy(source.uv, uv, uv.Length);
            Array.Copy(source.uv2, uv2, uv2.Length);
            Array.Copy(source.normals, normals, normals.Length);
            Array.Copy(source.tangents, tangents, tangents.Length);
            Array.Copy(source.colors32, colours, colours.Length);

            destination.Clear();
            destination.name = "PROC_" + source.name;
            destination.vertices = verts;

            destination.subMeshCount = tris.Length;

            for (int i = 0; i < tris.Length; i++)
            {
                destination.SetTriangles(tris[i], i);
            }

            destination.uv = uv;
            destination.uv2 = uv2;
            destination.normals = normals;
            destination.colors32 = colours;
            destination.tangents = tangents;
        }

        /// <summary>
        /// Copies data from source skinned mesh to destination skinned mesh
        /// </summary>
        /// <param name="source">The source mesh to copy from</param>
        /// <param name="destination">The destination mesh to copy to</param>
        private void CopySkinnedMesh(Mesh source, Mesh destination)
        {
            Vector3[] verts = new Vector3[source.vertices.Length];
            int[][] tris = new int[source.subMeshCount][];
            Vector2[] uv = new Vector2[source.uv.Length];
            Vector2[] uv2 = new Vector2[source.uv2.Length];
            Vector4[] tangents = new Vector4[source.tangents.Length];
            Vector3[] normals = new Vector3[source.normals.Length];
            Color32[] colours = new Color32[source.colors32.Length];
            BoneWeight[] boneWeights = new BoneWeight[source.boneWeights.Length];
            Matrix4x4[] bindPoses = new Matrix4x4[source.bindposes.Length];

            Array.Copy(source.vertices, verts, verts.Length);

            for (int i = 0; i < tris.Length; i++)
            {
                tris[i] = source.GetTriangles(i);
            }

            Array.Copy(source.uv, uv, uv.Length);
            Array.Copy(source.uv2, uv2, uv2.Length);
            Array.Copy(source.normals, normals, normals.Length);
            Array.Copy(source.tangents, tangents, tangents.Length);
            Array.Copy(source.colors32, colours, colours.Length);

            Array.Copy(source.boneWeights, boneWeights, boneWeights.Length);
            Array.Copy(source.bindposes, bindPoses, bindPoses.Length);

            destination.Clear();
            destination.name = "PROC_" + source.name;
            destination.vertices = verts;

            destination.subMeshCount = tris.Length;

            for (int i = 0; i < tris.Length; i++)
            {
                destination.SetTriangles(tris[i], i);
            }

            destination.uv = uv;
            destination.uv2 = uv2;
            destination.normals = normals;
            destination.colors32 = colours;
            destination.tangents = tangents;

            var bonesPerVertex = source.GetBonesPerVertex();
            if (bonesPerVertex.Length == 0)
            {
                Debug.Log("No bones per vertex");
                return;
            }

            destination.SetBoneWeights(bonesPerVertex, source.GetAllBoneWeights());
            destination.bindposes = bindPoses;

        }

        #endregion


    }
}
