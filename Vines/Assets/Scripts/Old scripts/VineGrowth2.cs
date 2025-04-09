using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class VineGrowth2 : MonoBehaviour
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
        if(vinePoints.Count <= 20 && vinePoints.Count > 0)
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
        RaycastHit hit;

        // try going up first
        if (Physics.Raycast(currentPoint.point, Vector3.up, out hit, maxDistance))
        {
            Debug.Log("Upward growth hit: " + hit.point);
            addPoint(new Vertex(hit.point, hit.normal));
            return;
        }

        // raycast from current point upwards with a max length
        if(Physics.Raycast(currentPoint.point, currentPoint.normal, out hit, maxDistance))
        {
            Debug.Log("Hit point: " + hit.point);
            addPoint(new Vertex(hit.point, hit.normal));
            return;
        }
        else
        {
            Debug.Log("Not initial hit, trying new directions");

            // raycast in lateral directions to search for a valid point
            RaycastHit hit1;

            Vector3 reference;

            if (Mathf.Abs(currentPoint.normal.y) > 0.9f)
            {
                reference = Vector3.right;
            }
            else
            {
                reference = Vector3.up;
            }

            Vector3 perp1 = Vector3.Cross(currentPoint.normal, reference).normalized;
            Vector3 perp2 = Vector3.Cross(currentPoint.normal, perp1).normalized;

            List<Vector3> perpDirections = new List<Vector3> { perp1, -perp1, perp2, -perp2 };

            foreach(Vector3 dir in perpDirections)
            {
                Debug.Log("new dir: " + dir);

                if (Physics.Raycast(currentPoint.point, dir, out hit1, maxDistance))
                {
                    Debug.Log("Hit point: " + hit1.point);
                    addPoint(new Vertex(hit1.point, hit1.normal));
                    return;
                }
            }
        }
    }
}
