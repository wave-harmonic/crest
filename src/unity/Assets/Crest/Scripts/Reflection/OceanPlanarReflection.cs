using UnityEngine;

public class OceanPlanarReflection : MonoBehaviour
{
    public bool disablePixelLights = true;
    public int textureSize = 256;
    public float clipPlaneOffset = 0.07f;
    public LayerMask reflectLayers = -1;
    public bool _showReflection = false;

    Camera m_ReflectionCamera;
    private RenderTexture m_ReflectionTexture;
    private int m_OldReflectionTextureSize;
    public Material m_OceanMat;
    Camera _camera;
    [Range(0, 1)]
    public float _waveHeightOffset = 0f;
    [Range(0, 1)]
    public float _waveDispOffset = 0f;
    public bool _hideCamera = true;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        if (!_camera)
        {
            Debug.LogWarning("Disabling planar reflections as no camera found on gameobject to generate reflection from.", this);
            enabled = false;
            return;
        }
    }

    private void LateUpdate()
    {
        UpdateReflection();
    }

    public void UpdateReflection()
    {
        CreateWaterObjects(_camera);

        // find out the reflection plane: position and normal in world space
        Vector3 pos = Crest.OceanRenderer.Instance.transform.position;
        //pos.y += Crest.OceanRenderer.Instance.MaxVertDisplacement * _waveHeightOffset;
        //Vector3 disp = Vector3.zero;
        //if (Crest.OceanRenderer.Instance.CollisionProvider.SampleDisplacement(ref pos, ref disp, 10f))
        //{
        //    pos.y += disp.y * _waveDispOffset;
        //}
        Vector3 normal = Vector3.up;

        // Optionally disable pixel lights for reflection/refraction
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if (disablePixelLights)
        {
            QualitySettings.pixelLightCount = 0;
        }

        UpdateCameraModes(_camera, m_ReflectionCamera);

        // Render reflection if needed
        // Reflect camera around reflection plane
        float d = -Vector3.Dot(normal, pos) - clipPlaneOffset;
        Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

        Matrix4x4 reflection = Matrix4x4.zero;
        CalculateReflectionMatrix(ref reflection, reflectionPlane);
        Vector3 oldpos = _camera.transform.position;
        Vector3 newpos = reflection.MultiplyPoint(oldpos);
        m_ReflectionCamera.worldToCameraMatrix = _camera.worldToCameraMatrix * reflection;

        // Setup oblique projection matrix so that near plane is our reflection
        // plane. This way we clip everything below/above it for free.
        Vector4 clipPlane = CameraSpacePlane(m_ReflectionCamera, pos, normal, 1.0f);
        m_ReflectionCamera.projectionMatrix = _camera.CalculateObliqueMatrix(clipPlane);

        // Set custom culling matrix from the current camera
        m_ReflectionCamera.cullingMatrix = _camera.projectionMatrix * _camera.worldToCameraMatrix;

        m_ReflectionCamera.cullingMask = ~(1 << 4) & reflectLayers.value; // never render water layer
        m_ReflectionCamera.targetTexture = m_ReflectionTexture;
        bool oldCulling = GL.invertCulling;
        GL.invertCulling = !oldCulling;
        m_ReflectionCamera.transform.position = newpos;
        Vector3 euler = _camera.transform.eulerAngles;
        m_ReflectionCamera.transform.eulerAngles = new Vector3(-euler.x, euler.y, euler.z);
        m_ReflectionCamera.Render();
        m_ReflectionCamera.transform.position = oldpos;
        GL.invertCulling = oldCulling;
        m_OceanMat.SetTexture("_ReflectionTex", m_ReflectionTexture);

        // Restore pixel light count
        if (disablePixelLights)
        {
            QualitySettings.pixelLightCount = oldPixelLightCount;
        }
    }


    void UpdateCameraModes(Camera src, Camera dest)
    {
        if (dest == null)
        {
            return;
        }
        // set water camera to clear the same way as current camera
        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = src.backgroundColor;
        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent<Skybox>();
            Skybox mysky = dest.GetComponent<Skybox>();
            if (!sky || !sky.material)
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }
        // update other values to match current camera.
        // even if we are supplying custom camera&projection matrices,
        // some of values are used elsewhere (e.g. skybox uses far plane)
        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
    }

    // On-demand create any objects we need for water
    void CreateWaterObjects(Camera currentCamera)
    {
        // Reflection render texture
        if (!m_ReflectionTexture || m_OldReflectionTextureSize != textureSize)
        {
            if (m_ReflectionTexture)
            {
                DestroyImmediate(m_ReflectionTexture);
            }
            m_ReflectionTexture = new RenderTexture(textureSize, textureSize, 16);
            m_ReflectionTexture.name = "__WaterReflection" + GetInstanceID();
            m_ReflectionTexture.isPowerOfTwo = true;
            m_ReflectionTexture.hideFlags = HideFlags.DontSave;
            m_OldReflectionTextureSize = textureSize;
        }

        // Camera for reflection
        if (!m_ReflectionCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
        {
            GameObject go = new GameObject("Water Refl Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
            m_ReflectionCamera = go.GetComponent<Camera>();
            m_ReflectionCamera.enabled = false;
            m_ReflectionCamera.transform.position = transform.position;
            m_ReflectionCamera.transform.rotation = transform.rotation;
            m_ReflectionCamera.gameObject.AddComponent<FlareLayer>();
            if (_hideCamera)
                go.hideFlags = HideFlags.HideAndDontSave;
        }
    }

    // Given position/normal of the plane, calculates plane in camera space.
    Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * clipPlaneOffset;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    // Calculates reflection matrix around the given plane
    static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (- 2F * plane[0] * plane[1]);
        reflectionMat.m02 = (- 2F * plane[0] * plane[2]);
        reflectionMat.m03 = (- 2F * plane[3] * plane[0]);

        reflectionMat.m10 = (- 2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (- 2F * plane[1] * plane[2]);
        reflectionMat.m13 = (- 2F * plane[3] * plane[1]);

        reflectionMat.m20 = (- 2F * plane[2] * plane[0]);
        reflectionMat.m21 = (- 2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (- 2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }

    // Cleanup all the objects we possibly have created
    void OnDisable()
    {
        if (m_ReflectionTexture)
        {
            Destroy(m_ReflectionTexture);
            m_ReflectionTexture = null;
        }
        if (m_ReflectionCamera)
        {
            Destroy(m_ReflectionCamera.gameObject);
            m_ReflectionCamera = null;
        }
    }

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (_showReflection)
        {
            GUI.DrawTexture(new Rect(300, 0, 100, 100), m_ReflectionTexture);
        }
    }
#endif
}

