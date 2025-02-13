using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class VineGrowth3 : MonoBehaviour
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
    private float maxDistance = 3.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generateVine();
    }

    // Update is called once per frame
    void Update()
    {
        if (vinePoints.Count <= 20 && vinePoints.Count > 0)
        {
            Vertex currentPoint = vinePoints[vinePoints.Count - 1];
            findNextPoint(currentPoint);
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
            enabled = false;
            return;
        }

        vinePoints.Add((newPoint));
        lineRenderer.positionCount = vinePoints.Count;
        lineRenderer.SetPosition(vinePoints.Count - 1, newPoint.point);
        Debug.Log("Added point: " + newPoint.point + "to lineRenderer!");
    }

    void findNextPoint(Vertex currentPoint)
    {
        float searchRadius = 1.0f;
        float maxY = float.MinValue;
        Vector3 bestPoint = Vector3.zero;
        Vector3 bestNormal = Vector3.zero;
        bool found = false;

        // check large radius to get nearby highest point
        Vector3 highestPoint = Vector3.zero;
        Vector3 highestNormal = Vector3.zero;
        float highestMaxY = float.MinValue;

        // get sample points in radius and cast downward rays to find valid surface points
        for (int i = 0; i < 50; i++)
        {
            Vector3 highestRandomPoint = currentPoint.point + Random.insideUnitSphere;
            highestRandomPoint.y += 1.0f;

            RaycastHit hit;
            if (Physics.Raycast(highestRandomPoint, Vector3.down, out hit, 5.0f))
            {
                // check if this point is the highest valid point
                if (hit.point.y > highestMaxY && !vinePoints.Any(vp => vp.point == hit.point))
                {
                    highestMaxY = hit.point.y;
                    highestPoint = hit.point;
                    highestNormal = hit.normal;
                }
            }
        }

        Debug.Log("highest nearby point: " + highestPoint);

        // get the growth direction using this point to guide the vine towards it
        Vector3 growthDirection = (highestPoint - currentPoint.point).normalized;

        // get sample points inside the collider bounds
        for (int i = 0; i < 50; i++)
        {
            Vector3 randomPoint = currentPoint.point + Random.insideUnitSphere;
            randomPoint.y += 1.0f;

            RaycastHit hit;
            if (Physics.Raycast(randomPoint, Vector3.down, out hit, 4.0f))
            {
                // check if the candidate point is somewhat in the right growth direction
                Vector3 candidateDirection = (hit.point - currentPoint.point).normalized;
                float alignment = Vector3.Dot(candidateDirection, growthDirection);

                // check if this point is the highest valid point
                if (alignment > 0.5f && hit.point.y > maxY && !vinePoints.Any(vp => vp.point == hit.point))
                {
                    maxY = hit.point.y;
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                    found = true;
                }
            }
        }

        // if a valid surface point is found, add it
        if (found)
        {
            Debug.Log("selected point: " + bestPoint);
            addPoint(new Vertex(bestPoint, bestNormal));
        }
    }
}
