using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    [Header("Панели Меню")]
    public GameObject mainMenuPanel;        public GameObject lobbyPanel;           public GameObject settingsPanel;    
    [Header("Менеджеры")]
    public GameLoopManager gameLoopManager; 
    void Start()
    {
                Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        TogglePlayerControl(false);
    }

    
        public void OpenLobby()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
    }

        public void OpenSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

        public void ExitGame()
    {
        Debug.Log("Выход из игры...");
        Application.Quit();
    }

    
    public void BackToMainMenuFromLobby()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void BackToMainMenuFromSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    
        public void StartGame()
{
    if (lobbyPanel != null) lobbyPanel.SetActive(false);

    TogglePlayerControl(true); 
        if (gameLoopManager != null)
        {
            gameLoopManager.StartQuizGame(5);
        }
    }

        private void TogglePlayerControl(bool enable)
    {
                PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);

        if (players.Length == 0)
        {
                                    if (!enable)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

                foreach (var player in players)
        {
            if (player != null && player.IsOwner)             {
                player.enabled = enable;

                if (enable)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
            }
        }
    }
}