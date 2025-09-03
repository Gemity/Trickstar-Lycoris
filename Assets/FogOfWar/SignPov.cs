using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SignPov : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private Vector2 _root;
    [SerializeField] private Vector2 _viewDir;
    [SerializeField] private LayerMask _obstacleMask;

    [Header("View")]
    [SerializeField] private float _viewRadius = 12f;
    [SerializeField] private float _viewAngle = 80f;
    [SerializeField] private bool _active = true;

    [Header("Sampling")]
    [SerializeField] private int _angleSubdiv = 2;        // extra offsets per vertex
    [SerializeField] private float _epsilonAngle = 0.0005f;// radians, small angular offset
    [SerializeField] private int _coarseRays = 32;        // backup ring if no vertices

    [Header("Limits & Debug")]
    [SerializeField] private int _maxAngles = 1024;       // hard cap for performance
    [SerializeField] private MeshFilter _debugMeshFilter; // optional preview mesh

    // Public read-only properties
    public bool Active => _active;
    public Mesh VisibilityMesh => _mesh;

    // Internal working buffers (reused to avoid GC)
    private readonly List<Vector2> _candidateVerts = new(256);
    private readonly List<float> _angles = new(512);
    private readonly List<Vector2> _hitPoints = new(512);
    private Mesh _mesh;

    private void OnEnable()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "SignPov_VisibilityMesh" };
            _mesh.MarkDynamic();
        }
        if (_debugMeshFilter) _debugMeshFilter.sharedMesh = _mesh;
    }

    private void OnDisable()
    {
        if (_mesh != null) _mesh.Clear();
    }

    private void LateUpdate()
    {
        _root = transform.position;
        _viewDir = transform.right;

        if (!_active) { if (_mesh != null) _mesh.Clear(); return; }
        BuildVisibilityMesh2D();
    }

    /// <summary> Set the POV position and view direction. </summary>
    public void SetView(Vector3 root, Vector3 dir)
    {
        _root = root;
        _viewDir = dir.normalized;
    }

    /// <summary>
    /// Rebuild visibility polygon around player using vertex-driven ray casting (2D XY).
    /// </summary>
    private void BuildVisibilityMesh2D()
    {
        // 1) Collect candidate vertices near origin
        CollectCandidateVertices();

        // 2) Build angle list from vertices with ±epsilon
        BuildAnglesFromVertices();

        // 3) Raycast each angle to get boundary points
        CastRays();

        // 4) Create triangle-fan mesh from origin
        BakeMesh();
    }

    // -------------------- Internals --------------------

    private void CollectCandidateVertices()
    {
        _candidateVerts.Clear();

        // ---- Configs ----
        const float dedupEps = 0.001f;                 // distance epsilon for dedup (world units)
        float dedupEps2 = dedupEps * dedupEps;
        float r = _viewRadius;
        float r2 = r * r;
        float margin = 1.1f;                           // 10% radius margin
        float r2WithMargin = r2 * margin * margin;

        bool limitedFov = _viewAngle < 359.9f;
        Vector2 vd = _viewDir.sqrMagnitude > 1e-8f ? _viewDir.normalized : Vector2.right;
        float halfFovDeg = _viewAngle * 0.5f;

        // ---- Local helpers ----
        bool PassesCull(Vector2 w)
        {
            Vector2 d = w - _root;
            if (d.sqrMagnitude > r2WithMargin) return false;
            if (!limitedFov) return true;
            float ang = Mathf.Abs(Vector2.SignedAngle(vd, d)); // degrees
            return ang <= (halfFovDeg + 0.5f);                  // small cushion to avoid edge popping
        }

        bool TryAddUnique(Vector2 w)
        {
            for (int i = 0; i < _candidateVerts.Count; i++)
                if ((_candidateVerts[i] - w).sqrMagnitude < dedupEps2) return false;
            _candidateVerts.Add(w);
            return true;
        }

        void AddPolyPathWorld(Transform t, Vector2 offset, Vector2[] localPath)
        {
            for (int i = 0; i < localPath.Length; i++)
            {
                Vector2 w = t.TransformPoint(localPath[i] + offset);
                if (PassesCull(w)) TryAddUnique(w);
            }
        }

        void AddBoxCorners(BoxCollider2D box)
        {
            var t = box.transform;
            Vector2 c = box.offset;
            Vector2 e = box.size * 0.5f; // local half extents
            Vector2[] cornersLocal = {
            c + new Vector2(-e.x, -e.y),
            c + new Vector2(-e.x,  e.y),
            c + new Vector2( e.x,  e.y),
            c + new Vector2( e.x, -e.y),
        };
            for (int i = 0; i < 4; i++)
            {
                Vector2 w = t.TransformPoint(cornersLocal[i]);
                if (PassesCull(w)) TryAddUnique(w);
            }
        }

        void AddCircleSamples(CircleCollider2D ccol, int samples = 12)
        {
            var t = ccol.transform;
            Vector2 c = ccol.offset;     // local center
                                         // sample in local circle, TransformPoint will create ellipse if non-uniform scale
            for (int i = 0; i < samples; i++)
            {
                float a = (Mathf.PI * 2f) * (i / (float)samples);
                Vector2 pLocal = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * ccol.radius;
                Vector2 w = t.TransformPoint(pLocal);
                if (PassesCull(w)) TryAddUnique(w);
            }
        }

        void AddCapsuleSamples(CapsuleCollider2D cap, int arcSamples = 6)
        {
            var t = cap.transform;
            Vector2 c = cap.offset;
            Vector2 h = cap.size * 0.5f; // local half size
            if (cap.direction == CapsuleDirection2D.Vertical)
            {
                float rx = h.x; float hy = h.y;
                // Top arc (π..0)
                for (int i = 0; i <= arcSamples; i++)
                {
                    float a = Mathf.PI * (i / (float)arcSamples);
                    Vector2 p = c + new Vector2(Mathf.Cos(a) * rx, hy + Mathf.Sin(a) * rx);
                    Vector2 w = t.TransformPoint(p);
                    if (PassesCull(w)) TryAddUnique(w);
                }
                // Bottom arc (0..π)
                for (int i = 0; i <= arcSamples; i++)
                {
                    float a = Mathf.PI * (i / (float)arcSamples);
                    Vector2 p = c + new Vector2(Mathf.Cos(a) * rx, -hy - Mathf.Sin(a) * rx);
                    Vector2 w = t.TransformPoint(p);
                    if (PassesCull(w)) TryAddUnique(w);
                }
            }
            else // Horizontal
            {
                float ry = h.y; float hx = h.x;
                // Right arc (-π/2..π/2)
                for (int i = 0; i <= arcSamples; i++)
                {
                    float a = -Mathf.PI * 0.5f + Mathf.PI * (i / (float)arcSamples);
                    Vector2 p = c + new Vector2(hx + Mathf.Sin(a) * ry, Mathf.Cos(a) * ry);
                    Vector2 w = t.TransformPoint(p);
                    if (PassesCull(w)) TryAddUnique(w);
                }
                // Left arc (π/2..3π/2)
                for (int i = 0; i <= arcSamples; i++)
                {
                    float a = Mathf.PI * 0.5f + Mathf.PI * (i / (float)arcSamples);
                    Vector2 p = c + new Vector2(-hx + Mathf.Sin(a) * ry, Mathf.Cos(a) * ry);
                    Vector2 w = t.TransformPoint(p);
                    if (PassesCull(w)) TryAddUnique(w);
                }
            }
        }

        // ---- Main pass: gather from colliders in range ----
        var cols = Physics2D.OverlapCircleAll(_root, _viewRadius, _obstacleMask);
        for (int ci = 0; ci < cols.Length; ci++)
        {
            var col = cols[ci];
            if (!col || !col.enabled || col.isTrigger) continue;

            if (col is PolygonCollider2D poly)
            {
                int pathCount = poly.pathCount;
                for (int p = 0; p < pathCount; p++)
                {
                    var path = poly.GetPath(p);                 // local
                    AddPolyPathWorld(poly.transform, poly.offset, path);
                }
            }
            else if (col is CompositeCollider2D comp)
            {
                int pathCount = comp.pathCount;
                for (int p = 0; p < pathCount; p++)
                {
                    int n = comp.GetPathPointCount(p);
                    var path = new Vector2[n];
                    comp.GetPath(p, path);                      // local
                    AddPolyPathWorld(comp.transform, comp.offset, path);
                }
            }
            else if (col is EdgeCollider2D edge)
            {
                var pts = edge.points;                          // local
                var t = edge.transform;
                for (int i = 0; i < pts.Length; i++)
                {
                    Vector2 w = t.TransformPoint(pts[i] + edge.offset);
                    if (PassesCull(w)) TryAddUnique(w);
                }
            }
            else if (col is BoxCollider2D box)
            {
                AddBoxCorners(box);
            }
            else if (col is CircleCollider2D circle)
            {
                AddCircleSamples(circle, 12);                   // 8–16 tuỳ map
            }
            else if (col is CapsuleCollider2D cap)
            {
                AddCapsuleSamples(cap, 6);
            }
            else
            {
                // Fallback: sample its local AABB corners via offset/size if possible… 
                // hoặc dùng world bounds (ít chính xác khi rotate), chỉ để có vài điểm thô:
                var b = col.bounds; // world AABB
                Vector2[] cornersW = {
                new Vector2(b.min.x, b.min.y), new Vector2(b.min.x, b.max.y),
                new Vector2(b.max.x, b.max.y), new Vector2(b.max.x, b.min.y),
            };
                for (int i = 0; i < 4; i++)
                    if (PassesCull(cornersW[i])) TryAddUnique(cornersW[i]);
            }
        }
    }


    private void BuildAnglesFromVertices()
    {
        _angles.Clear();
        for (int i = 0; i < _candidateVerts.Count; i++)
        {
            Vector2 to = _candidateVerts[i] - _root;
            if (to.sqrMagnitude < 1e-8f) continue;

            float baseA = Mathf.Atan2(to.y, to.x);
            PushAngle(baseA);
            PushAngle(baseA - _epsilonAngle);
            PushAngle(baseA + _epsilonAngle);

            for (int k = 1; k <= _angleSubdiv; k++)
            {
                float off = _epsilonAngle * (k + 1);
                PushAngle(baseA - off);
                PushAngle(baseA + off);
            }
        }

        // Safety ring if empty or too sparse
        if (_angles.Count < 8 && _coarseRays > 0)
            for (int i = 0; i < _coarseRays; i++)
                PushAngle((Mathf.PI * 2f) * (i / (float)_coarseRays));

        // Sort + dedup near-equal angles
        _angles.Sort();

        const float same = 1e-4f;
        for (int i = _angles.Count - 2; i >= 0; i--)
            if (Mathf.Abs(_angles[i + 1] - _angles[i]) < same) _angles.RemoveAt(i + 1);

        // Cap to max
        if (_angles.Count > _maxAngles) _angles.RemoveRange(_maxAngles, _angles.Count - _maxAngles);

        void PushAngle(float a)
        {
            // normalize to [0, 2π)
            float twoPi = Mathf.PI * 2f;
            if (a < 0) a = a + twoPi * (1 + Mathf.FloorToInt(-a / twoPi));
            if (a >= twoPi) a = a - twoPi * Mathf.FloorToInt(a / twoPi);
            _angles.Add(a);
        }
    }

    private void CastRays()
    {
        _hitPoints.Clear();
        _hitPoints.Capacity = Mathf.Max(_hitPoints.Capacity, _angles.Count);

        for (int i = 0; i < _angles.Count; i++)
        {
            float a = _angles[i];
            Vector2 dir = new(Mathf.Cos(a), Mathf.Sin(a));

            var hit = Physics2D.Raycast(_root, dir, _viewRadius, _obstacleMask);
            if (hit.collider)
                _hitPoints.Add(hit.point);
            else
                _hitPoints.Add(_root + dir * _viewRadius);
        }
    }

    private void BakeMesh()
    {
        int n = _hitPoints.Count;
        if (n < 3) { _mesh.Clear(); return; }

        // Build triangle fan (origin + ordered boundary points)
        var verts = new Vector3[n + 1];
        var tris = new int[n * 3];

        verts[0] = _root;
        for (int i = 0; i < n; i++) verts[i + 1] = _hitPoints[i];

        int t = 0;
        for (int i = 0; i < n; i++)
        {
            int a = 0;
            int b = i + 1;
            int c = (i + 1) <= n - 1 ? i + 2 : 1;
            tris[t++] = a; tris[t++] = c; tris[t++] = b;
        }

        _mesh.Clear();
        _mesh.SetVertices(verts);
        _mesh.SetTriangles(tris, 0);
        _mesh.RecalculateBounds();

        if (_debugMeshFilter && _debugMeshFilter.sharedMesh != _mesh)
            _debugMeshFilter.sharedMesh = _mesh;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_active || _mesh == null) return;
        Gizmos.color = new Color(1, 1, 0, 1f);
        Gizmos.DrawMesh(_mesh);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(_root, _root + _viewDir.normalized * _viewRadius);
    }
#endif
}
