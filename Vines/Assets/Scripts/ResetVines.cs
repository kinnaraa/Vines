using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResetVines : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public Slider startRadiusSlider;
    public Slider endRadiusSlider;

    // Reset scene
    public void ResetScene()
    {
        var allVines = GameObject.FindGameObjectsWithTag("Vine");
        foreach( var vine in allVines)
        {
            Destroy(vine);
        }
        sunSlider.value = 45.0f;
        leafSlider.value = 0.4f;
        startRadiusSlider.value = 0.06f;
        endRadiusSlider.value = 0.001f;
    }
}
