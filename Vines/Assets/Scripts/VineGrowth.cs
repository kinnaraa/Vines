using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class VineGrowth : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public List<Vector3> vinePoints = new List<Vector3> ();
    public Vector3 tendril;
    private float radius = 5.0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        addPoint(transform.position);
    }

    // Update is called once per frame
    void Update()
    {
        tendril = vinePoints[^1];
        findNextObject();
    }

    void addPoint(Vector3 newPoint)
    {
        if (vinePoints.Contains(newPoint)) // prevents duplicates
        {
            Debug.Log("Point already exists in vine");
            enabled = false;
            return;
        }

        vinePoints.Add((newPoint));
        lineRenderer.positionCount = vinePoints.Count;
        lineRenderer.SetPosition(vinePoints.Count - 1, newPoint);
        Debug.Log("Added point: " + newPoint + "to lineRenderer!");
    }

    void findNextObject()
    {
        // find all colliders in a radius around the tendril tip
        int layerMask = ~LayerMask.GetMask("Vine");
        Collider[] objectsInRadius = Physics.OverlapSphere(tendril, radius, layerMask);

        Debug.Log("Objects detected: " + objectsInRadius.Length);

        Vector3 closestPoint = Vector3.zero;
        float minDistance = Mathf.Infinity;

        if (objectsInRadius.Count() == 0)
        {
            Debug.Log("No objects found");
            return;
        }

        // check points on colliders for closest point
        foreach (Collider obj in objectsInRadius)
        {
            Vector3 offsetTendril = tendril + (Vector3.up * 0.01f); // Small offset
            Vector3 option = obj.ClosestPoint(offsetTendril);

            if (vinePoints.Contains(option)) continue;

            float dist = Vector3.Distance(tendril, option);
            if (dist < minDistance)
            {
                minDistance = dist;
                closestPoint = option;
            }
        }

        // have closest point on another object
        if (closestPoint != Vector3.zero)
        {
            Debug.Log("Next found point: " + closestPoint);
            addPoint(closestPoint);
        }
        else { return; }
    }
}
