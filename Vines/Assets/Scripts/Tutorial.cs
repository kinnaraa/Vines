using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Tutorial : MonoBehaviour
{
    public GameObject tutorialArea;
    public GameObject tutorialButton;
    public TMP_Text buttonText;
    bool tutorialOpen = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttonText = tutorialButton.GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenTutorial()
    {
        tutorialOpen = !tutorialOpen;
        tutorialArea.SetActive(tutorialOpen);
        if(tutorialOpen )
        {
            buttonText.text = "Close";
        }
        else { buttonText.text = "Tutorial"; }
    }
}
