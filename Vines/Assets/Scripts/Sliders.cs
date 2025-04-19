using UnityEngine;
using UnityEngine.UI;

public class Sliders : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public Slider startRadiusSlider;
    public Slider endRadiusSlider;

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

        startRadiusSlider.minValue = 0.001f;
        startRadiusSlider.maxValue = 0.2f;
        startRadiusSlider.onValueChanged.AddListener(UpdateStartRadius);
        startRadiusSlider.value = 0.06f;

        endRadiusSlider.minValue = 0.001f;
        endRadiusSlider.maxValue = 0.2f;
        endRadiusSlider.onValueChanged.AddListener(UpdateEndRadius);
        endRadiusSlider.value = 0.001f;

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
            vine.leafProbability = value;
            vine.RedoLeaves();
            vine.CleanUpLeaves();
        }
    }

    void UpdateStartRadius(float value)
    {
        startRadiusSlider.value = value;
        foreach (var vineObject in GameObject.FindGameObjectsWithTag("Vine"))
        {
            var vine = vineObject.GetComponent<CombinedVine>();
            vine.startRadius = value;
            vine.GenerateMesh();
        }
    }

    void UpdateEndRadius(float value)
    {
        endRadiusSlider.value = value;
        foreach (var vineObject in GameObject.FindGameObjectsWithTag("Vine"))
        {
            var vine = vineObject.GetComponent<CombinedVine>();
            vine.endRadius = value;
            vine.GenerateMesh();
        }
    }
}
