using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Caputure: MonoBehaviour
{
    [SerializeField] private MeshRenderer mr;
    [SerializeField] private MeshFilter mf;
    [SerializeField] private Camera cam;

    private Texture2D texture;
    private RenderTexture renderTexture;
    public void Shot(Texture2D topTexture, Texture2D frontTexture, Texture2D leftTexture, string path)
    {
        var vertices = new List<Vector3>();
        var frontTriangles = new List<int>();
        var topTriangles = new List<int>();
        var leftTriangles = new List<int>();
        var uvs = new List<Vector2>();
        var normals = new List<Vector3>();
        //var vertCount = 0;
        
        // top
        vertices.Add(new Vector3(0.5f, 0.65f, 0));
        vertices.Add(new Vector3(0.1f, 0.75f, 0));
        vertices.Add(new Vector3(0.5f, 0.85f, 0));
        vertices.Add(new Vector3(0.9f, 0.75f, 0));
        uvs.Add(new Vector2( 0.0f, 0.0f));
        uvs.Add(new Vector2( 0.0f, 1.0f));
        uvs.Add(new Vector2( 1.0f, 1.0f));
        uvs.Add(new Vector2( 1.0f, 0f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        topTriangles.Add(0);
        topTriangles.Add(1);
        topTriangles.Add(2);
        topTriangles.Add(2);
        topTriangles.Add(3);
        topTriangles.Add(0);
        
        // front
        vertices.Add(new Vector3(0.5f, 0.1f, 0));
        vertices.Add(new Vector3(0.5f, 0.65f, 0));
        vertices.Add(new Vector3(0.9f, 0.75f, 0));
        vertices.Add(new Vector3(0.9f, 0.25f, 0));
        uvs.Add(new Vector2( 0.0f, 0.0f));
        uvs.Add(new Vector2( 0.0f, 1.0f));
        uvs.Add(new Vector2( 1.0f, 1.0f));
        uvs.Add(new Vector2( 1.0f, 0f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        frontTriangles.Add(4);
        frontTriangles.Add(5);
        frontTriangles.Add(6);
        frontTriangles.Add(6);
        frontTriangles.Add(7);
        frontTriangles.Add(4);
        
        // left
        vertices.Add(new Vector3(0.1f, 0.25f, 0));
        vertices.Add(new Vector3(0.1f, 0.75f, 0));
        vertices.Add(new Vector3(0.5f, 0.65f, 0));
        vertices.Add(new Vector3(0.5f, 0.1f, 0));
        uvs.Add(new Vector2( 0.0f, 0.0f));
        uvs.Add(new Vector2( 0.0f, 1.0f));
        uvs.Add(new Vector2( 1.0f, 1.0f));
        uvs.Add(new Vector2( 1.0f, 0f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        normals.Add(new Vector3(0, 0, -1f));
        leftTriangles.Add(8);
        leftTriangles.Add(9);
        leftTriangles.Add(10);
        leftTriangles.Add(10);
        leftTriangles.Add(11);
        leftTriangles.Add(8);
        
        var mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.subMeshCount = 3;
        mesh.normals = normals.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.SetTriangles(topTriangles, 0);
        mesh.SetTriangles(frontTriangles, 1);
        mesh.SetTriangles(leftTriangles, 2);
        mf.mesh = mesh;
        var materials = mr.materials;
        materials[0].mainTexture = topTexture;
        materials[1].mainTexture = frontTexture;
        materials[2].mainTexture = leftTexture;
        
        renderTexture = new RenderTexture(64, 64, 24);
        cam.targetTexture = renderTexture;
        cam.Render();
        
        texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        RenderTexture.active = renderTexture;
        //texture.alphaIsTransparency = true;
        texture.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        texture.Apply();
        var pixels = texture.GetPixels32();
        for (var i = 0; i < pixels.Length; i++)
        {
            if (pixels[i].a == 0)
            {
                pixels[i] = Color.clear;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        RenderTexture.active = null;
        
        var bytes = texture.EncodeToPNG();
        File.WriteAllBytes(path + "icon.png", bytes);
    }

    private void OnDestroy()
    {
        Destroy(texture);
        Destroy(renderTexture);
    }
}