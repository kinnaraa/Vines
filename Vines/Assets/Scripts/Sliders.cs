using UnityEngine;
using UnityEngine.UI;

public class Sliders : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public Slider radiusSlider;
    public Slider growthSpeedSlider;

    public GameObject sun;
    private float sunXAngle = 45.0f;

    public SpawnVine spawnVine;
    private float originalStart = 0.06f;
    private float originalEnd = 0.003f;

    void Start()
    {        
        sunSlider.minValue = 0f;
        sunSlider.maxValue = 180f;
        leafSlider.minValue = 0f;
        leafSlider.maxValue = 1f;

        sunSlider.onValueChanged.AddListener(UpdateSunRotation);
        leafSlider.onValueChanged.AddListener(UpdateLeafProbability);

        sunSlider.value = sun.transform.eulerAngles.x;

        leafSlider.value = 0.6f;

        radiusSlider.minValue = 0.4f;
        radiusSlider.maxValue = 2.0f;
        radiusSlider.onValueChanged.AddListener(UpdateRadius);
        radiusSlider.value = 1.0f;

        growthSpeedSlider.minValue = -2.5f;
        growthSpeedSlider.maxValue = -1.4f;
        growthSpeedSlider.wholeNumbers = false;
        growthSpeedSlider.value = spawnVine.growthSpeed;

        growthSpeedSlider.onValueChanged.AddListener(exp =>
        {
            spawnVine.growthSpeed = Mathf.Pow(10f, exp);
        });

        Application.targetFrameRate = 100;
    }

    void UpdateSunRotation(float value)
    {
        sunXAngle = value;
        sun.transform.rotation = Quaternion.Euler(sunXAngle, 0f, 0f);

        foreach (var vineObject in spawnVine.allVines)
        {
            CombinedVine vine = vineObject.GetComponent<CombinedVine>();
            vine.stopVine = false;
        }
    }

    void UpdateLeafProbability(float value)
    {
        leafSlider.value = value;

        CombinedVine vine = spawnVine.latestVine.GetComponent<CombinedVine>();
        vine.leafProbability = value;
        vine.RedoLeaves();
        vine.CleanUpLeaves();
    }

    void UpdateRadius(float value)
    {
        radiusSlider.value = value;
        var vine = spawnVine.latestVine.GetComponent<CombinedVine>();
        vine.startRadius = originalStart * value;
        vine.endRadius = originalEnd * value;
        vine.GenerateMesh();
    }

    void UpdateGrowthSpeed(float value)
    {
        growthSpeedSlider.value = value;
        spawnVine.growthSpeed = growthSpeedSlider.value;
    }
}
