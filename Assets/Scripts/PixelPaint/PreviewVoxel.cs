
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;

    public class PreviewVoxel: MonoBehaviour
    {
        [SerializeField] private PixelPaint pixelPaint;
        [SerializeField] private MeshRenderer mr;
        [SerializeField] private MeshFilter mf;

        [SerializeField] private GameObject parent;
        [SerializeField] private Slider xSlider;
        [SerializeField] private Slider ySlider;

        private Vector3 startRotate;
        private float startY;

        private void Start()
        {
            CreatePreviewVoxel();
            xSlider.onValueChanged.AddListener(RotateXPreview);
            ySlider.onValueChanged.AddListener(RotateYPreview);

            startRotate = parent.transform.localEulerAngles;
            
            xSlider.value = -16.2f;
            ySlider.value = -25.5f;
        }
        
        private void FixedUpdate()
        {
            if (!pixelPaint.isLoaded) return;
            for (var i = 0; i < 6; i++)
            {
                UpdateMesh(i, pixelPaint.textureList[pixelPaint.MirrorsState[i]]);   
            }
        }

        private void RotateXPreview(float value)
        {
            var rot = parent.transform.localEulerAngles;
            rot.x = startRotate.x + value;
            parent.transform.rotation = Quaternion.Euler(rot);
        }
        private void RotateYPreview(float value)
        {
            var rot = parent.transform.localEulerAngles;
            rot.y = startRotate.y + value;
            parent.transform.rotation = Quaternion.Euler(rot);
        }
        
        private void CreatePreviewVoxel()
        {
            var vertices = new List<Vector3>();
            var backTriangles = new List<int>();
            var frontTriangles = new List<int>();
            var topTriangles = new List<int>();
            var bottomTriangles = new List<int>();
            var leftTriangles = new List<int>();
            var rightTriangles = new List<int>();
            var uvs = new List<Vector2>();
            var normals = new List<Vector3>();
            var vertCount = 0;
            
            // Loop through neighbour points using faceCheck array.
            for (var p = 0; p < 6; p++)
            {
                for (var i = 0; i < 4; i++)
                {
                    Vector3 vert = new Vector3Int(0, 0, 0);
                    vert += VoxelData.voxelVerts[VoxelData.voxelTris[p, i]];
                    //vert.y *= 1;
                    vertices.Add(vert);
                    
                    Vector2 uv = new Vector2Int(0, 0);
                    uv += VoxelData.voxelUvs[i];
                    uvs.Add(uv);
                }

                for (var i = 0; i < 4; i++)
                    normals.Add(VoxelData.faceChecks[p]);

                switch (p)
                {
                    case 0:
                        backTriangles.Add(vertCount);
                        backTriangles.Add(vertCount + 1);
                        backTriangles.Add(vertCount + 2);
                        backTriangles.Add(vertCount + 2);
                        backTriangles.Add(vertCount + 1);
                        backTriangles.Add(vertCount + 3);
                        break;
                    case 1:
                        frontTriangles.Add(vertCount);
                        frontTriangles.Add(vertCount + 1);
                        frontTriangles.Add(vertCount + 2);
                        frontTriangles.Add(vertCount + 2);
                        frontTriangles.Add(vertCount + 1);
                        frontTriangles.Add(vertCount + 3);
                        break;
                    case 2:
                        topTriangles.Add(vertCount);
                        topTriangles.Add(vertCount + 1);
                        topTriangles.Add(vertCount + 2);
                        topTriangles.Add(vertCount + 2);
                        topTriangles.Add(vertCount + 1);
                        topTriangles.Add(vertCount + 3);
                        break;
                    case 3:
                        bottomTriangles.Add(vertCount);
                        bottomTriangles.Add(vertCount + 1);
                        bottomTriangles.Add(vertCount + 2);
                        bottomTriangles.Add(vertCount + 2);
                        bottomTriangles.Add(vertCount + 1);
                        bottomTriangles.Add(vertCount + 3);
                        break;
                    case 4:
                        leftTriangles.Add(vertCount);
                        leftTriangles.Add(vertCount + 1);
                        leftTriangles.Add(vertCount + 2);
                        leftTriangles.Add(vertCount + 2);
                        leftTriangles.Add(vertCount + 1);
                        leftTriangles.Add(vertCount + 3);
                        break;
                    case 5:
                        rightTriangles.Add(vertCount);
                        rightTriangles.Add(vertCount + 1);
                        rightTriangles.Add(vertCount + 2);
                        rightTriangles.Add(vertCount + 2);
                        rightTriangles.Add(vertCount + 1);
                        rightTriangles.Add(vertCount + 3);
                        break;
                }
                vertCount += 4;
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 6;
            mesh.normals = normals.ToArray();
            mesh.uv = uvs.ToArray();

            mesh.SetTriangles(backTriangles, 0);
            mesh.SetTriangles(frontTriangles, 1);
            mesh.SetTriangles(topTriangles, 2);
            mesh.SetTriangles(bottomTriangles, 3);
            mesh.SetTriangles(leftTriangles, 4);
            mesh.SetTriangles(rightTriangles, 5);
            mf.mesh = mesh;
        }

        private void UpdateMesh(int index, Texture2D _texture)
        {
            mr.materials[index].mainTexture = _texture;
        }
    }