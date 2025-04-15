using UnityEngine;
using UnityEngine.UI;

public class Sliders : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public CombinedVine combinedVine;
    public GameObject sun;
    private float sunXAngle = 45.0f;

    void Start()
    {
        // get vines
        combinedVine = GameObject.Find("Vine Spawn Point(Clone)").GetComponent<CombinedVine>();

        // Set slider ranges
        sunSlider.minValue = 0f;
        sunSlider.maxValue = 180f;
        leafSlider.minValue = 0f;
        leafSlider.maxValue = 1f;

        // Set up listeners for slider value changes
        sunSlider.onValueChanged.AddListener(UpdateSunRotation);
        leafSlider.onValueChanged.AddListener(UpdateLeafProbability);

        // Initialize slider values from current state
        sunSlider.value = sun.transform.eulerAngles.x;
        leafSlider.value = combinedVine.leafProbability;
    }

    // Directly update the sun's rotation on the X-axis
    void UpdateSunRotation(float value)
    {
        sunXAngle = value;
        sun.transform.rotation = Quaternion.Euler(sunXAngle, 0f, 0f);
    }

    // Directly update the leaf probability in CombinedVine
    void UpdateLeafProbability(float value)
    {
        combinedVine.leafProbability = value;
    }
}
