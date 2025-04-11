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
    public float leafProbability = 0.7f;
    public GameObject leafPrefab;
    public List<GameObject> leaves = new List<GameObject>();
    public bool generateLeaves = true;

    [Header("Stuff")]
    public List<Vertex> vinePoints = new List<Vertex>();
    public GameObject environmentMesh;
    public MeshFilter environmentMeshFilter;

    [Header("Vine growth stuff")]
    public float searchRadius = 1.0f;
    public bool stopVine = false;

    [Header("Mesh tube stuff")]
    public int circleDivisions = 24;
    public float startRadius = 0.06f;
    public float endRadius = 0.005f;
    public float uvTileFactor = 10.0f;
    public Texture2D vineTexture;

    MeshFilter mf;
    MeshRenderer mr;

    [Header("Light stuff")]
    public Transform lightTransform;

    private float growthSpeed = 0.05f;
    private float growthTimer = 0f;

    List<Vertex> smoothedPoints = new List<Vertex>();

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
        environmentMesh = GameObject.Find("default");
        lightTransform = GameObject.Find("Sun").transform;
        environmentMeshFilter = environmentMesh.GetComponent<MeshFilter>();

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
        if (vinePoints.Count > 0 && !stopVine && vinePoints.Count < 100)
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
            return;
        }
        vinePoints.Add(newPoint);
        SpawnLeavesAtPoint(newPoint);
    }

    List<Vertex> GetNearbyMeshPoints(Vector3 currentPoint, float radius)
    {
        List<Vertex> nearbyVertices = new List<Vertex>();
        Mesh mesh = environmentMeshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;

        for (int i = 0; i < vertices.Length; i++)
        {
            // get vertex data in world space
            Vector3 worldVertex = environmentMesh.transform.TransformPoint(vertices[i]);
            Vector3 worldNormal = environmentMesh.transform.TransformDirection(normals[i]).normalized;

            // get nearby points only
            if (Vector3.Distance(worldVertex, currentPoint) <= radius)
            {
                nearbyVertices.Add(new Vertex(worldVertex, worldNormal));
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
                AddPoint(new Vertex(bestPoint, bestnormal));
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
                for (float t = 0; t <= 1; t += 0.1f)
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

        // makes lists for vertices, triangles, and uvs
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Quaternion lastRingRotation = Quaternion.identity;

        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            // get t which is position along vine basically
            float t = (float)i / (smoothedPoints.Count - 1);

            // use t to get the right value between the start and end radius of the vine mesh
            float radius = Mathf.Lerp(startRadius, endRadius, t);

            // offset the center of the mesh by the radius so that it doesn't clip
            Vector3 centerWorld = smoothedPoints[i].point + smoothedPoints[i].normal * radius * -0.2f;
            Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);

            // if there is a next point, use that and the current point to get the tangent
            Vector3 tangentLocal;
            if (i < smoothedPoints.Count - 1)
            {
                Vector3 nextWorld = smoothedPoints[i + 1].point;
                Vector3 nextLocal = transform.InverseTransformPoint(nextWorld);
                tangentLocal = (nextLocal - centerLocal).normalized;
            }
            else // if there is no next point, use the previous one
            {
                Vector3 prevWorld = smoothedPoints[i - 1].point;
                Vector3 prevLocal = transform.InverseTransformPoint(prevWorld);
                tangentLocal = (centerLocal - prevLocal).normalized;
            }

            Vector3 upRef = Vector3.up;

            // get the current vine point's ring rotation based on the up vector and its tangent
            Quaternion currentIdealRotation = Quaternion.LookRotation(tangentLocal, upRef);

            // if it's the first point, just use the current rotation, if not, interpolate between the last ring and the current one
            Quaternion ringRotation;
            if (i == 0)
            {
                ringRotation = currentIdealRotation;
            }
            else
            {
                ringRotation = Quaternion.Slerp(lastRingRotation, currentIdealRotation, 0.5f);
            }

            // update so this is now the last ring rotation
            lastRingRotation = ringRotation;

            // get ring vertices by dividing circle into divisions and 
            for (int j = 0; j < circleDivisions; j++)
            {
                float angle = 2 * Mathf.PI * j / circleDivisions;
                // getting the 2d points of the circle
                Vector3 localOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

                // rotating the 2d points to the correct orientation in 3d
                Vector3 offset = ringRotation * localOffset;

                // add that offset to the center of the ring to get its position
                vertices.Add(centerLocal + offset);

                // get the uv where u is j / number of divisions and v is based on the t value and the tile factor
                uvs.Add(new Vector2((float)j / circleDivisions, t * uvTileFactor));
            }
        }

        // building triangles!!! yay!
        for (int i = 0; i < smoothedPoints.Count - 1; i++)
        {
            int ringStart = i * circleDivisions;
            int nextRingStart = (i + 1) * circleDivisions;
            for (int j = 0; j < circleDivisions; j++)
            {
                // dr peters always said modulo for wrapping !!
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
    }
}


