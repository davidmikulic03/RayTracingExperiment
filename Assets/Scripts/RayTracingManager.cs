using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteAlways, ImageEffectAllowedInSceneView]
public class RayTracingManager : MonoBehaviour
{
    [SerializeField] Shader rayTracingShader;
    [SerializeField] private int maxBounces;
    [SerializeField] bool useShaderInSceneView;
    
    private Material _rayTracingMaterial;
    private Mesh[] meshes;
    private Triangle[] triangles;
    
    private void OnRenderImage(RenderTexture source, RenderTexture target)
    {
        if (useShaderInSceneView || Camera.current.name != "SceneCamera")
        {
            _rayTracingMaterial = new Material(rayTracingShader);
            SendCameraParams(Camera.current);
                
            Graphics.Blit(null, target, _rayTracingMaterial);
        }
        else
            Graphics.Blit(source, target);
    }
    
    void SendCameraParams(Camera cam)
    {
        float clipPlaneHeight = cam.nearClipPlane * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * 2;
        float clipPlaneWidth = clipPlaneHeight * cam.aspect;

        Quaternion rotation = cam.transform.rotation;
        
        _rayTracingMaterial.SetVector("ViewParams", new Vector3(clipPlaneWidth, clipPlaneHeight, cam.nearClipPlane));
        _rayTracingMaterial.SetMatrix("CameraRotation", Matrix4x4.Rotate(rotation));
        _rayTracingMaterial.SetInteger("FrameCount", Time.frameCount);
    }

    void SendParams()
    {
        _rayTracingMaterial.SetInteger("MaxBounces", maxBounces);
        
        //_rayTracingMaterial.SetBuffer("Triangles", );
    }

    void GetMeshesByTag(string tag)
    {
        GameObject[] renderObjects = GameObject.FindGameObjectsWithTag(tag);

        foreach (var renderObject in renderObjects)
        {
            
        }
    }
}