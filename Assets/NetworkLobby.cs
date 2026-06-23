using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkLobby : NetworkBehaviour
{
    [Header("Панели интерфейса")]
    public GameObject loginPanel;          public GameObject lobbyPanel;          public GameObject quizHUDPanel;        public GameObject resultsPanel;    
    [Header("Элементы Ввода (Login Panel)")]
    public InputField nicknameInputField;     public InputField ipInputField;       
    [Header("Элементы Лобби Панели")]
    public Text playerListText;            public Button startGameButton;         public InputField roundsInputField;

    [Header("Ссылки на менеджеры")]
    public GameLoopManager gameLoopManager;

    [Header("Текст результатов")]
    public Text finalResultsText;

    public NetworkVariable<int> totalRoundsSync = new NetworkVariable<int>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
                if (loginPanel != null) loginPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (quizHUDPanel != null) quizHUDPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

                if (ipInputField != null && string.IsNullOrEmpty(ipInputField.text))
        {
            ipInputField.text = "127.0.0.1";
        }

        if (nicknameInputField != null)
        {
            nicknameInputField.characterLimit = 20;
            nicknameInputField.text = PlayerPrefs.GetString("SavedNickname", "Игрок");
        }

                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

            
        public void StartHostLobby()
    {
        string enteredName = GetCleanNickname();
        ConfigureNetworkTransport(true); 
        if (NetworkManager.Singleton.StartHost())
        {
            Debug.Log("[СЕТЬ] Лобби успешно создано в режиме HOST.");

            if (loginPanel != null) loginPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);

                        if (startGameButton != null) startGameButton.gameObject.SetActive(true);

                        if (gameLoopManager != null)
            {
                gameLoopManager.SetPlayerNickname(NetworkManager.Singleton.LocalClientId, enteredName);
            }

            UpdatePlayerListUI();
        }
        else
        {
            Debug.LogError("[СЕТЬ] Не удалось запустить Хост. Проверь порты.");
        }
    }

        public void LeaveLobby()
    {
        if (NetworkManager.Singleton != null)
        {
                        NetworkManager.Singleton.Shutdown();
        }

                if (loginPanel != null) loginPanel.SetActive(true);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (quizHUDPanel != null) quizHUDPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(false);

                Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[СЕТЬ] Вы успешно вышли из лобби и отключились.");
    }

        public void StartClientLobby()
    {
        string enteredName = GetCleanNickname();
        ConfigureNetworkTransport(false); 
        if (NetworkManager.Singleton.StartClient())
        {
            Debug.Log("[СЕТЬ] Попытка подключения к Хосту в режиме CLIENT...");

            if (loginPanel != null) loginPanel.SetActive(false);
            if (lobbyPanel != null) lobbyPanel.SetActive(true);

                        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogError("[СЕТЬ] Ошибка инициализации сетевого Клиента.");
        }
    }

        private void OnClientConnected(ulong clientId)
    {
        UpdatePlayerListUI();

                if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            string myName = GetCleanNickname();
            if (gameLoopManager != null)
            {
                gameLoopManager.SetPlayerNicknameServerRpc(clientId, myName);
            }
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        UpdatePlayerListUI();
    }

        public void PressStartGameButton()
    {
        if (!IsServer) return;

        int rounds = 5;
        if (roundsInputField != null && int.TryParse(roundsInputField.text, out int value))
        {
            rounds = value;
        }

                StartGameClientRpc();

                if (gameLoopManager != null)
        {
            gameLoopManager.StartQuizGame(rounds);
        }
    }

    [ClientRpc]
    public void StartGameClientRpc()
    {
        if (loginPanel != null) loginPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (quizHUDPanel != null) quizHUDPanel.SetActive(true);
        if (resultsPanel != null) resultsPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

            
    private string GetCleanNickname()
    {
        string enteredName = "Игрок";
        if (nicknameInputField != null && !string.IsNullOrEmpty(nicknameInputField.text))
        {
            enteredName = nicknameInputField.text;
        }
        PlayerPrefs.SetString("SavedNickname", enteredName);
        return enteredName;
    }

    private void ConfigureNetworkTransport(bool isHost)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport != null)
        {
            if (isHost)
            {
                                transport.ConnectionData.Address = "0.0.0.0";
            }
            else
            {
                                string targetIP = (ipInputField != null && !string.IsNullOrEmpty(ipInputField.text)) ? ipInputField.text : "127.0.0.1";
                transport.ConnectionData.Address = targetIP;
            }
            Debug.Log($"[СЕТЕВОЙ ТРАНСПОРТ] Установлен IP адрес: {transport.ConnectionData.Address}, Порт: {transport.ConnectionData.Port}");
        }
    }

    private void UpdatePlayerListUI()
    {
        if (playerListText == null) return;

        string listStr = "ПОДКЛЮЧЕННЫЕ ИГРОКИ:\n";
        int index = 1;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            listStr += $"{index}. Игрок ID: {client.ClientId}\n";
            index++;
        }
        playerListText.text = listStr;
    }

    public void ShowFinalResults(string finalLeaderboardText)
    {
        if (!IsServer) return;
        ShowFinalResultsClientRpc(finalLeaderboardText);
    }

    [ClientRpc]
    private void ShowFinalResultsClientRpc(string finalLeaderboardText)
    {
        if (quizHUDPanel != null) quizHUDPanel.SetActive(false);
        if (resultsPanel != null) resultsPanel.SetActive(true);

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
        foreach (var player in allPlayers)
        {
            if (player != null)
            {
                player.isGameActive = false;
                player.canAnswer = false;
            }
        }

        if (finalResultsText != null)
        {
            finalResultsText.text = finalLeaderboardText;
        }

                StartCoroutine(ForceReleaseCursorRoutine());
    }

            private System.Collections.IEnumerator ForceReleaseCursorRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log("[ИНТЕРФЕЙС] Курсор принудительно возвращен на экран результатов.");
    }

    public void PressReturnToLobbyButton()
    {
        if (!IsServer) return;

                ReturnToLobbyClientRpc();
    }

    [ClientRpc]
    private void ReturnToLobbyClientRpc()
    {
                if (resultsPanel != null) resultsPanel.SetActive(false);
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        if (quizHUDPanel != null) quizHUDPanel.SetActive(false);

                Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

                PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
        foreach (var player in allPlayers)
        {
            if (player != null && player.IsOwner)
            {
                player.isGameActive = false;                 if (player.characterController != null) player.characterController.enabled = false;
            }
        }

        Debug.Log("[СЕТЬ] Все игроки успешно вернулись в лобби.");
    }
}