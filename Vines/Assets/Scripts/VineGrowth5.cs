using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class VineGrowth5 : MonoBehaviour
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
    private float searchRadius = 0.75f;
    private float highestSearchRadius = 4.0f;

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
            Debug.Log("point already exists in vine");
            return;
        }

        vinePoints.Add((newPoint));
        lineRenderer.positionCount = vinePoints.Count;
        lineRenderer.SetPosition(vinePoints.Count - 1, newPoint.point);
        Debug.Log("added point: " + newPoint.point + "to lineRenderer!");
    }

    void findNextPoint(Vertex currentPoint, float highestSearchRadius)
    {
        // if the current point is close enough to the highest point, find a new highest point to guide the vine
        if (foundHighest && (currentPoint.point.y - currentHighestPoint.y >= 0.0f || currentPoint.point.y - currentHighestPoint.y >= -0.05f))
        {
            // search for new highest point again
            highestSearchRadius *= 2.0f;
            foundHighest = false;
        }

        if (!foundHighest)
        {
            float highestMaxY = currentHighestPoint.y;

            // get sample points in radius and cast downward rays to find valid surface points
            for (int i = 0; i < 500; i++)
            {
                Vector3 samplePoint = currentPoint.point + Random.insideUnitSphere * highestSearchRadius;
                samplePoint.y += 1.0f;
                RaycastHit hit;

                if (Physics.Raycast(samplePoint, Vector3.down, out hit, 20.0f))
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
                foundHighest = true;
                Debug.Log("highest nearby point: " + currentHighestPoint);
            }
        }

        // get the growth direction using this point to guide the vine towards it
        Vector3 growthDirection = (currentHighestPoint - currentPoint.point).normalized;

        // check for vertical wall close in front, if so, attach
        /*if (vinePoints.Count > 2)
        {
            Vector3 currentDir = (currentPoint.point - vinePoints[vinePoints.Count - 2].point).normalized;
            RaycastHit hitVert;
            if (Physics.Raycast(currentPoint.point, currentDir, out hitVert, 1.0f))
            {
                addPoint(new Vertex(hitVert.point, hitVert.normal));
                Debug.Log("found vertical wall to attach to");
                return;
            }
        }*/

        float sphereRadius = 0.1f;
        float castDistance = 1.0f;
        int numSamples = 100;

        Vector3 bestPoint = Vector3.zero;
        Vector3 bestNormal = Vector3.zero;
        float bestScore = float.MinValue;

        // offset casting point to make sure flat surfaces are found
        Vector3 offset = currentPoint.point + currentPoint.normal * 0.1f + Vector3.up * 0.1f;

        // get sample points inside the radius
        for (int i = 0; i < numSamples; i++)
        {
            // bias random direction towards the growth direction
            Vector3 randomDir = Random.onUnitSphere;
            Debug.DrawRay(offset, randomDir * castDistance, Color.cyan, 1.0f);

            RaycastHit hit;
            if (Physics.SphereCast(offset, sphereRadius, randomDir, out hit, castDistance))
            {
                // score based on alignment with growth direction
                Vector3 candidateDir = (hit.point - currentPoint.point).normalized;
                float alignment = Vector3.Dot(candidateDir, growthDirection);
                float score = alignment;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPoint = hit.point;
                    bestNormal = hit.normal;
                }
            }
        }

        if (bestScore != float.MinValue)
        {
            addPoint(new Vertex(bestPoint, bestNormal));
        }

        // for debugging
        Debug.DrawRay(currentPoint.point, currentPoint.normal * 2f, Color.green, 1.0f); // normal - GREEN
        Debug.DrawRay(currentPoint.point, growthDirection, Color.yellow, 1.0f); // growth direction - YELLOW
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
