using UnityEngine;
using TMPro;

public class ScoreboardScript : MonoBehaviour
{
    public TextMeshProUGUI scoreboardText; // Drag your TMP object here
    private float startTime;
    private bool isFinished = false;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        if (!isFinished)
        {
            float t = Time.time - startTime;

            string minutes = ((int)t / 60).ToString("00");
            string seconds = (t % 60).ToString("00");
            string milliseconds = ((int)(t * 100) % 100).ToString("00");

            scoreboardText.text = $"{minutes}:{seconds}:{milliseconds}";
        }
    }

    public void FinishGame()
    {
        isFinished = true;
    }
}
