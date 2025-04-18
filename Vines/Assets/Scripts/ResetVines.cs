using UnityEngine;
using UnityEngine.SceneManagement;

public class ResetVines : MonoBehaviour
{
    // Reset scene
    public void ResetScene()
    {
        var allVines = GameObject.FindGameObjectsWithTag("Vine");
        foreach( var vine in allVines)
        {
            Destroy(vine);
        }
    }
}
