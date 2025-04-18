using UnityEngine;
using UnityEngine.UI;

public class Sliders : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public GameObject sun;
    private float sunXAngle = 45.0f;

    void Start()
    {        
        sunSlider.minValue = 0f;
        sunSlider.maxValue = 180f;
        leafSlider.minValue = 0f;
        leafSlider.maxValue = 1f;

        sunSlider.onValueChanged.AddListener(UpdateSunRotation);
        leafSlider.onValueChanged.AddListener(UpdateLeafProbability);

        sunSlider.value = sun.transform.eulerAngles.x;

        leafSlider.value = 0.4f;

    }

    void UpdateSunRotation(float value)
    {
        sunXAngle = value;
        sun.transform.rotation = Quaternion.Euler(sunXAngle, 0f, 0f);

        var allVines = GameObject.FindGameObjectsWithTag("Vine");

        foreach (var vineObject in allVines)
        {
            CombinedVine vine = vineObject.GetComponent<CombinedVine>();
            vine.stopVine = false;
        }
    }

    void UpdateLeafProbability(float value)
    {
        leafSlider.value = value;
        var allVines = GameObject.FindGameObjectsWithTag("Vine");

        foreach (var vineObject in allVines)
        {
            CombinedVine vine = vineObject.GetComponent<CombinedVine>();
            // 3) set the new probability
            vine.leafProbability = value;
            // 4) rebuild leaves immediately
            vine.RedoLeaves();
            vine.CleanUpLeaves();
        }
    }
}
