using UnityEngine;
using UnityEngine.UI;

public class SpawnVine : MonoBehaviour
{
    public GameObject vinePrefab;
    public Camera cam;
    public CombinedVine combinedVine;

    public GameObject canvas;

    public Slider leafSlider;

    public GameObject environmentMesh;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        canvas.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0) == true)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, 100.0f))
            {
                if (hit.collider.gameObject == environmentMesh)
                {
                    Debug.Log("spawn point at " + hit.point);

                    GameObject vine = Instantiate(vinePrefab, hit.point, Quaternion.identity);

                    combinedVine = vine.GetComponent<CombinedVine>();

                    combinedVine.leafProbability = leafSlider.value;
                    combinedVine.RedoLeaves();

                    combinedVine.AddPoint(new CombinedVine.Vertex(hit.point, hit.normal));

                    canvas.SetActive(true);
                }
                else
                {
                    Debug.Log("no vine spawned");
                }                
            }
        }
    }
}
