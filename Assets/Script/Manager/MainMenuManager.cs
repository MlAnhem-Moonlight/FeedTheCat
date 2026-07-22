using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public Button play;
    public GameObject levelChoice;
    public Button archive;
    public Button quit;

    void Start()
    {
        GameManager gameManager = FindAnyObjectByType<GameManager>();

        if (gameManager != null)
        {
            //LogFilter.LogLevelTransfer($"MainMenuManager: Found GameManager '{gameManager.name}' and WinGame : {gameManager.winGame}");
            
            // Check if player just won a level and return from GamePlay
            if (gameManager.winGame)
            {
                
                levelChoice.SetActive(true);
                gameManager.TurnOnLevelChoice();
                gameManager.winGame = false;
            }

            // Add TurnOnLevelChoice to play button
            if (play != null)
            {
                play.onClick.AddListener(() => OnPlayClicked(gameManager));
            }
        }
        else
        {
            //LogFilter.LogLevelTransfer("MainMenuManager: GameManager not found in scene");
        }
        AudioManager.Instance.PlayMusic("theme_menu");
    }

    /// <summary>
    /// Handle play button click - turn on level choice and load level selection scene
    /// </summary>
    private void OnPlayClicked(GameManager gameManager)
    {
        //LogFilter.LogLevelTransfer("MainMenuManager: Play button clicked");

        // Turn on level choice event subscription in GameManager
        gameManager.TurnOnLevelChoice();

        //// Load the level selection scene
        //// Change "LevelSelection" to match your actual scene name if different
        //SceneManager.LoadScene("LevelSelection");
    }

    public void QuitGame()
    {
        //LogFilter.LogLevelTransfer("MainMenuManager: Quit button clicked");
        Application.Quit();
    }

    // Update is called once per frame
    void Update()
    {

    }
}

