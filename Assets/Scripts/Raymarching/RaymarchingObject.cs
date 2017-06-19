using UnityEngine;

[ExecuteInEditMode, RequireComponent(typeof(Renderer))]
public class RaymarchingObject : MonoBehaviour
{
    public enum Shape
    {
        Cube,
        Sphere,
        None,
    }

    [SerializeField]
    Shape shape = Shape.Cube;

    [SerializeField]
    Color GizmoColor = new Color(1f, 1f, 1f, 0.1f);

    [SerializeField] Color GizmoSelectedColor = new Color(1f, 0f, 0f, 1f);

    private int _scaleId;
    private Material _material;

    private Vector3 Scale
    {
        get
        {
            var s = transform.localScale;
            return new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z));
        }
    }

    void Awake()
    {
        _scaleId = Shader.PropertyToID("_Scale");
        _material = GetComponent<Renderer>().sharedMaterial;
    }

    void Update()
    {
#if UNITY_EDITOR
        _material = GetComponent<Renderer>().sharedMaterial;
#endif
        UpdateScale();
        UpdateShape();
    }

    private void UpdateScale()
    {
        _material.SetVector(_scaleId, Scale);
    }

    private void UpdateShape()
    {
        switch (shape)
        {
             case Shape.Cube:
                _material.EnableKeyword("OBJECT_SHAPE_CUBE");
                _material.DisableKeyword("OBJECT_SHAPE_SPHERE");
                break;
            case Shape.Sphere:
                _material.EnableKeyword("OBJECT_SHAPE_SPHERE");
                _material.DisableKeyword("OBJECT_SHAPE_CUBE");
                break;
        }
    }

    void OnDrawGizmos()
    {
        DrawGizmos(GizmoColor);
    }

    void OnDrawGizmosSelected()
    {
        DrawGizmos(GizmoSelectedColor);
    }

    private void DrawGizmos(Color color)
    {
        Gizmos.color = color;
        Gizmos.matrix = Matrix4x4.identity * transform.localToWorldMatrix;
        switch (shape)
        {
            case Shape.Cube:
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                break;
            case Shape.Sphere:
                Gizmos.DrawWireSphere(Vector3.zero, 0.5f);
                break;
            case Shape.None:
                break;
        }
    }
}