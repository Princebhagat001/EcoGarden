using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject mainMenuUI;
    public GameObject menuPrompt;
    
    // Static variables stay the same even when a scene reloads
    private static bool shouldShowMenu = true;
    private bool isMenuOpen = false;

    void Start()
    {
        if (shouldShowMenu)
        {
            ShowMenu();
            // After showing it once, we set this to false 
            // so it doesn't auto-show on a Restart
            shouldShowMenu = false; 
        }
        else
        {
            ResumeGame();
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            if (isMenuOpen)
                ResumeGame();
            else
                ShowMenu();
        }
    }

    public void ShowMenu()
    {
        mainMenuUI.SetActive(true);
        menuPrompt.SetActive(false);
        isMenuOpen = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        mainMenuUI.SetActive(false);
        menuPrompt.SetActive(true);
        isMenuOpen = false;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        // We leave shouldShowMenu as false here so that 
        // when the scene reloads, the game starts immediately!
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}