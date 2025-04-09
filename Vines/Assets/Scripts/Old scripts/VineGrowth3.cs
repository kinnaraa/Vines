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

    private Vector3 currentHighestPoint = Vector3.zero;
    private Vector3 currentHighestNormal = Vector3.zero;
    private bool foundHighest = false;
    private float searchRadius = 0.5f;
    private float highestSearchRadius = 3.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        generateVine();
    }

    // Update is called once per frame
    void Update()
    {
        if (vinePoints.Count <= 100 && vinePoints.Count > 0)
        {
            Vertex currentPoint = vinePoints[vinePoints.Count - 1];
            findNextPoint(currentPoint, highestSearchRadius);
        }
    }

    void generateVine()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        //lineRenderer.startColor = new Color(0.4f, 0.2f, 0.1f, 1);
        //lineRenderer.endColor = Color.green;
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

        // get sample points inside the radius
        for (int i = 0; i < 100; i++)
        {
            Vector3 randomPoint = currentPoint.point + Random.insideUnitSphere * searchRadius;
            randomPoint.y += 1.0f;

            RaycastHit hit;
            if (Physics.Raycast(randomPoint, Vector3.down, out hit, 20.0f))
            {
                // check if the candidate point is somewhat in the right growth direction
                Vector3 candidateDirection = (hit.point - currentPoint.point).normalized;
                float alignment = Vector3.Dot(candidateDirection, growthDirection);

                // check if this point is the highest valid point
                if (alignment >= 0.5f && hit.point.y >= maxY && !vinePoints.Any(vp => vp.point == hit.point))
                {
                    maxY = hit.point.y;
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                    found = true;
                }else if (alignment >= 0.5f && !vinePoints.Any(vp => vp.point == hit.point)) // allow movement downwards only if necessary
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
            // try raycasting along the current point's tangent to surface normal
            Vector3 tangent = (growthDirection - Vector3.Dot(growthDirection, currentPoint.normal) * currentPoint.normal).normalized;

            Debug.DrawRay(currentPoint.point, currentPoint.normal * 2f, Color.green, 1.0f);  // normal
            Debug.DrawRay(currentPoint.point, tangent * 2f, Color.blue, 1.0f); // tangent
            Debug.DrawRay(currentPoint.point, growthDirection, Color.yellow, 1.0f); // growth direction

            RaycastHit hit;

            if (Physics.Raycast(currentPoint.point, tangent, out hit, 1.0f))
            {
                addPoint(new Vertex(hit.point, hit.normal));
            }


            // raycast in growth direction from currentPoint.point
            /*
            RaycastHit hit;
            if (Physics.Raycast(currentPoint.point, growthDirection, out hit, 5.0f))
            {
                currentHighestPoint = hit.point;
                currentHighestNormal = hit.normal;
                Debug.Log("highest nearby point: " + currentHighestPoint);
                foundHighest = true;
                addPoint(new Vertex(hit.point, hit.normal));
            }
            */
        }
    }

    void OnDrawGizmos()
    {
        if (vinePoints.Count > 0)
        {
            Vertex currentPoint = vinePoints[vinePoints.Count - 1];

            Gizmos.color = new Color(0, 1, 0, 0.3f);

            Gizmos.DrawWireSphere(currentPoint.point, searchRadius);

            Gizmos.color = new Color(1, 0, 0, 0.3f);
            Gizmos.DrawWireSphere(currentPoint.point, highestSearchRadius);
        }
    }

}
