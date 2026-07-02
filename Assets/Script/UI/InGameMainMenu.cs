using UnityEngine;
using UnityEngine.SceneManagement;

public class InGameMainMenu : MonoBehaviour
{
    public GameObject inGameMenuPanel;
    public GameObject winPanel;
    public GameObject losePanel;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void NextLevel()
    {
        winPanel.SetActive(false);
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        gameManager.NextLevel();
    }

    public void WinGame()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        Debug.Log("win" + gameManager.name);
        gameManager.WinLevel();
        Debug.Log("Player reached destination - WIN");
    }

    public void LoseGame()
    {
        
        GameManager gameManager = FindAnyObjectByType<GameManager>();
        Debug.Log("lose" + gameManager.name);
        gameManager?.LoseLevel();
        Debug.Log("NPCMover: NPC entered player cell - PLAYER LOSE");
        losePanel.SetActive(false);
    }

    public void ReturnToMainMenu()
    {
        //GameManager gameManager = FindAnyObjectByType<GameManager>();
        //Debug.Log("Return to Main Menu" + gameManager.name);
        SceneManager.LoadScene("MainMenu");
    }
}
