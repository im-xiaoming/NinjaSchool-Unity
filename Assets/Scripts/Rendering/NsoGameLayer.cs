using UnityEngine;

// Hierarchy object "NSO Game Layer": the game's own drawing goes into a texture that is shown on a quad in front of
// the camera, so objects you add to the scene can be sorted behind or in front of the game (Sorting Order below).
// While no layer is enabled, or Draw Into Layer is off, the game draws straight to the screen as the original did.
public class NsoGameLayer : MonoBehaviour
{
    public static NsoGameLayer Active;
    [Header("Game presentation")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("drawIntoLayer")]
    private bool _drawIntoLayer = true;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("sortingOrder")]
    private int _sortingOrder;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("sortingLayerName")]
    private string _sortingLayerName = "Default";
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("opaque")]
    private bool _opaque = true;
    [Header("Camera / material")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetCamera")]
    private Camera _targetCamera;
    [Tooltip("Shader is assigned when the layer is enabled; restart Play to change it.")]
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("shader")]
    private Shader _shader;
    [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("distance"), Min(0.01f)]
    private float _distance = 10f;
    public bool drawIntoLayer
    {
        get
        {
            return _drawIntoLayer;
        }

        set
        {
            _drawIntoLayer = value;
        }
    }

    public int sortingOrder
    {
        get
        {
            return _sortingOrder;
        }

        set
        {
            _sortingOrder = value;
        }
    }

    public string sortingLayerName
    {
        get
        {
            return _sortingLayerName;
        }

        set
        {
            _sortingLayerName = value;
        }
    }

    public bool opaque
    {
        get
        {
            return _opaque;
        }

        set
        {
            _opaque = value;
        }
    }

    public Camera targetCamera
    {
        get
        {
            return _targetCamera;
        }

        set
        {
            _targetCamera = value;
        }
    }

    public Shader shader
    {
        get
        {
            return _shader;
        }

        set
        {
            _shader = value;
        }
    }

    public float distance
    {
        get
        {
            return _distance;
        }

        set
        {
            _distance = Mathf.Max(0.01f, value);
        }
    }

    [Header("Anti-aliasing (whole game picture)")]
    [Tooltip("Smooths jagged edges of the whole game picture with FXAA, one extra full-screen pass per frame.")]
    [SerializeField]
    private bool _antiAliasing = true;
    [Tooltip("FXAA shader (NSO/FXAA). Found by name when empty.")]
    [SerializeField]
    private Shader _fxaaShader;
    [Tooltip("Lower = more edges are smoothed (also softer). 0.0312 smooths the most.")]
    [SerializeField, Range(0.0312f, 0.0833f)]
    private float _contrastThreshold = 0.0312f;
    [Tooltip("Lower = more edges are smoothed, relative to the brightness around them.")]
    [SerializeField, Range(0.063f, 0.333f)]
    private float _relativeThreshold = 0.063f;
    [Tooltip("Blur strength on one-pixel details: 0 = only edges, 1 = softest.")]
    [SerializeField, Range(0f, 1f)]
    private float _subpixelBlending = 0.75f;
    public bool antiAliasing
    {
        get
        {
            return _antiAliasing;
        }

        set
        {
            _antiAliasing = value;
        }
    }

    // FXAA strength, set by the Settings menu (F6 > Hình ảnh > Khử răng cưa); clamped to the Inspector ranges
    public float contrastThreshold
    {
        get
        {
            return _contrastThreshold;
        }

        set
        {
            _contrastThreshold = Mathf.Clamp(value, 0.0312f, 0.0833f);
        }
    }

    public float relativeThreshold
    {
        get
        {
            return _relativeThreshold;
        }

        set
        {
            _relativeThreshold = Mathf.Clamp(value, 0.063f, 0.333f);
        }
    }

    public float subpixelBlending
    {
        get
        {
            return _subpixelBlending;
        }

        set
        {
            _subpixelBlending = Mathf.Clamp01(value);
        }
    }

    private void OnValidate()
    {
        _distance = Mathf.Max(0.01f, _distance);
    }

    private RenderTexture texture;
    private RenderTexture smoothTexture; // FXAA result shown on the quad while anti-aliasing is on
    private Material fxaaMaterial;
    private Material material;
    private MeshRenderer quadRenderer;
    private Transform quad;
    private Mesh mesh;
    private RenderTexture previousTarget;
    private Matrix4x4 savedMatrix;
    public RenderTexture Texture
    {
        get
        {
            return texture;
        }
    }

    private void OnEnable()
    {
        Active = this;
        BuildQuad();
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;
        if (quad != null)
            Destroy(quad.gameObject);
        if (mesh != null)
            Destroy(mesh);
        if (material != null)
            Destroy(material);
        if (texture != null)
        {
            texture.Release();
            Destroy(texture);
        }

        if (smoothTexture != null)
        {
            smoothTexture.Release();
            Destroy(smoothTexture);
        }

        if (fxaaMaterial != null)
            Destroy(fxaaMaterial);
        quad = null;
        mesh = null;
        material = null;
        texture = null;
        quadRenderer = null;
        smoothTexture = null;
        fxaaMaterial = null;
    }

    private void BuildQuad()
    {
        if (shader == null)
            shader = Shader.Find("NSO/Game Layer");
        material = new Material(shader)
        {
            name = "NSO Game Layer (instance)"
        };
        GameObject go = new GameObject("Game Quad");
        go.hideFlags = HideFlags.DontSave;
        go.transform.SetParent(transform, false);
        MeshFilter filter = go.AddComponent<MeshFilter>();
        mesh = new Mesh
        {
            name = "NSO Game Quad"
        };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f),
            new Vector3(0.5f, -0.5f),
            new Vector3(0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f)
        };
        mesh.uv = new[]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        mesh.triangles = new[]
        {
            0,
            2,
            1,
            0,
            3,
            2
        };
        filter.sharedMesh = mesh;
        quadRenderer = go.AddComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = material;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
        quad = go.transform;
    }

    private void LateUpdate()
    {
        if (quad == null)
            return;
        quadRenderer.enabled = drawIntoLayer && texture != null;
        quadRenderer.sortingOrder = sortingOrder;
        quadRenderer.sortingLayerName = sortingLayerName;
        material.SetFloat("_Opaque", opaque ? 1f : 0f);
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
            return;
        Transform ct = cam.transform;
        quad.position = ct.position + ct.forward * distance;
        quad.rotation = ct.rotation;
        float height = cam.orthographic ? cam.orthographicSize * 2f : 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        quad.localScale = new Vector3(height * cam.aspect, height, 1f);
    }

    // Called by Main.OnGUI around the game's paint. Returns true when the drawing is redirected into the layer.
    public static bool BeginPaint()
    {
        NsoGameLayer layer = Active;
        return layer != null && layer.drawIntoLayer && layer.Begin();
    }

    public static void EndPaint()
    {
        if (Active != null)
            Active.End();
    }

    private bool Begin()
    {
        int w = Screen.width, h = Screen.height;
        if (w <= 0 || h <= 0)
            return false;
        if (texture == null || texture.width != w || texture.height != h)
        {
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
            }

            // bilinear: FXAA reads between pixels; the quad shows it 1:1, so the plain view looks the same as Point
            texture = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
            {
                name = "NSO Game Layer",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1
            };
            texture.Create();
            material.mainTexture = texture;
        }

        previousTarget = RenderTexture.active;
        RenderTexture.active = texture;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, w, h, 0);
        GL.Clear(true, true, Color.clear);
        savedMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.identity;
        return true;
    }

    private void End()
    {
        GUI.matrix = savedMatrix;
        GL.PopMatrix();
        RenderTexture.active = previousTarget;
        ApplyAntiAliasing();
    }

    // FXAA pass: game texture -> smoothTexture, which the quad then shows. Off (or no shader) = the raw game texture.
    private void ApplyAntiAliasing()
    {
        if (antiAliasing && fxaaMaterial == null)
        {
            if (_fxaaShader == null)
                _fxaaShader = Shader.Find("NSO/FXAA");
            if (_fxaaShader != null && _fxaaShader.isSupported)
                fxaaMaterial = new Material(_fxaaShader)
                {
                    name = "NSO FXAA (instance)"
                };
        }

        if (!antiAliasing || fxaaMaterial == null || texture == null)
        {
            if (material.mainTexture != texture)
                material.mainTexture = texture;
            return;
        }

        if (smoothTexture == null || smoothTexture.width != texture.width || smoothTexture.height != texture.height)
        {
            if (smoothTexture != null)
            {
                smoothTexture.Release();
                Destroy(smoothTexture);
            }

            smoothTexture = new RenderTexture(texture.width, texture.height, 0, RenderTextureFormat.ARGB32)
            {
                name = "NSO Game Layer (FXAA)",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            smoothTexture.Create();
        }

        fxaaMaterial.SetFloat("_ContrastThreshold", _contrastThreshold);
        fxaaMaterial.SetFloat("_RelativeThreshold", _relativeThreshold);
        fxaaMaterial.SetFloat("_SubpixelBlending", _subpixelBlending);
        RenderTexture keep = RenderTexture.active; // Blit leaves its target active; the GUI must draw to the screen again
        Graphics.Blit(texture, smoothTexture, fxaaMaterial);
        RenderTexture.active = keep;
        if (material.mainTexture != smoothTexture)
            material.mainTexture = smoothTexture;
    }
}
