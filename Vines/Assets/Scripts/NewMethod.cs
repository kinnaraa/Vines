using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class NewMethod : MonoBehaviour
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

        public override bool Equals(object obj)
        {
            Vertex other = (Vertex)obj;
            if (Vector3.Distance(point, other.point) < 0.01f && Vector3.Dot(normal, other.normal) > 0.99f)
            {
                return true;
            }

            return false;
        }
    }

    public LineRenderer lineRenderer;
    public List<Vertex> vinePoints = new List<Vertex>();

    private int numRays = 100;
    private float searchRadius = 0.5f;

    private Vector3 currentHighestPoint = Vector3.zero;
    private bool foundHighest = false;
    private float highestSearchRadius = 4.0f;

    private bool stopVine = false;

    void Start()
    {
        generateVine();
    }

    void Update()
    {
        if (vinePoints.Count < 100 && vinePoints.Count > 0 && !stopVine)
        {
            Vertex currentPoint = vinePoints[vinePoints.Count - 1];
            findNextPoint(currentPoint);
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
        if (vinePoints.Contains(newPoint)) // prevents duplicates (made Equal override)
        {
            Debug.Log("point already exists in vine");
            return;
        }

        vinePoints.Add((newPoint));
        lineRenderer.positionCount = vinePoints.Count;
        lineRenderer.SetPosition(vinePoints.Count - 1, newPoint.point);
        Debug.Log("added point: " + newPoint.point + "to lineRenderer!");
    }

    void findNextPoint(Vertex currentPoint)
    {
        // if current point is very near the local high point, find a new one by increasing radius of the search
        if (foundHighest && (currentPoint.point.y - currentHighestPoint.y >= 0.0f || currentPoint.point.y - currentHighestPoint.y >= -0.05f))
        {
            highestSearchRadius *= 1.5f;
            foundHighest = false;
        }

        if (!foundHighest)
        {
            float highestMaxY = currentHighestPoint.y;

            // sample points to find a local high point
            for (int i = 0; i < 500; i++)
            {
                Vector3 samplePoint = currentPoint.point + Random.insideUnitSphere * highestSearchRadius;
                samplePoint.y += 1.0f;

                RaycastHit hit;
                if (Physics.Raycast(samplePoint, Vector3.down, out hit, 20.0f))
                {
                    if (hit.point.y >= highestMaxY && !vinePoints.Any(vp => vp.point == hit.point))
                    {
                        highestMaxY = hit.point.y;
                        currentHighestPoint = hit.point;
                    }
                }
            }

            if (currentHighestPoint != Vector3.zero)
            {
                foundHighest = true;
                Debug.Log("New highest point: " + currentHighestPoint);
            }
            else
            {
                // if no higher point is found, you've reached the top so the vine must stop
                stopVine = true;
            }
        }

        Vector3 bestPoint = Vector3.zero;
        Vector3 bestNormal = Vector3.zero;
        float bestScore = float.MinValue;

        Vector3 growthDirection = (currentHighestPoint - currentPoint.point).normalized;

        // offset the ray origin so it doesn't limit itself
        Vector3 rayOrigin = currentPoint.point + currentPoint.normal * 0.05f;

        for (int i = 0; i < numRays; i++)
        {
            float azimuth = (360f / numRays) * i;
            float elevation = Random.Range(0f, 360);

            // create rays in a sphere basically
            Quaternion rotationAzimuth = Quaternion.AngleAxis(azimuth, currentPoint.normal);
            Quaternion rotationElevation = Quaternion.AngleAxis(elevation, Vector3.Cross(currentPoint.normal, growthDirection));

            Vector3 direction = rotationAzimuth * rotationElevation * growthDirection;
            direction.Normalize();

            RaycastHit hit;
            Debug.DrawRay(rayOrigin, direction * searchRadius, Color.cyan, 1.0f);

            if (Physics.Raycast(rayOrigin, direction, out hit, searchRadius))
            {
                // get its alignment with growth direction
                float alignment = Vector3.Dot(direction, growthDirection);

                // if the candidate is more upwards, give it a higher score
                //float upwardBonus = (hit.normal.y > 0.5f) ? 0.2f : 0.0f;

                float score = alignment; // + upwardBonus;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                }
            }
        }

        // --- 4. ADD THE BEST FOUND POINT ---
        if (bestScore != float.MinValue)
        {
            addPoint(new Vertex(bestPoint, bestNormal));
        }
        else
        {
            Debug.Log("No valid surface found. Stopping growth.");
        }
    }


    void OnDrawGizmos()
    {
        if (vinePoints.Count > 0)
        {
            Vertex currentPoint = vinePoints[vinePoints.Count - 1];
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(currentPoint.point, searchRadius);
        }
    }
}
