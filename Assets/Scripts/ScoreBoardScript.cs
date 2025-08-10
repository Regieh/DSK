using UnityEngine;
using TMPro;

public class ScoreBoardScript : MonoBehaviour
{
    public int playerScore = 0; 
    public TextMeshProUGUI scoreText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        scoreText.text = playerScore.ToString();
    }

}
