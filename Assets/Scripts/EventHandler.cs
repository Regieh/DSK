using UnityEngine;

public class EventHandler : MonoBehaviour
{
    public bool isGameOver = true;
    public bool isGamePaused = false;
    public GameObject gameOverPanel;
    public GameObject scorePanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gameOverPanel.SetActive(isGameOver);
        scorePanel.SetActive(!isGameOver && !isGamePaused);
    }
}
