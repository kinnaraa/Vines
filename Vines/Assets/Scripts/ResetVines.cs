using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ResetVines : MonoBehaviour
{
    public Slider sunSlider;
    public Slider leafSlider;
    public Slider startRadiusSlider;
    public SpawnVine spawnVine;

    // Reset scene
    public void ResetScene()
    {
        foreach (var vine in spawnVine.allVines)
            Destroy(vine);

        spawnVine.allVines.Clear();

        if (spawnVine != null)
        {
            spawnVine.latestVine = null;
        }
        else
        {
            return;
        }

        sunSlider.SetValueWithoutNotify(45f);
        leafSlider.SetValueWithoutNotify(0.4f);
        startRadiusSlider.SetValueWithoutNotify(1.0f);
    }
}
