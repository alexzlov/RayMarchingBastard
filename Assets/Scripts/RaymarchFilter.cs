using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Effects/Raymarch (Generic)")]
public class RaymarchFilter : SceneViewFilter
{
    [SerializeField]
    private Shader   _EffectShader;

    private Material _EffectMaterial;

    private Camera   _CurrentCamera;

    public Transform SunLight;

    public bool DebugMode;

    [SerializeField]
    private Texture2D _ColorRamp;

    [SerializeField]
    private Texture2D _PerfRamp;

    public Material EffectMaterial
    {
        get
        {
            if (!_EffectMaterial && _EffectShader)
            {
                _EffectMaterial = new Material(_EffectShader);
                _EffectMaterial.hideFlags = HideFlags.HideAndDontSave;
            }
            return _EffectMaterial;
        }
    }

    public Camera CurrentCamera
    {
        get
        {
            if (!_CurrentCamera)
            {
                _CurrentCamera = GetComponent<Camera>();
            }
            return _CurrentCamera;
        }
    }

    [ImageEffectOpaque]
    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!EffectMaterial)
        {
            // Do nothing
            Graphics.Blit(source, destination);
            return;
        }

        // Construct a Model Matrix for the Torus
        Matrix4x4 MatTorus = Matrix4x4.TRS(
            Vector3.right * Mathf.Sin(Time.time) * 5,
            Quaternion.identity,
            Vector3.one
        );
        MatTorus *= Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.Euler(new Vector3(0, 0, (Time.time * 200) % 360)),
            Vector3.one
        );

        // Pass frustrum rays to shader
        EffectMaterial.SetMatrix("_FrustrumCornersES", GetFrustrumCorners(CurrentCamera));
        EffectMaterial.SetMatrix("_CameraInvViewMatrix", CurrentCamera.cameraToWorldMatrix);
        EffectMaterial.SetVector("_CameraWS", CurrentCamera.transform.position);
        EffectMaterial.SetVector("_LightDir", SunLight ? SunLight.forward : Vector3.down);
        EffectMaterial.SetMatrix("_MatTorus_InvModel", MatTorus.inverse);
        EffectMaterial.SetTexture("_ColorRamp", _ColorRamp);
        EffectMaterial.SetTexture("_PerfRamp", _PerfRamp);
        EffectMaterial.SetInt("_DebugMode", DebugMode ? 1 : 0);

        // Use given effect shader as image effect
        CustomGraphicsBlit(source, destination, EffectMaterial, 0);
    }

    /// <summary>
    /// Stores the normalized rays representing the camera frustrum 
    /// in a 4x4 matrix. Each row is a vector.
    /// </summary>
    /// The following rays are stored in each row
    /// (in eyespace, not worldspace):
    /// Top Left corner:        row = 0
    /// Top Right corner:       row = 1
    /// Bottom Right corner:    row = 2
    /// Bottom Left corner:     row = 3
    private Matrix4x4 GetFrustrumCorners(Camera camera)
    {
        float cameraFov = camera.fieldOfView;
        float cameraAspect = camera.aspect;

        Matrix4x4 frustrumCorners = Matrix4x4.identity;

        float fovHalf = cameraFov * 0.5f;
        float tanFov = Mathf.Tan(fovHalf * Mathf.Deg2Rad);

        Vector3 toRight = Vector3.right * tanFov * cameraAspect;
        Vector3 toTop = Vector3.up * tanFov;

        Vector3 topLeft     = (-Vector3.forward - toRight + toTop);
        Vector3 topRight    = (-Vector3.forward + toRight + toTop);
        Vector3 bottomRight = (-Vector3.forward + toRight - toTop);
        Vector3 bottomLeft  = (-Vector3.forward - toRight - toTop);

        frustrumCorners.SetRow(0, topLeft);
        frustrumCorners.SetRow(1, topRight);
        frustrumCorners.SetRow(2, bottomRight);
        frustrumCorners.SetRow(3, bottomLeft);

        return frustrumCorners;
    }

    /// <summary>
    /// Custom version of Graphics.Blit that encodes frustrum corner indices
    /// into the input vertices.
    /// </summary>
    /// In a shader the following frustrum corner index information
    /// gets passed to the z coordinate:
    /// Top Left vertex:     z = 0, u = 0; v = 0
    /// Top Right vertex:    z = 1; u = 1; v = 0
    /// Bottom Right vertex: z = 2; u = 1; v = 1
    /// Bottom Left vertex:  z = 3; u = 1; v = 0
    private static void CustomGraphicsBlit(RenderTexture source, RenderTexture destination, Material fxMaterial, int passNumber)
    {
        RenderTexture.active = destination;
        fxMaterial.SetTexture("_MainTex", source);

        GL.PushMatrix();
        // Note: z value of vertices don't make a difference
        // 'cause we are using orthographic projection
        GL.LoadOrtho();

        fxMaterial.SetPass(passNumber);

        GL.Begin(GL.QUADS);
        // Here, GL.MultitexCoords2(0, x, y) assigns the value (x, y)
        // to the TEXCOORD0 slot in the shader. GL.Vertex3(x, y, z)
        // queues up a vertex at position (x, y, z) to be drawn.
        // Note that we are storing our own custom frustrum information
        // in the z coordinate.
        GL.MultiTexCoord2(0, 0.0f, 0.0f);
        GL.Vertex3(0.0f, 0.0f, 3.0f); // Bottom left
        GL.MultiTexCoord2(0, 1.0f, 0.0f);
        GL.Vertex3(1.0f, 0.0f, 2.0f); // Borrom right
        GL.MultiTexCoord2(0, 1.0f, 1.0f);
        GL.Vertex3(1.0f, 1.0f, 1.0f); // Top right
        GL.MultiTexCoord2(0, 0.0f, 1.0f);
        GL.Vertex3(0.0f, 1.0f, 0.0f); // Top left
        GL.End();
        GL.PopMatrix();        
    }

    
}