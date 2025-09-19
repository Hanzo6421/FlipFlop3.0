using UnityEngine;
using TMPro;

public class TextManager : MonoBehaviour
{
    private TMP_Text text;
    private string scoreNumber;

    public string inputText;
    public int score;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text = GetComponent<TMP_Text>();
        scoreNumber = score.ToString();
        text.text = inputText + " " + scoreNumber;
    }

    // Update is called once per frame
    public void AddScore(int Score)
    {
        score += Score;
        scoreNumber = score.ToString();
        text.text = inputText + " " + scoreNumber;
    }

    public void SetScore(int Score)
    {
        score = Score;
        scoreNumber = score.ToString();
        text.text = inputText + " " + scoreNumber;
    }
}
