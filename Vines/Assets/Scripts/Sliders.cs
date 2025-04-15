using UnityEngine;
using UnityEngine.UI;

public class Sliders : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public CombinedVine combinedVine;
    public GameObject sun;

    void Start()
    {
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
        leafSlider.value = CombinedVine.leafProbability;
    }

    // Directly update the sun's rotation on the X-axis
    void UpdateSunRotation(float value)
    {
        Vector3 currentEuler = sun.transform.eulerAngles;
        sun.transform.eulerAngles = new Vector3(value, currentEuler.y, currentEuler.z);
    }

    // Directly update the leaf probability in CombinedVine
    void UpdateLeafProbability(float value)
    {
        CombinedVine.leafProbability = value;
    }
}
