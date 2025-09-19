using UnityEngine;
using UnityEditor; //Remove before Compile

public class Quitter : MonoBehaviour
{
    public void Quit()
    {
        Debug.Log("Quitting Game!");

        Application.Quit();
        EditorApplication.isPlaying = false;
    }
}
