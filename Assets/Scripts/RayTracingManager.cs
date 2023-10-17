
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
    [SerializeField] ComputeShader rayTracingShader;
    [SerializeField] private Vector2Int baseResolution = new Vector2Int(1920, 1080);
    [Range(0.000001f, 1f)]
    [SerializeField] private float rescaleFactor;
    private RenderTexture target;
    [SerializeField] private int maxBounces;
    [SerializeField] private int samples;
    private uint currentSample = 0;
    private Material addMaterial;
    [SerializeField] bool useShaderInSceneView;
    [SerializeField] private bool refine;
    
    private static List<MeshObject> _meshes = new List<MeshObject>();
    private static List<Vector3> _vertices = new List<Vector3>();
    private static List<Vector3> _normals = new List<Vector3>();
    private static List<int> _indices = new List<int>();
    private ComputeBuffer _meshObjectBuffer;
    private ComputeBuffer _vertexBuffer;
    private ComputeBuffer _normalsBuffer;
    private ComputeBuffer _indexBuffer;
    
    private void Update()
    {
        if (Camera.current && Camera.current.transform.hasChanged)
        {
            currentSample = 0;
            transform.hasChanged = false;
        }
    }

    private void OnValidate()
    {
        currentSample = 0;
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (useShaderInSceneView || Camera.current.name != "SceneCamera")
        {
            RebuildMeshObjectBuffers();
            SendParams();
            Render(destination);
        }
        else
        {
            _meshObjectsNeedRebuilding = true;
            Graphics.Blit(source, destination);
        }
    }

    void Render(RenderTexture destination)
    {
        InitRenderTexture();
        
        rayTracingShader.SetTexture(0, "Result", target);
        
        uint kernelX, kernelY, kernelZ;
        rayTracingShader.GetKernelThreadGroupSizes(0, out kernelX, out kernelY, out kernelZ);
        
        int threadGroupsX = Mathf.CeilToInt(target.width / (float)kernelX);
        int threadGroupsY = Mathf.CeilToInt(target.height / (float)kernelY);
        rayTracingShader.Dispatch(0, threadGroupsX, threadGroupsY, 1);

        if (refine)
        {
            if (addMaterial == null)
                addMaterial = new Material(Shader.Find("Hidden/AddShader"));
            addMaterial.SetFloat("_CurrentSample", currentSample);
            Graphics.Blit(target, destination, addMaterial);
            currentSample++;
        }
        else Graphics.Blit(target, destination);
    }
    
    private void InitRenderTexture()
    {
        int targetWidth = (int)(baseResolution.x * rescaleFactor);
        int targetHeighth = (int)(baseResolution.y * rescaleFactor);
        
        if (target == null || 
            target.width != targetWidth || 
            target.height != targetHeighth)
        {
            if (target != null)
                target.Release();
            
            target = new RenderTexture(targetWidth, 
                targetHeighth, 0,
                RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            target.filterMode = FilterMode.Point;
            target.enableRandomWrite = true;
            target.Create();
        }
    }
    
    void SendParams()
    {
        Camera cam = Camera.current;
        
        rayTracingShader.SetMatrix("_CameraToWorld", cam.cameraToWorldMatrix);
        rayTracingShader.SetMatrix("_CameraInverseProjection", cam.projectionMatrix.inverse);
        rayTracingShader.SetInt("_FrameCount", Time.frameCount);
        rayTracingShader.SetInt("_MaxBounces", maxBounces);
        rayTracingShader.SetInt("_Samples", samples);
        rayTracingShader.SetFloat("_RescaleFactor", rescaleFactor);

        Vector2 pixelOffset = refine ? new Vector2(Random.value, Random.value) : new Vector2(0.5f, 0.5f);
        rayTracingShader.SetVector("_PixelOffset", pixelOffset);
        
        SetBuffer("_meshes", _meshObjectBuffer);
        SetBuffer("_vertices", _vertexBuffer);
        SetBuffer("_normals", _vertexBuffer);
        SetBuffer("_indices", _indexBuffer);
    }
    
    private static bool _meshObjectsNeedRebuilding = false;
    private static List<RayTracingObject> _rayTracingObjects = new List<RayTracingObject>();
    public static void RegisterObject(RayTracingObject obj)
    {
        //obj.index = _rayTracingObjects.Count;
        _rayTracingObjects.Add(obj);
        _meshObjectsNeedRebuilding = true;
    }
    public static void UnregisterObject(RayTracingObject obj)
    {
        _rayTracingObjects.Remove(obj);
        _meshObjectsNeedRebuilding = true;
    }
    
    public static void UpdateObject(RayTracingObject obj)
    {
        UnregisterObject(obj);
        RegisterObject(obj);
        /*_rayTracingObjects[obj.index] = obj;
        _meshObjectsNeedRebuilding = true;*/
    }
    
    struct MeshObject
    {
        public Matrix4x4 localToWorld;
        public int firstIndex;
        public int indexCount;

        public Vector3 boundsMin;
        public Vector3 boundsMax;
        
        public Color diffuseColor;
        public Color emissiveColor;
        public float emission;
    }
    
    private void RebuildMeshObjectBuffers()
    {
        if (!_meshObjectsNeedRebuilding)
        {
            return;
        }
        _meshObjectsNeedRebuilding = false;
        
        _meshes.Clear();
        _vertices.Clear();
        _normals.Clear();
        _indices.Clear();

        foreach (RayTracingObject obj in _rayTracingObjects)
        {
            Mesh mesh = obj.GetComponent<MeshFilter>().sharedMesh;
            if(mesh == null) continue;

            int firstVertex = _vertices.Count;
            _vertices.AddRange(mesh.vertices);
            _normals.AddRange(mesh.normals);

            int firstIndex = _indices.Count;
            var indices = mesh.GetIndices(0);
            _indices.AddRange(indices.Select(index => index + firstVertex));

            Vector3 min = new Vector3();
            Vector3 max = new Vector3();
            
            obj.GetBounds(ref min, ref max);
            
            _meshes.Add(new MeshObject()
            {
                localToWorld = obj.transform.localToWorldMatrix,
                firstIndex = firstIndex,
                indexCount = indices.Length,
                boundsMin = min,
                boundsMax = max,
                diffuseColor = obj.material.diffuseColor,
                emissiveColor = obj.material.emissiveColor,
                emission = obj.material.emission
            });
        }
        CreateComputeBuffer(ref _meshObjectBuffer, _meshes, 132);
        CreateComputeBuffer(ref _vertexBuffer, _vertices, 12);
        CreateComputeBuffer(ref _normalsBuffer, _normals, 12);
        CreateComputeBuffer(ref _indexBuffer, _indices, 4);
        Debug.Log(_vertices.Count + " triangles built.");
    }
    
    private static void CreateComputeBuffer<T>(ref ComputeBuffer buffer, List<T> data, int stride)
        where T : struct
    {
        if (buffer != null)
        {
            if (data.Count == 0 || buffer.count != data.Count || buffer.stride != stride)
            {
                buffer.Release();
                buffer = null;
            }
        }
        if (data.Count != 0)
        {
            if (buffer == null)
            {
                buffer = new ComputeBuffer(data.Count, stride);
            }
            buffer.SetData(data);
        }
    }
    
    private void SetBuffer(string name, ComputeBuffer buffer)
    {
        if (buffer != null)
        {
            rayTracingShader.SetBuffer(0, name, buffer);
        }
    }
}