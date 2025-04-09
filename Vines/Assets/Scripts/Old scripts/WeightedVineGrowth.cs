using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class WeightedVineGrowth : MonoBehaviour
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

    public LineRenderer lineRenderer;
    public List<Vertex> vinePoints = new List<Vertex>();

    private Vector3 currentHighestPoint = Vector3.zero;
    private Vector3 currentHighestNormal = Vector3.zero;
    private bool foundHighest = false;
    private float searchRadius = 1.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generateVine();
    }

    // Update is called once per frame
    void Update()
    {
        if (vinePoints.Count <= 15 && vinePoints.Count > 0)
        {
            Vertex currentPoint = vinePoints[vinePoints.Count - 1];
            findNextPoint(currentPoint, 3.0f);
        }
    }

    void generateVine()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        addPoint(getGround(transform.position));
    }

    Vertex getGround(Vector3 startPosition)
    {
        Debug.Log(transform.position);

        RaycastHit hit;

        if (Physics.Raycast(startPosition + Vector3.up, Vector3.down, out hit, 10.0f))
        {
            Debug.Log("Hit point: " + hit.point);
            return new Vertex(hit.point, hit.normal);
        }
        Debug.Log("didn't find ground point");
        return new Vertex(hit.point, hit.normal);
    }

    void addPoint(Vertex newPoint)
    {
        if (vinePoints.Contains(newPoint)) // prevents duplicates
        {
            Debug.Log("Point already exists in vine");
            return;
        }

        vinePoints.Add((newPoint));
        lineRenderer.positionCount = vinePoints.Count;
        lineRenderer.SetPosition(vinePoints.Count - 1, newPoint.point);
        Debug.Log("Added point: " + newPoint.point + "to lineRenderer!");
    }

    void findNextPoint(Vertex currentPoint, float highestSearchRadius)
    {
        float maxY = float.MinValue;

        Vector3 bestPoint = Vector3.zero;
        Vector3 bestNormal = Vector3.zero;

        bool found = false;

        // if the current point is close enough to the highest point, find a new highest point to guide the vine
        if (foundHighest == true && (currentPoint.point.y - currentHighestPoint.y >= 0.0f || currentPoint.point.y - currentHighestPoint.y >= -0.1f))
        {
            // search for new highest point again
            Debug.Log("search for another highest point...");
            highestSearchRadius *= highestSearchRadius;
            foundHighest = false;
        }

        if (!foundHighest)
        {
            float highestMaxY = currentHighestPoint.y;

            // get sample points in radius and cast downward rays to find valid surface points
            for (int i = 0; i < 500; i++)
            {
                Vector3 highestRandomPoint = currentPoint.point + Random.insideUnitSphere * highestSearchRadius;
                highestRandomPoint.y += 1.0f;

                RaycastHit hit;
                if (Physics.Raycast(highestRandomPoint, Vector3.down, out hit, 20.0f))
                {
                    // check if this point is the highest valid point
                    if (hit.point.y >= highestMaxY && !vinePoints.Any(vp => vp.point == hit.point))
                    {
                        highestMaxY = hit.point.y;
                        currentHighestPoint = hit.point;
                        currentHighestNormal = hit.normal;
                    }
                }
            }

            if (currentHighestPoint != Vector3.zero)
            {
                Debug.Log("highest nearby point: " + currentHighestPoint);
                foundHighest = true;
            }
        }

        // get the growth direction using this point to guide the vine towards it
        Vector3 growthDirection = (currentHighestPoint - currentPoint.point).normalized;
        float bestScore = float.MinValue;

        // get sample points inside the radius
        for (int i = 0; i < 500; i++)
        {
            Vector3 randomPoint = currentPoint.point + Random.insideUnitSphere * searchRadius;
            randomPoint.y += 1.0f;

            RaycastHit hit;
            if (Physics.Raycast(randomPoint, Vector3.down, out hit, 20.0f))
            {
                // get distance from candidate point to currentHighestPoint
                float candidateDistance = Vector3.Distance(currentHighestPoint, randomPoint);

                // get alignment of candidate point direction from current point with growth direction
                Vector3 candidateDirection = (hit.point - currentPoint.point).normalized;
                float alignment = Vector3.Dot(candidateDirection, growthDirection);

                // get y value
                float candidateY = hit.point.y;

                // weighing system to rank possible next points
                float score = (1.0f / (1.0f + candidateDistance)) + (candidateY * 5.0f) + (alignment * 3.0f);

                if (score > bestScore)
                {
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                    found = true;
                }
            }
        }

        // if a valid surface point is found, add it
        if (found)
        {
            addPoint(new Vertex(bestPoint, bestNormal));
        }
        else
        {
            // raycast in growth direction from currentPoint.point
            RaycastHit hit;
            if (Physics.Raycast(currentPoint.point, growthDirection, out hit, 5.0f))
            {
                addPoint(new Vertex(hit.point, hit.normal));
            }
        }
    }
}
