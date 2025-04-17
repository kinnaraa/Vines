using UnityEngine;
using UnityEngine.UI;

public class SpawnVine : MonoBehaviour
{
    public GameObject vinePrefab;
    public Camera cam;
    public CombinedVine combinedVine;

    public GameObject canvas;

    public Slider leafSlider;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        canvas.SetActive(false);

        if (leafSlider == null)
        {
            var s = FindFirstObjectByType<Sliders>();
            if (s != null) leafSlider = s.leafSlider;
        }
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

                combinedVine.leafProbability = leafSlider != null? leafSlider.value: combinedVine.leafProbability;
                combinedVine.RedoLeaves();

                combinedVine.AddPoint(new CombinedVine.Vertex(hit.point, hit.normal));

                canvas.SetActive(true);
            }
            else { Debug.Log("not found"); }
        }
    }
}
