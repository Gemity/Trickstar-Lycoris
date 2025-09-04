using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class FogRenderer : MonoBehaviour
{
    [Header("World Bounds (XY)")]
    [SerializeField] private Rect _worldRect = new Rect(-25, -25, 50, 50);

    [Header("Render Texture")]
    [SerializeField] private int _textureSize = 512;        // 512 (mobile), 1024 (PC)
    [SerializeField] private FilterMode _filterMode = FilterMode.Bilinear;

    [Header("Materials")]
    [SerializeField] private Material _maskWriterMat;       // Fog/MaskWriter
    [SerializeField] private Material _compositeMat;        // Fog/Composite (set on overlay)

    [Header("Sources")]
    [SerializeField] private List<SignPov> _sources = new List<SignPov>(); // auto-fill if empty
    [SerializeField] private bool _autoFindSources = true;

    public RenderTexture FogTexture => _rt;
    public Rect WorldRect => _worldRect;

    private RenderTexture _rt;

    void OnEnable()
    {
        AllocateRT();
        if (_autoFindSources) RefreshSources();
        PushWorldParamsToMats();
    }

    void OnDisable()
    {
        ReleaseRT();
    }

    void OnValidate()
    {
        if (_textureSize < 64) _textureSize = 64;
        if (enabled) { AllocateRT(); PushWorldParamsToMats(); }
    }

    void LateUpdate()
    {
        if (_autoFindSources && (_sources == null || _sources.Count == 0)) RefreshSources();
        if (_maskWriterMat == null) return;

        // Clear RT to 1 (full fog)
        var prev = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.PushMatrix();
        GL.LoadPixelMatrix(0, _rt.width, 0, _rt.height);
        GL.Clear(true, true, new Color(1, 0, 0, 0)); // R8 → R=1 = fog; 0 = clear
        GL.PopMatrix();

        // Provide world→UV parameters to writer
        PushWorldParamsToMats();

        // Draw every SignPov mesh using MaskWriter (BlendOp Min keeps 0)
        _maskWriterMat.SetPass(0);
        if (_sources != null)
        {
            for (int i = 0; i < _sources.Count; i++)
            {
                var pov = _sources[i];
                if (pov == null || !pov.Active) continue;
                var mesh = pov.VisibilityMesh;
                if (mesh == null || mesh.vertexCount == 0) continue;

                // Mesh is in WORLD coordinates, so draw with identity matrix.
                Graphics.DrawMeshNow(mesh, Matrix4x4.identity);
            }
        }

        RenderTexture.active = prev;

        // Feed RT to composite material for overlay
        if (_compositeMat != null)
            _compositeMat.SetTexture("_FogTex", _rt);
    }

    public void RefreshSources()
    {
        _sources = new List<SignPov>(FindObjectsByType<SignPov>(FindObjectsSortMode.None));
    }

    public void SetWorldRect(Rect rect)
    {
        _worldRect = rect;
        PushWorldParamsToMats();
    }

    private void AllocateRT()
    {
        // Re-create if needed
        if (_rt != null && (_rt.width != _textureSize || _rt.height != _textureSize))
            ReleaseRT();

        if (_rt == null)
        {
            var fmt = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.R8)
                ? RenderTextureFormat.R8 : RenderTextureFormat.ARGB32;

            _rt = new RenderTexture(_textureSize, _textureSize, 0, fmt)
            {
                name = "FogMaskRT",
                filterMode = _filterMode,
                wrapMode = TextureWrapMode.Clamp,
                autoGenerateMips = false,
                useMipMap = false
            };
            _rt.Create();
        }
    }

    private void ReleaseRT()
    {
        if (_rt != null)
        {
            if (Application.isPlaying) Destroy(_rt);
            else DestroyImmediate(_rt);
            _rt = null;
        }
    }

    private void PushWorldParamsToMats()
    {
        if (_maskWriterMat != null)
        {
            _maskWriterMat.SetVector("_WorldMin", new Vector4(_worldRect.xMin, _worldRect.yMin, 0, 0));
            _maskWriterMat.SetVector("_WorldMax", new Vector4(_worldRect.xMax, _worldRect.yMax, 0, 0));
        }
        if (_compositeMat != null)
        {
            _compositeMat.SetVector("_WorldMin", new Vector4(_worldRect.xMin, _worldRect.yMin, 0, 0));
            _compositeMat.SetVector("_WorldMax", new Vector4(_worldRect.xMax, _worldRect.yMax, 0, 0));
        }
    }
}
