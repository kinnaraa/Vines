using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    public float leafProbability = 0.4f;
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

    private float growthSpeed = 0.1f;
    private float growthTimer = 0f;

    List<Vertex> smoothedPoints = new List<Vertex>();

    List<Vector3> worldVertices = new List<Vector3>();
    List<Vector3> worldNormals = new List<Vector3>();


    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        environmentMesh = GameObject.Find("default");
        lightTransform = GameObject.Find("Sun").transform;
        environmentMeshFilter = environmentMesh.GetComponent<MeshFilter>();

        Vector3[] localVertices = environmentMeshFilter.mesh.vertices;
        Vector3[] localNormals = environmentMeshFilter.mesh.normals;

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

        AddPoint(GetGround(transform.position));
        GenerateMesh();
    }

    void Update()
    {
        if (vinePoints.Count > 0 && !stopVine)
        {
            growthTimer += Time.deltaTime;

            if (growthTimer >= growthSpeed)
            {
                Vertex currentPoint = vinePoints.Last();
                FindNextPoint(currentPoint);
                SmoothVine();
                growthTimer = 0f;
            }
            GenerateMesh();
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


    Vertex GetGround(Vector3 startPosition)
    {
        RaycastHit hit;
        if (Physics.Raycast(startPosition + Vector3.up, Vector3.down, out hit, 10.0f))
        {
            return new Vertex(hit.point, hit.normal);
        }
        return new Vertex(startPosition, Vector3.up);
    }

    public void AddPoint(Vertex newPoint)
    {
        if (vinePoints.Any(vp => Vector3.Distance(vp.point, newPoint.point) < 0.001f))
        {
            stopVine = true;
            return;
        }
        vinePoints.Add(newPoint);

        if(vinePoints.Count > 4)
        {
            SpawnLeavesAtPoint(newPoint);
        }
    }

    public void RedoLeaves()
    {
        Debug.Log("redoing leaves");

        // destroy old leaves
        foreach (var leaf in spawnedLeaves)
            Destroy(leaf);
        spawnedLeaves.Clear();

        if (!generateLeaves)
            return;

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
            // get nearby points only
            if (Vector3.Distance(worldVertices[i], currentPoint) <= radius)
            {
                nearbyVertices.Add(new Vertex(worldVertices[i], worldNormals[i]));
            }
        }

        return nearbyVertices;
    }

    void AddMidpoint(Vertex lastPoint, Vertex newPoint)
    {
        Vector3 midpoint = (lastPoint.point + newPoint.point) * 0.5f;
        Vector3 averageNormal = (lastPoint.normal + newPoint.normal) * 0.5f;

        float sphereRadius = 0.05f;
        Collider[] hits = Physics.OverlapSphere(midpoint, sphereRadius);
        bool inEnvironment = false;

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == environmentMesh)
            {
                inEnvironment = true;
                break;
            }
        }

        if (inEnvironment)
        {
            midpoint += averageNormal * 0.05f;
        }

        AddPoint(new Vertex(midpoint, averageNormal));
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
                AddMidpoint(currentPoint, newPoint);
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

    void SmoothVine()
    {
        if (vinePoints.Count() >= 6)
        {
            // new list for smoothed points
            smoothedPoints = new List<Vertex>();

            // catmull-rom for interpolating between vinePoints
            for (int i = 1; i < vinePoints.Count - 2; i++)  // start from second point and stop before the last two points
            {
                Vector3 p0 = vinePoints[i - 1].point;  // prev
                Vector3 p1 = vinePoints[i].point;      // current
                Vector3 p2 = vinePoints[i + 1].point;  // next
                Vector3 p3 = vinePoints[i + 2].point;  // next next

                // interpolate between p1 and p2 using catmull-rom spline
                for (float t = 0; t <= 1; t += 0.05f)
                {
                    Vector3 smoothedPoint = CatmullRom(p0, p1, p2, p3, t);
                    
                        smoothedPoints.Add(new Vertex(smoothedPoint, Vector3.up));
                }
            }
        }
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // catmull-rom spline interpolation formula
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }


    void GenerateMesh()
{
        if (smoothedPoints.Count < 2)
            return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Vector3 lastValidTangent = Vector3.forward;

        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            // get progress
            float t = (float)i / (smoothedPoints.Count - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);
        
            // convert smoothed point from world space to local space
            Vector3 centerWorld = smoothedPoints[i].point;
            Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);
        
            // compute tangent for orienting the ring
            Vector3 tangentLocal = Vector3.zero;

            if (i < smoothedPoints.Count - 1)
            {
                Vector3 nextLocal = transform.InverseTransformPoint(smoothedPoints[i + 1].point);
                tangentLocal = nextLocal - centerLocal;
            }
            else
            {
                Vector3 prevLocal = transform.InverseTransformPoint(smoothedPoints[i - 1].point);
                tangentLocal = centerLocal - prevLocal;
            }

            if (tangentLocal.magnitude < 0.0001f)
            {
                tangentLocal = lastValidTangent;
            }
            else
            {
                tangentLocal.Normalize();
                lastValidTangent = tangentLocal;
            }

                tangentLocal.Normalize();

            Vector3 arbitrary = Vector3.up;

            if (Mathf.Abs(Vector3.Dot(arbitrary, tangentLocal)) > 0.99f)
            {
                arbitrary = Vector3.forward;
            }
            
            Vector3 binormal = Vector3.Cross(tangentLocal, arbitrary).normalized;
            Vector3 computedNormal = Vector3.Cross(binormal, tangentLocal).normalized;

            Quaternion currentIdealRotation = Quaternion.LookRotation(tangentLocal, computedNormal);
            Quaternion ringRotation = currentIdealRotation;

            // adjust center to avoid clipping
            Vector3 adjustedCenterLocal = centerLocal + transform.InverseTransformDirection(computedNormal) * radius * -0.05f;

            // ring of verticies around the point
            for (int j = 0; j < circleDivisions; j++)
            {
                float angle = 2f * Mathf.PI * j / circleDivisions;
                Vector3 localOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                Vector3 offset = ringRotation * localOffset;
                vertices.Add(adjustedCenterLocal + offset);

                // uv mapping
                uvs.Add(new Vector2((float)j / circleDivisions, t * uvTileFactor));
            }
    }

        // build triangles!
        for (int i = 0; i < smoothedPoints.Count - 1; i++)
        {
            int ringStart = i * circleDivisions;
            int nextRingStart = (i + 1) * circleDivisions;
            for (int j = 0; j < circleDivisions; j++)
            {
                int current = ringStart + j;
                int next = ringStart + ((j + 1) % circleDivisions);
                int nextRingCurrent = nextRingStart + j;
                int nextRingNext = nextRingStart + ((j + 1) % circleDivisions);

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(nextRingCurrent);

                triangles.Add(next);
                triangles.Add(nextRingNext);
                triangles.Add(nextRingCurrent);
            }
    }

    Mesh mesh = new Mesh();

    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    mesh.name = "Vine Mesh";
    mesh.SetVertices(vertices);
    mesh.SetTriangles(triangles, 0);
    mesh.SetUVs(0, uvs);
    mesh.RecalculateNormals();
    mf.mesh = mesh;
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
        CleanUpLeaves();
    }

}


