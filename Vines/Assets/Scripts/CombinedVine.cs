using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CombinedVine : MonoBehaviour
{
    public struct Vertex
    {
        public Vector3 point;
        public Vector3 normal;
        public Vertex(Vector3 point, Vector3 normal)
        {
            this.point = point;
            this.normal = normal;
        }
    }
    [Header("Leaf stuff")]
    public float leafProbability = 0.6f;
    public GameObject leafPrefab;
    public bool generateLeaves = true;
    private List<GameObject> spawnedLeaves = new List<GameObject>();


    public List<Vertex> vinePoints = new List<Vertex>();
    public GameObject environmentMesh;
    public MeshFilter environmentMeshFilter;

    [Header("Vine growth stuff")]
    public float searchRadius = 0.4f;
    public bool stopVine = false;

    [Header("Mesh tube stuff")]
    public int circleDivisions = 12;
    public float startRadius = 0.06f;
    public float endRadius = 0.003f;
    public float uvTileFactor = 10.0f;
    public Texture2D vineTexture;

    MeshFilter mf;
    MeshRenderer mr;

    [Header("Light stuff")]
    public Transform lightTransform;

    public float growthSpeed = 0.01f;
    private float growthTimer = 0f;

    List<Vertex> smoothedPoints = new List<Vertex>();

    List<Vector3> worldVertices = new List<Vector3>();
    List<Vector3> worldNormals = new List<Vector3>();

    Transform tr;
    Vector3[] localPts, tangents, normals;
    Vector3[] circleDirs;
    List<Vector3> verts;
    List<Vector2> uvs;
    List<int> tris;
    float surfaceOffset = 0.07f;
    private int revealedSmoothCount = 0;

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        environmentMesh = GameObject.Find("default");
        lightTransform = GameObject.Find("Sun").transform;
        environmentMeshFilter = environmentMesh.GetComponent<MeshFilter>();

        Vector3[] localVertices = environmentMeshFilter.mesh.vertices;
        Vector3[] localNormals = environmentMeshFilter.mesh.normals;

        tr = transform;
        verts = new List<Vector3>();
        uvs = new List<Vector2>();
        tris = new List<int>();

        circleDirs = new Vector3[circleDivisions];
        for (int j = 0; j < circleDivisions; j++)
        {
            float ang = 2f * Mathf.PI * j / circleDivisions;
            circleDirs[j] = new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f);
        }

        for (int i = 0; i < localVertices.Length; i++)
        {
            worldVertices.Add(environmentMesh.transform.TransformPoint(localVertices[i]));
            worldNormals.Add(environmentMesh.transform.TransformDirection(localNormals[i]).normalized);
        }

        if (mr.sharedMaterial == null)
        {
            Material mat = new Material(Shader.Find("Unlit/Texture"));
            if (vineTexture != null)
            {
                mat.mainTexture = vineTexture;
            }
            else
            {
                mat.color = new Color(0.3f, 0.15f, 0.05f);
            }
            mr.material = mat;
        }
    }

    void Update()
    {
        if (vinePoints.Count > 0 && !stopVine)
        {
            growthTimer += Time.deltaTime;
            if (growthTimer >= growthSpeed)
            {
                growthTimer = 0f;

                if (revealedSmoothCount < smoothedPoints.Count)
                {
                    revealedSmoothCount++;
                }
                else
                {
                    FindNextPoint(vinePoints.Last());
                }
            }

            GenerateMesh();
        }
    }

    void SmoothOverTime()
    {
        int c = vinePoints.Count;
        if (c < 4) return;

        var p0 = vinePoints[c - 4].point;
        var p1 = vinePoints[c - 3].point;
        var p2 = vinePoints[c - 2].point;
        var p3 = vinePoints[c - 1].point;

        for (float t = 0; t <= 1f; t += 0.075f)
        {
            var pt = CentripetalCR(p0, p1, p2, p3, t);
            smoothedPoints.Add(new Vertex(pt, Vector3.up));
        }
    }


    public void CleanUpLeaves()
    {
        var leaves = GameObject.FindGameObjectsWithTag("Leaf");

        foreach (var leaf in leaves)
        {
            bool hasRequiredParent = false;
            Transform p = leaf.transform.parent;
            while (p != null)
            {
                if (p.name == "Vine Spawn Point(Clone)")
                {
                    hasRequiredParent = true;
                    break;
                }
                p = p.parent;
            }

            if (!hasRequiredParent)
                Destroy(leaf);
        }
    }


    public void AddPoint(Vertex newPoint)
    {
        if (vinePoints.Any(vp => Vector3.Distance(vp.point - vp.normal.normalized * surfaceOffset, newPoint.point) < 0.001f))
        {
            stopVine = true;
            return;
        }

        newPoint.point += newPoint.normal.normalized * surfaceOffset;
        vinePoints.Add(newPoint);

        if (vinePoints.Count == 4)
        {
            smoothedPoints.Clear();
            SmoothOverTime();
            revealedSmoothCount = 0;
        }
        else if (vinePoints.Count > 4)
        {
            SmoothOverTime();
        }

        if (vinePoints.Count > 4)
        {
            StartCoroutine(spawnLeaf(newPoint));
        }
    }

    private IEnumerator spawnLeaf(Vertex newPoint)
    {
        yield return new WaitForSeconds(1);
        SpawnLeavesAtPoint(newPoint);
    }

    public void RedoLeaves()
    {
        // destroy old leaves
        foreach (var leaf in spawnedLeaves)
            Destroy(leaf);
        spawnedLeaves.Clear();

        // respawn leaves on every existing segment
        foreach (var vp in vinePoints.Skip(4))
            SpawnLeavesAtPoint(vp);
    }


    List<Vertex> GetNearbyMeshPoints(Vector3 currentPoint, float radius)
    {
        List<Vertex> nearbyVertices = new List<Vertex>();
        Mesh mesh = environmentMeshFilter.mesh;

        for (int i = 0; i < worldVertices.Count; i++)
        {
            if (Vector3.Distance(worldVertices[i], currentPoint) <= radius)
            {
                nearbyVertices.Add(new Vertex(worldVertices[i], worldNormals[i]));
            }
        }

        return nearbyVertices;
    }


    void FindNextPoint(Vertex currentPoint)
    {
        Vector3 prevPos = currentPoint.point;

        // get nearby points on mesh
        List<Vertex> nearbyPoints = GetNearbyMeshPoints(prevPos, searchRadius).Distinct().ToList();

        float bestScore = float.MinValue;
        Vector3 bestPoint = prevPos;
        Vector3 bestnormal = Vector3.up;

        // get direction "sun" is facing
        Vector3 lightDirection = -lightTransform.forward;

        // score each point based on light intensity and upward movement
        foreach (Vertex candidate in nearbyPoints)
        {
            if (candidate.point == prevPos || Vector3.Distance(prevPos, candidate.point) < 0.001f)
                continue;  // skip repeat points

            // get vertical movement value
            float verticalMovement = candidate.point.y - prevPos.y;

            // use lambert's cosine law (cosine of angle between surface normal and direction of light)
            Vector3 surfaceNormal = candidate.normal;
            float sunFactor = Mathf.Clamp01(Vector3.Dot(surfaceNormal.normalized, lightDirection.normalized));

            float heightFactor;
            if (verticalMovement < 0)
            {
                heightFactor = Mathf.Clamp01(verticalMovement * 0.5f);
            }
            else
            {
                heightFactor = Mathf.Clamp01(verticalMovement * 2f);
            }

            // get score based on sun intensity and height
            float score = (sunFactor * 2.0f) + (heightFactor * 2.0f);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidate.point;
                bestnormal = candidate.normal;
            }
        }

        // add new point if a new one was found, otherwise stop growth
        if (bestScore != float.MinValue)
        {
            if (Vector3.Distance(bestPoint, vinePoints.Last().point) > 0.001f)
            {
                Vertex newPoint = new Vertex(bestPoint, bestnormal);
                AddPoint(newPoint);
            }
            else
            {
                stopVine = true;
            }
        }
        else
        {
            stopVine = true;
        }
    }


    Vector3 CentripetalCR(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float a = Mathf.Pow((p1 - p0).sqrMagnitude, 0.25f);
        float b = Mathf.Pow((p2 - p1).sqrMagnitude, 0.25f);
        float c = Mathf.Pow((p3 - p2).sqrMagnitude, 0.25f);
        float t0 = 0.0f;
        float t1 = t0 + a;
        float t2 = t1 + b;
        float t3 = t2 + c;

        t = Mathf.Lerp(t1, t2, t);  // remap t into [t1,t2]
        Vector3 A1 = (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
        Vector3 A2 = (t2 - t) / (t2 - t1) * p1 + (t - t1) / (t2 - t1) * p2;
        Vector3 A3 = (t3 - t) / (t3 - t2) * p2 + (t - t2) / (t3 - t2) * p3;

        Vector3 B1 = (t2 - t) / (t2 - t0) * A1 + (t - t0) / (t2 - t0) * A2;
        Vector3 B2 = (t3 - t) / (t3 - t1) * A2 + (t - t1) / (t3 - t1) * A3;

        return (t2 - t) / (t2 - t1) * B1 + (t - t1) / (t2 - t1) * B2;
    }


    public void GenerateMesh()
    {
        int N = Mathf.Min(revealedSmoothCount, smoothedPoints.Count);
        if (N < 2) return;

        if (localPts == null || localPts.Length != N)
        {
            localPts = new Vector3[N];
            tangents = new Vector3[N];
            normals = new Vector3[N];
        }

        verts.Clear();
        uvs.Clear();
        tris.Clear();

        for (int i = 0; i < N; i++)
            localPts[i] = tr.InverseTransformPoint(smoothedPoints[i].point);

        Vector3 lastT = Vector3.forward;
        for (int i = 0; i < N; i++)
        {
            Vector3 a = localPts[i];
            Vector3 b = (i < N - 1) ? localPts[i + 1] : localPts[i - 1];
            Vector3 tng = b - a;
            if (tng.sqrMagnitude < 1e-8f)
                tng = lastT;
            else
            {
                tng.Normalize();
                lastT = tng;
            }
            tangents[i] = tng;
        }

        Vector3 up = Vector3.up;
        if (Mathf.Abs(Vector3.Dot(up, tangents[0])) > 0.99f)
            up = Vector3.forward;

        normals[0] = (up - Vector3.Dot(up, tangents[0]) * tangents[0]).normalized;

        for (int i = 1; i < N; i++)
        {
            Vector3 prevN = normals[i - 1];
            Vector3 tn = tangents[i];
            Vector3 proj = prevN - Vector3.Dot(prevN, tn) * tn;

            if (proj.sqrMagnitude < 1e-8f)
            {
                proj = Vector3.Cross(tn, Vector3.up);
                if (proj.sqrMagnitude < 1e-8f)
                    proj = Vector3.Cross(tn, Vector3.forward);
            }

            normals[i] = proj.normalized;
        }

        for (int i = 0; i < N; i++)
        {
            float t = (float)i / (N - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);

            Vector3 center = localPts[i];
            Quaternion rot = Quaternion.LookRotation(tangents[i], normals[i]);

            for (int j = 0; j < circleDivisions; j++)
            {
                Vector3 offset = rot * (circleDirs[j] * radius);
                verts.Add(center + offset);
                uvs.Add(new Vector2((float)j / circleDivisions, t * uvTileFactor));
            }
        }

        for (int i = 0; i < N - 1; i++)
        {
            int aBase = i * circleDivisions;
            int bBase = (i + 1) * circleDivisions;

            for (int j = 0; j < circleDivisions; j++)
            {
                int a = aBase + j;
                int b = aBase + ((j + 1) % circleDivisions);
                int c = bBase + j;
                int d = bBase + ((j + 1) % circleDivisions);

                tris.Add(a); tris.Add(b); tris.Add(c);
                tris.Add(b); tris.Add(d); tris.Add(c);
            }
        }

        var mesh = mf.sharedMesh ?? new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.Clear();
        mesh.SetVertices(verts);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        mf.sharedMesh = mesh;
    }



    void SpawnLeavesAtPoint(Vertex vinePoint)
    {
        int leafCount = Random.Range(2, 4); // spawn 2-3 leaves
        for (int i = 0; i < leafCount; i++)
        {
            if (Random.value <= leafProbability) // only spawn sometimes
            {
                SpawnLeafAtPosition(vinePoint.point, vinePoint.normal);
            }
        }
        CleanUpLeaves();
    }

    void SpawnLeafAtPosition(Vector3 point, Vector3 normal)
    {
        // get leaf spawn position using the normal and some offset
        float radius = Mathf.Lerp(startRadius, endRadius, 0.5f); 
        Vector3 leafOffset = normal * radius;
        Vector3 randomDeviation = Random.insideUnitSphere * 0.02f;
        Vector3 leafPosition = point + leafOffset + randomDeviation;

        // leaf rotation
        float deviationAngle = 5f;
        Vector3 randomAxis = Vector3.Cross(normal, Random.onUnitSphere);
        if (randomAxis.sqrMagnitude < 0.001f)
        {
            randomAxis = Vector3.right;
        }
        randomAxis.Normalize();
        Quaternion deviationRotation = Quaternion.AngleAxis(Random.Range(-deviationAngle, deviationAngle), randomAxis);
        Vector3 adjustedUp = deviationRotation * normal;

        Vector3 randomForwardCandidate = Random.onUnitSphere;
        Vector3 leafForward = Vector3.ProjectOnPlane(randomForwardCandidate, adjustedUp).normalized;
        if (leafForward == Vector3.zero)
        {
            leafForward = Vector3.forward;
        }

        Quaternion leafRotation = Quaternion.LookRotation(leafForward, adjustedUp);

        Vector3 centerLocal = transform.InverseTransformPoint(leafPosition);
        GameObject leaf = Instantiate(leafPrefab, transform);
        leaf.transform.localPosition = centerLocal;
        leaf.transform.localRotation = leafRotation;

        // scaling
        float baseScale = 0.005f;
        float randomScale = Random.Range(0.6f, 1.4f);
        leaf.transform.localScale = Vector3.one * (baseScale * randomScale);

        spawnedLeaves.Add(leaf);
    }

}


