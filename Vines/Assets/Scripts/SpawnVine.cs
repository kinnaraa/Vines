using UnityEngine;

public class SpawnVine : MonoBehaviour
{
    public GameObject vinePrefab;
    public Camera cam;
    public CombinedVine combinedVine;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = gameObject.GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0) == true)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, 100.0f))
            {
                Debug.Log("spawn point at " + hit.point);
                Vector3 spawnPoint = hit.point;

                Instantiate(vinePrefab, spawnPoint, Quaternion.identity);

                combinedVine = vinePrefab.GetComponent<CombinedVine>();
                combinedVine.AddPoint(new CombinedVine.Vertex(hit.point, hit.normal));
            }
            else { Debug.Log("not found"); }
        }
    }
}
