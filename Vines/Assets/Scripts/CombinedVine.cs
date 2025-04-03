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
    public float leafProbability = 0.5f;
    public GameObject leafPrefab;
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

    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mr = GetComponent<MeshRenderer>();
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
        if (!stopVine && vinePoints.Count < 100)
        {
            Vertex currentPoint = vinePoints.Last();
            FindNextPoint(currentPoint);
            GenerateMesh();
        }
        else
        {
            if (generateLeaves)
            {
                
                SmoothVine();
                GenerateMesh();
                GenerateLeaves();
                generateLeaves = false;
            }
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

    void AddPoint(Vertex newPoint)
    {
        if (vinePoints.Any(vp => Vector3.Distance(vp.point, newPoint.point) < 0.001f))
        {
            return;
        }
        vinePoints.Add(newPoint);
        Debug.Log("added point: " +  newPoint.point);
    }

    List<Vector3> GetNearbyMeshPoints(Vector3 currentPoint, float radius)
    {
        List<Vector3> nearbyPoints = new List<Vector3>();

        Vector3[] vertices = environmentMeshFilter.mesh.vertices;

        if (environmentMesh != null && environmentMeshFilter.mesh != null)
        {
            vertices = environmentMeshFilter.mesh.vertices;
        }
        else
        {
            Debug.LogWarning("Mesh is not assigned or mesh is empty.");
        }

        foreach (Vector3 vertex in vertices)
        {
            Vector3 worldVertex = environmentMesh.transform.TransformPoint(vertex);

            if (Vector3.Distance(worldVertex, currentPoint) <= radius)
            {
                nearbyPoints.Add(worldVertex);
            }
        }

        return nearbyPoints;
    }

    void FindNextPoint(Vertex currentPoint)
    {
        Vector3 prevPos = currentPoint.point;

        // Get the nearby mesh points around the current vine point and remove duplicates
        List<Vector3> nearbyPoints = GetNearbyMeshPoints(prevPos, searchRadius).Distinct().ToList();

        string nearbyPointsList = "Nearby Points: ";
        foreach (Vector3 point in nearbyPoints)
        {
            nearbyPointsList += point.ToString() + ", "; // Append each point to the string
        }

        // Remove the last comma and space if there are points listed
        if (nearbyPointsList.Length > 0)
            nearbyPointsList = nearbyPointsList.Substring(0, nearbyPointsList.Length - 2);

        Debug.Log(nearbyPointsList);

        float bestScore = float.MinValue;
        Vector3 bestPoint = prevPos;

        // Light position to calculate sun alignment (using the direction to the light)
        Vector3 lightPosition = lightTransform.position;
        Vector3 toLight = (lightPosition - prevPos).normalized; // Direction from vine point to light source

        // Flag to determine if the vine should go down
        bool shouldMoveDown = true;

        // Check if there are any candidates that are equal or higher than the current position
        foreach (Vector3 candidate in nearbyPoints)
        {
            if (candidate == prevPos || Vector3.Distance(prevPos, candidate) < 0.001f)
                continue;  // Skip if the candidate is the same as the previous point

            // Compute vertical movement (Y difference)
            float verticalMovement = candidate.y - prevPos.y;

            // If any candidate point is higher or equal, set shouldMoveDown to false
            if (verticalMovement >= 0)
            {
                shouldMoveDown = false; // There's no need to go down if there are candidates higher or at the same height
            }
        }

        // Iterate over nearby points to choose the best one based on scoring
        foreach (Vector3 candidate in nearbyPoints)
        {
            if (candidate == prevPos || Vector3.Distance(prevPos, candidate) < 0.001f)
                continue;  // Skip if the candidate is the same as the previous point

            // Compute vertical movement (Y difference)
            float verticalMovement = candidate.y - prevPos.y;

            // Check if we should allow downward movement or only upwards
            if (shouldMoveDown && verticalMovement >= 0) // Allow downward movement only if no higher candidates
            {
                // If we're allowing downward movement, apply a smaller penalty
                verticalMovement = Mathf.Clamp(verticalMovement * 0.5f, -1f, 0f);
            }

            // Lambert's Cosine Law: Compute the dot product of the surface normal and the light direction
            Vector3 surfaceNormal = vinePoints.FirstOrDefault(vp => vp.point == prevPos).normal;
            float sunFactor = Mathf.Clamp01(Vector3.Dot(surfaceNormal, toLight));

            // If the sun factor is very low, we can reduce its impact by scaling it up slightly
            if (sunFactor < 0.2f)  // If the sun factor is very low, increase its weight
            {
                sunFactor = Mathf.Clamp01(sunFactor * 2f);  // Increase the sun factor's impact on the score
            }

            float heightFactor;
            if (verticalMovement < 0)
            {
                heightFactor = Mathf.Clamp01(verticalMovement * 0.1f);
            }
            else
            {
                heightFactor = Mathf.Clamp01(verticalMovement * 2f);
            }

            Debug.Log("Sun Factor: " + sunFactor);
            Debug.Log("Height Factor: " + heightFactor);

            // Compute a score based on sun alignment and vertical movement
            float score = (sunFactor * 2.0f) + (heightFactor * 2.0f);

            if (score > bestScore)
            {
                bestScore = score;
                bestPoint = candidate;
            }
        }

        if (bestScore != float.MinValue)
        {
            if (Vector3.Distance(bestPoint, vinePoints.Last().point) > 0.001f)
            {
                AddPoint(new Vertex(bestPoint, Vector3.up));
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
        // Create a new list for the smoothed points
        List<Vector3> smoothedPoints = new List<Vector3>();

        // Loop through the vine points and generate intermediate points using Catmull-Rom spline
        for (int i = 1; i < vinePoints.Count - 2; i++)  // Start from second point and stop before the last two points
        {
            Vector3 p0 = vinePoints[i - 1].point;  // Previous point
            Vector3 p1 = vinePoints[i].point;      // Current point
            Vector3 p2 = vinePoints[i + 1].point;  // Next point
            Vector3 p3 = vinePoints[i + 2].point;  // Next next point

            // Interpolate between p1 and p2 using the Catmull-Rom spline
            for (float t = 0; t <= 1; t += 0.1f)  // Increase the step for smoother or rougher interpolation
            {
                Vector3 smoothedPoint = CatmullRom(p0, p1, p2, p3, t);
                smoothedPoints.Add(smoothedPoint);
            }
        }

        // Now replace the old vine points with the smoothed ones
        vinePoints.Clear();
        for (int i = 0; i < smoothedPoints.Count; i++)
        {
            AddPoint(new Vertex(smoothedPoints[i], Vector3.up));  // You can adjust the normal vector if needed
        }

        Debug.Log("Smoothing Complete");
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        // Catmull-Rom spline interpolation formula
        return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t);
    }


    void GenerateMesh()
    {
        if (vinePoints.Count < 2)
            return;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // We'll store the previous ring rotation to interpolate smoothly
        Quaternion lastRingRotation = Quaternion.identity;

        for (int i = 0; i < vinePoints.Count; i++)
        {
            float t = (float)i / (vinePoints.Count - 1);
            float radius = Mathf.Lerp(startRadius, endRadius, t);

            // Offset the center along the vine's normal to help prevent clipping.
            Vector3 centerWorld = vinePoints[i].point + vinePoints[i].normal * radius;
            Vector3 centerLocal = transform.InverseTransformPoint(centerWorld);

            // Compute the tangent using adjacent vine points.
            Vector3 tangentLocal;
            if (i < vinePoints.Count - 1)
            {
                Vector3 nextWorld = vinePoints[i + 1].point;
                Vector3 nextLocal = transform.InverseTransformPoint(nextWorld);
                tangentLocal = (nextLocal - centerLocal).normalized;
            }
            else
            {
                Vector3 prevWorld = vinePoints[i - 1].point;
                Vector3 prevLocal = transform.InverseTransformPoint(prevWorld);
                tangentLocal = (centerLocal - prevLocal).normalized;
            }

            // Choose an up reference that isn’t too aligned with the tangent.
            Vector3 upRef = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(tangentLocal, upRef)) > 0.99f)
                upRef = Vector3.right;

            // Compute the target rotation for the ring.
            Quaternion targetRotation = Quaternion.LookRotation(tangentLocal, upRef);

            // Smoothly interpolate with the previous ring's rotation.
            Quaternion ringRotation = (i == 0) ? targetRotation : Quaternion.Slerp(lastRingRotation, targetRotation, 0.5f);
            lastRingRotation = ringRotation;

            // Generate ring vertices using the interpolated rotation.
            for (int j = 0; j < circleDivisions; j++)
            {
                float angle = 2 * Mathf.PI * j / circleDivisions;
                // Create a 2D point on a circle (lying in the ring's plane)
                Vector3 localOffset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                // Rotate the offset into the correct orientation for this ring.
                Vector3 offset = ringRotation * localOffset;
                vertices.Add(centerLocal + offset);
                uvs.Add(new Vector2((float)j / circleDivisions, t * uvTileFactor));
            }
        }

        // Build triangles by connecting consecutive rings.
        int ringCount = vinePoints.Count;
        for (int i = 0; i < ringCount - 1; i++)
        {
            int ringStart = i * circleDivisions;
            int nextRingStart = (i + 1) * circleDivisions;
            for (int j = 0; j < circleDivisions; j++)
            {
                int current = ringStart + j;
                int next = ringStart + ((j + 1) % circleDivisions);
                int currentNext = nextRingStart + j;
                int nextNext = nextRingStart + ((j + 1) % circleDivisions);

                triangles.Add(current);
                triangles.Add(next);
                triangles.Add(currentNext);

                triangles.Add(next);
                triangles.Add(nextNext);
                triangles.Add(currentNext);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "VineMesh";
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        mf.mesh = mesh;
    }

    void GenerateLeaves()
    {
        Debug.Log("Generating leaves...");
        for (int i = 0; i < vinePoints.Count; i++)
        {
            if (Random.value <= leafProbability)
            {
                Debug.Log("Leaf generated at: " + vinePoints[i].point);
                SpawnLeafAtPoint(i);
            }
        }
    }

    void SpawnLeafAtPoint(int i)
    {
        Vector3 centerWorld = vinePoints[i].point;
        Vector3 surfaceNormal = vinePoints[i].normal;

        float radius = Mathf.Lerp(startRadius, endRadius, (float)i / (vinePoints.Count - 1));
        Vector3 leafOffset = surfaceNormal * radius;

        Vector3 randomDeviation = Random.insideUnitSphere * 0.02f;
        Vector3 leafPosition = centerWorld + leafOffset + randomDeviation;

        float deviationAngle = 5f;
        Vector3 randomAxis = Vector3.Cross(surfaceNormal, Random.onUnitSphere);
        if (randomAxis.sqrMagnitude < 0.001f)
            randomAxis = Vector3.right;
        randomAxis.Normalize();
        Quaternion deviationRotation = Quaternion.AngleAxis(Random.Range(-deviationAngle, deviationAngle), randomAxis);
        Vector3 adjustedUp = deviationRotation * surfaceNormal;

        Vector3 randomForwardCandidate = Random.onUnitSphere;
        Vector3 leafForward = Vector3.ProjectOnPlane(randomForwardCandidate, adjustedUp).normalized;
        if (leafForward == Vector3.zero)
            leafForward = Vector3.forward;

        Quaternion leafRotation = Quaternion.LookRotation(leafForward, adjustedUp);

        Vector3 centerLocal = transform.InverseTransformPoint(leafPosition);
        GameObject leaf = Instantiate(leafPrefab, transform);
        leaf.transform.localPosition = centerLocal;
        leaf.transform.localRotation = leafRotation;

        float baseScale = 0.005f;
        float randomScale = Random.Range(0.6f, 1.4f);
        leaf.transform.localScale = Vector3.one * (baseScale * randomScale);
    }

}
