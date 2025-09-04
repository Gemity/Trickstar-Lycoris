using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class SignPov : MonoBehaviour
{
    [Header("General")]
    [SerializeField] private LayerMask _obstacleMask;
    [SerializeField] private bool _active = true;

    [Header("View")]
    [SerializeField] private float _viewRadius = 12f;            // max sight distance
    [SerializeField] private float _viewAngle = 80f;             // degrees (full FOV)
    [SerializeField] private Vector2 _viewDir = Vector2.right;   // will be normalized

    [Header("Sampling")]
    [SerializeField] private int _raysNum = 32;                  // base rays inside sector (≥2)
    [SerializeField] private float _edgeEpsilonDeg = 0.25f;      // small offset outside sector edges (deg)

    [Header("Refinement")]
    [SerializeField] private bool _enableRefine = true;          // insert mid ray when large gap
    [SerializeField] private float _refineGap = 0.5f;            // world gap threshold
    [SerializeField] private int _refineMaxInsert = 64;          // safety cap

    [Header("Physics")]
    [SerializeField] private bool _useCircleCast = false;
    [SerializeField] private float _probeRadius = 0.05f;         // circlecast radius if enabled

    [Header("Update & Debug")]
    [SerializeField] private float _interval = 0.08f;            // seconds between updates
    [SerializeField] private bool _drawDebugRays = false;
    [SerializeField] private MeshFilter _debugMeshFilter;

    // Public read-only
    public bool Active => _active;
    public Mesh VisibilityMesh => _mesh;

    // Working buffers
    private readonly List<Vector2> _raysDir = new(512);
    private readonly List<Vector2> _hitPoints = new(512);
    private readonly List<bool> _hitByCollider = new(512);
    private Mesh _mesh;

    // Internal state
    [SerializeField, HideInInspector] private Vector2 _root;
    private float _timer;

    void OnEnable()
    {
        if (_mesh == null)
        {
            _mesh = new Mesh { name = "SignPov_VisibilityMesh" };
            _mesh.MarkDynamic();
        }
        if (_debugMeshFilter) _debugMeshFilter.sharedMesh = _mesh;
        _timer = Time.time;
    }

    void OnDisable()
    {
        if (_mesh != null) _mesh.Clear();
    }

    void LateUpdate()
    {
        if (!_active) { if (_mesh != null) _mesh.Clear(); return; }
        if (Time.time - _timer < _interval) return;

        _root = transform.position;
        _viewDir = (_viewDir.sqrMagnitude > 1e-8f ? _viewDir : (Vector2)transform.right).normalized;

        BuildVisibilityMesh2D();
        _timer = Time.time;
    }

    public void SetView(Vector3 root, Vector3 dir) { _root = root; _viewDir = ((Vector2)dir).normalized; }

    // ---------------- Core pipeline ----------------
    private void BuildVisibilityMesh2D()
    {
        BuildRayDirections();
        CastRays();
        if (_enableRefine) RefineGapsOnlyWhenBothHits();
        BakeMesh();
    }

    /// Build ray directions: left→right inside sector, then insert leftOut at 0 and append rightOut at end.
    private void BuildRayDirections()
    {
        _raysDir.Clear();

        int rays = Mathf.Max(2, _raysNum);
        float half = _viewAngle * 0.5f;
        float step = _viewAngle / (rays - 1);

        // Base rays inside sector (ordered left -> right)
        for (int i = 0; i < rays; i++)
        {
            float angle = -half + step * i;                 // degrees offset from viewDir
            Vector2 d = Quaternion.Euler(0, 0, angle) * _viewDir;
            _raysDir.Add(d.normalized);
        }

        // Edge rays just outside the sector: left at index 0, right at the end
        if (_edgeEpsilonDeg > 0.0001f && _viewAngle < 359.9f)
        {
            Vector2 leftOut = (Quaternion.Euler(0, 0, -half - _edgeEpsilonDeg) * _viewDir).normalized;
            Vector2 rightOut = (Quaternion.Euler(0, 0, half + _edgeEpsilonDeg) * _viewDir).normalized;

            _raysDir.Insert(0, leftOut);                    // left edge ray at index 0
            _raysDir.Add(rightOut);                         // right edge ray at last
        }
    }

    /// Cast rays (or circlecasts) and record both hit point and whether it hit a collider.
    private void CastRays()
    {
        _hitPoints.Clear();
        _hitByCollider.Clear();
        _hitPoints.Capacity = Mathf.Max(_hitPoints.Capacity, _raysDir.Count);
        _hitByCollider.Capacity = Mathf.Max(_hitByCollider.Capacity, _raysDir.Count);

        for (int i = 0; i < _raysDir.Count; i++)
        {
            Vector2 dir = _raysDir[i];
            bool hitCol;
            Vector2 p;

            if (_useCircleCast)
            {
                var hit = Physics2D.CircleCast(_root, _probeRadius, dir, _viewRadius, _obstacleMask);
                hitCol = hit.collider;
                p = hitCol ? hit.point : _root + dir * _viewRadius;
                if (_drawDebugRays) Debug.DrawRay(_root, dir * _viewRadius, hitCol ? Color.red : Color.green, _interval);
            }
            else
            {
                var hit = Physics2D.Raycast(_root, dir, _viewRadius, _obstacleMask);
                hitCol = hit.collider;
                p = hitCol ? hit.point : _root + dir * _viewRadius;
                if (_drawDebugRays) Debug.DrawRay(_root, dir * _viewRadius, hitCol ? Color.red : Color.green, _interval);
            }

            _hitPoints.Add(p);
            _hitByCollider.Add(hitCol);
        }
    }

    /// Refine only when BOTH adjacent points are collider hits; skip the wrap pair (last,0).
    private void RefineGapsOnlyWhenBothHits()
    {
        if (_hitPoints.Count < 3) return;

        float gap2 = _refineGap * _refineGap;
        int inserts = 0;

        // Go through linear neighbors; do NOT refine across the wrap (last,0)
        for (int i = 0; i < _hitPoints.Count - 1; i++)
        {
            if (inserts >= _refineMaxInsert) break;

            int j = i + 1;
            if (!_hitByCollider[i] || !_hitByCollider[j]) continue;          // refine only if both hit colliders

            if ((_hitPoints[i] - _hitPoints[j]).sqrMagnitude <= gap2) continue;

            // Mid-direction between two rays
            Vector2 midDir = (_raysDir[i] + _raysDir[j]).normalized;
            bool midHitCol;
            Vector2 midHit;

            if (_useCircleCast)
            {
                var mid = Physics2D.CircleCast(_root, _probeRadius, midDir, _viewRadius, _obstacleMask);
                midHitCol = mid.collider;
                midHit = midHitCol ? mid.point : _root + midDir * _viewRadius;
            }
            else
            {
                var mid = Physics2D.Raycast(_root, midDir, _viewRadius, _obstacleMask);
                midHitCol = mid.collider;
                midHit = midHitCol ? mid.point : _root + midDir * _viewRadius;
            }

            _raysDir.Insert(j, midDir);
            _hitPoints.Insert(j, midHit);
            _hitByCollider.Insert(j, midHitCol);

            inserts++;
            i++; // skip the inserted one on next iteration
        }
    }

    /// Build triangle-fan mesh with correct winding (camera at z<0 looking +Z).
    private void BakeMesh()
    {
        int n = _hitPoints.Count;
        if (n < 3) { _mesh.Clear(); return; }

        var verts = new Vector3[n + 1];
        var tris = new int[n * 3];
        var norms = new Vector3[n + 1];

        verts[0] = _root;
        norms[0] = Vector3.forward;

        for (int i = 0; i < n; i++) { verts[i + 1] = _hitPoints[i]; norms[i + 1] = Vector3.forward; }

        int t = 0;
        for (int i = 0; i < n; i++)
        {
            int a = 0;
            int b = i + 1;
            int c = (i + 1) <= n - 1 ? i + 2 : 1;
            tris[t++] = a; tris[t++] = c; tris[t++] = b;   // a,c,b → face +Z if directions ordered CCW
        }

        _mesh.Clear();
        _mesh.SetVertices(verts);
        _mesh.SetNormals(norms);
        _mesh.SetTriangles(tris, 0);
        _mesh.RecalculateBounds();

        if (_debugMeshFilter && _debugMeshFilter.sharedMesh != _mesh) _debugMeshFilter.sharedMesh = _mesh;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!_active || _mesh == null) return;
        Gizmos.color = new Color(1, 1, 0, 0.6f); Gizmos.DrawMesh(_mesh);
        Gizmos.color = Color.cyan; Gizmos.DrawLine(_root, _root + _viewDir.normalized * _viewRadius);
    }
#endif
}
