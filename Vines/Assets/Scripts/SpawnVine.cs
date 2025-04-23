using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class SpawnVine : MonoBehaviour
{
    public GameObject vinePrefab;
    public Camera cam;
    public CombinedVine combinedVine;

    public GameObject canvas;

    public Slider leafSlider;
    public Slider growthSpeedSlider;

    public GameObject environmentMesh;
    public List<GameObject> allVines;
    public GameObject latestVine = null;

    public float growthSpeed;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        canvas.SetActive(false);
        allVines = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButtonDown(0) == true)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if(Physics.Raycast(ray, out RaycastHit hit, 100.0f))
            {
                if (hit.collider.gameObject.tag == "blocker")
                {
                    return;
                }

                if (hit.collider.gameObject == environmentMesh)
                {
                    GameObject vine = Instantiate(vinePrefab, hit.point, Quaternion.identity);
                    latestVine = vine;

                    combinedVine = vine.GetComponent<CombinedVine>();

                    combinedVine.growthSpeed = growthSpeed;

                    combinedVine.leafProbability = leafSlider.value;
                    combinedVine.RedoLeaves();

                    allVines.Add(vine);

                    combinedVine.AddPoint(new CombinedVine.Vertex(hit.point, hit.normal));

                    canvas.SetActive(true);
                }               
            }
        }
    }
}
