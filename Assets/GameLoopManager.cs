using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System.Collections.Generic;

public class GameLoopManager : NetworkBehaviour
{
    private static Dictionary<ulong, string> playerNamesDatabase = new Dictionary<ulong, string>();

    [System.Serializable]
    public struct QuizTrack
    {
        public Texture2D trackImage;
        public AudioClip trackAudio;
    }

    [Header("Точка наблюдения (Куб)")]
    public Transform spectatorSpawnPoint; 
    [Header("Ссылка на Генератор Карты")]
    public MapGenerator mapGenerator; 
    [Header("База данных треков")]
    public List<QuizTrack> quizDatabase = new List<QuizTrack>();

    [Header("Ссылки на HUD элементы")]
    public RawImage mainDisplayImage;
    public AudioSource audioSource;
    public Text timerText;
    public Text infoText;
    public Text roundText;
    public Text leaderboardText;

    [Header("Панель Вариантов")]
    public GameObject optionsGridPanel;

    [Header("4 Кнопки-варианта")]
    public RawImage[] optionButtonImages = new RawImage[4];

    [Header("Префаб Столба (с NetworkObject)")]
    public GameObject pillarPrefab;

    private Dictionary<ulong, int> playerScores = new Dictionary<ulong, int>();
    private List<ulong> correctAnswersOrder = new List<ulong>();
    private Dictionary<ulong, int> allPlayerChoices = new Dictionary<ulong, int>();
    private List<ulong> alivePlayers = new List<ulong>();
    private List<GameObject> spawnedPillars = new List<GameObject>();

    private int correctButtonIndexSync = -1;
    private int currentRound = 0;
    private int totalRounds = 5;
    private float roundTimer = 15f;
    private bool isTimerRunning = false;

    private Coroutine gameRoutine;

    public void StartQuizGame(int roundsCount)
    {
        if (!IsServer) return;

        totalRounds = roundsCount;
        currentRound = 0;

        playerScores.Clear();
        alivePlayers.Clear();

                foreach (var oldPillar in spawnedPillars)
        {
            if (oldPillar != null) oldPillar.GetComponent<NetworkObject>().Despawn();
        }
        spawnedPillars.Clear();

        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;
        int playerCount = connectedClients.Count;

                List<Vector3> spawnPoints = new List<Vector3>();
        if (mapGenerator != null)
        {
            spawnPoints = mapGenerator.CalculateSpawnPositions(playerCount);
        }
        else
        {
                        Debug.LogWarning("");
            for (int i = 0; i < playerCount; i++)
            {
                spawnPoints.Add(new Vector3(i * 12f, 0f, 0f));
            }
        }

        int index = 0;
        foreach (var client in connectedClients)
        {
            ulong clientId = client.ClientId;
            playerScores[clientId] = 0;
            alivePlayers.Add(clientId);

            Vector3 pillarSpawnPos = spawnPoints[index];
                        Vector3 playerSpawnPos = new Vector3(pillarSpawnPos.x, 3f, pillarSpawnPos.z);

                        if (pillarPrefab != null)
            {
                GameObject pillarInstance = Instantiate(pillarPrefab, pillarSpawnPos, Quaternion.identity);
                pillarInstance.GetComponent<NetworkObject>().Spawn(true);

                PlayerPillar pillarScript = pillarInstance.GetComponent<PlayerPillar>();
                if (pillarScript != null)
                {
                    pillarScript.targetPlayerId.Value = clientId;
                }

                spawnedPillars.Add(pillarInstance);
            }

                        PlayerMovement player = GetPlayerMovementByID(clientId);
            if (player != null)
            {
                                StartCoroutine(SafeTeleportRoutine(player, playerSpawnPos));
            }

            index++;
        }

        UpdateLeaderboard();

        if (gameRoutine != null) StopCoroutine(gameRoutine);
        gameRoutine = StartCoroutine(QuizGameFlowRoutine());

        LavaTrigger lava = FindFirstObjectByType<LavaTrigger>();
        if (lava != null)
        {
            lava.ResetLava();
            lava.SetLavaRising(true);
        }
    }
    
        private IEnumerator SafeTeleportRoutine(PlayerMovement player, Vector3 targetPos)
    {
                player.SetGameplayStateClientRpc(false);
        yield return new WaitForSeconds(0.1f);

                player.SyncSpawnPositionClientRpc(targetPos);
        yield return new WaitForSeconds(0.1f);

                player.SetGameplayStateClientRpc(true);
    }

    private IEnumerator QuizGameFlowRoutine()
    {
        while (currentRound < totalRounds && alivePlayers.Count > 0)
        {
            currentRound++;
            UpdateRoundTextClientRpc(currentRound, totalRounds);

                        correctAnswersOrder.Clear();
            allPlayerChoices.Clear();

                        SetOptionsGridActiveClientRpc(false);
            SetCanAnswerStateClientRpc(false);

                        float prepareTimer = 3f;
            while (prepareTimer > 0f)
            {
                string textMessage = $"ПРИГОТОВЬТЕСЬ!\nРаунд начнется через: {Mathf.CeilToInt(prepareTimer)}";
                UpdateInfoTextClientRpc(textMessage, Color.yellow);
                yield return new WaitForSeconds(1f);
                prepareTimer -= 1f;
            }

                        if (quizDatabase.Count > 0)
            {
                int randomIndex = Random.Range(0, quizDatabase.Count);
                correctButtonIndexSync = Random.Range(0, 4);

                List<int> fakeIndices = new List<int>();
                for (int i = 0; i < quizDatabase.Count; i++)
                {
                    if (i != randomIndex) fakeIndices.Add(i);
                }

                int[] roundButtonsLayout = new int[4];
                for (int i = 0; i < 4; i++)
                {
                    if (i == correctButtonIndexSync) roundButtonsLayout[i] = randomIndex;
                    else
                    {
                        if (fakeIndices.Count > 0)
                        {
                            int fIdx = Random.Range(0, fakeIndices.Count);
                            roundButtonsLayout[i] = fakeIndices[fIdx];
                            fakeIndices.RemoveAt(fIdx);
                        }
                        else roundButtonsLayout[i] = randomIndex;
                    }
                }

                                SetupRoundClientRpc(roundButtonsLayout, randomIndex);
            }

                        SetCanAnswerStateClientRpc(true);

                        roundTimer = 15f;
            isTimerRunning = true;
            while (roundTimer > 0f && allPlayerChoices.Count < alivePlayers.Count)
            {
                roundTimer -= Time.deltaTime;
                UpdateTimerTextClientRpc(Mathf.CeilToInt(roundTimer));
                yield return null;
            }
            isTimerRunning = false;

                        SetCanAnswerStateClientRpc(false);

                        RevealCorrectAnswerClientRpc(correctButtonIndexSync);

                        foreach (var clientID in new List<ulong>(alivePlayers))
            {
                if (allPlayerChoices.ContainsKey(clientID) && allPlayerChoices[clientID] == correctButtonIndexSync)
                {
                    int bonus = 100;
                    int orderIdx = correctAnswersOrder.IndexOf(clientID);
                    if (orderIdx == 0) bonus += 50;                      else if (orderIdx == 1) bonus += 25;

                    playerScores[clientID] += bonus;

                                        ChangePlayerPillarHeight(clientID, 1.5f);
                }
            }

            UpdateLeaderboard();

                        yield return new WaitForSeconds(3f);
        }

        FinishQuizGame();
    }

    private void FinishQuizGame()
    {
        LavaTrigger lava = FindFirstObjectByType<LavaTrigger>();
        if (lava != null)
        {
            lava.SetLavaRising(false);
            lava.ResetLava();
        }

        foreach (var pillar in spawnedPillars)
        {
            if (pillar != null && pillar.GetComponent<NetworkObject>().IsSpawned)
            {
                pillar.GetComponent<NetworkObject>().Despawn();
            }
        }
        spawnedPillars.Clear();

        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
        foreach (var player in allPlayers)
        {
            if (player != null && player.NetworkObject != null && player.NetworkObject.IsSpawned)
            {
                player.SetGameplayStateClientRpc(false);
            }
        }

                Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        string finalLeaderboard = "ФИНАЛЬНЫЕ ИТОГИ ВИКТОРИНЫ:\n";
        foreach (var entry in playerScores)
        {
            string name = playerNamesDatabase.ContainsKey(entry.Key) ? playerNamesDatabase[entry.Key] : $"Игрок {entry.Key}";
            finalLeaderboard += $"{name}: {entry.Value} PTS\n";
        }

        NetworkLobby lobby = FindFirstObjectByType<NetworkLobby>();
        if (lobby != null)
        {
            lobby.ShowFinalResults(finalLeaderboard);
        }
    }

        private void ChangePlayerPillarHeight(ulong targetClientId, float amount)
    {
        if (!IsServer) return;

        PlayerPillar[] pillars = FindObjectsByType<PlayerPillar>(FindObjectsInactive.Include);
        foreach (var pillar in pillars)
        {
                        if (pillar != null && pillar.targetPlayerId.Value == targetClientId)
            {
                pillar.ChangeHeightServerRpc(amount);
                break;
            }
        }
    }

    [ClientRpc]
    private void SetCanAnswerStateClientRpc(bool state)
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
        foreach (var player in allPlayers)
        {
                        if (player != null && player.IsOwner)
            {
                player.canAnswer = state;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitAnswerServerRpc(ulong clientId, int choiceIndex)
    {
        if (!isTimerRunning) return;
        if (allPlayerChoices.ContainsKey(clientId)) return;

        PlayerMovement player = GetPlayerMovementByID(clientId);
        if (player != null && player.isSpectator) return;

        allPlayerChoices[clientId] = choiceIndex;

        if (choiceIndex == correctButtonIndexSync)
        {
            correctAnswersOrder.Add(clientId);
        }

        HighlightSelectedButtonClientRpc(clientId, choiceIndex);
    }

    [ClientRpc]
    private void HighlightSelectedButtonClientRpc(ulong clientId, int choiceIndex)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            if (choiceIndex >= 0 && choiceIndex < 4 && optionButtonImages[choiceIndex] != null)
            {
                optionButtonImages[choiceIndex].color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }

            PlayerMovement localPlayer = GetPlayerMovementByID(clientId);
            if (localPlayer != null) localPlayer.canAnswer = false;
        }
    }

    [ClientRpc]
    private void SetOptionsGridActiveClientRpc(bool isActive)
    {
        if (optionsGridPanel != null) optionsGridPanel.SetActive(isActive);
    }

    [ClientRpc]
    private void SetupRoundClientRpc(int[] buttonsLayoutData, int correctTrackIdx)
    {
        if (optionsGridPanel != null) optionsGridPanel.SetActive(true);

        if (infoText != null)
        {
            infoText.text = "Угадайте мем по звуку!";
            infoText.color = Color.white;
        }

        for (int i = 0; i < 4; i++)
        {
            int trackIndexForButton = buttonsLayoutData[i];
            if (optionButtonImages[i] != null && trackIndexForButton < quizDatabase.Count)
            {
                optionButtonImages[i].texture = quizDatabase[trackIndexForButton].trackImage;
                optionButtonImages[i].color = Color.white;
            }
        }

        if (audioSource != null && correctTrackIdx < quizDatabase.Count && quizDatabase[correctTrackIdx].trackAudio != null)
        {
            audioSource.clip = quizDatabase[correctTrackIdx].trackAudio;
            audioSource.Play();
        }
    }

    [ClientRpc]
    private void UpdateInfoTextClientRpc(string text, Color textColor)
    {
        if (infoText != null)
        {
            infoText.text = text;
            infoText.color = textColor;
        }
    }

    [ClientRpc]
    private void UpdateTimerTextClientRpc(int secondsLeft)
    {
        if (timerText != null) timerText.text = $"ОСТАЛОСЬ ВРЕМЕНИ: {secondsLeft}с";
    }

    [ClientRpc]
    private void UpdateRoundTextClientRpc(int current, int total)
    {
        if (roundText != null) roundText.text = $"РАУНД: {current} / {total}";
    }

    [ClientRpc]
    private void RevealCorrectAnswerClientRpc(int correctIdx)
    {
        if (audioSource != null) audioSource.Stop();

        for (int i = 0; i < 4; i++)
        {
            if (optionButtonImages[i] != null)
            {
                if (i == correctIdx) optionButtonImages[i].color = new Color(0.2f, 1f, 0.2f, 1f);
                else optionButtonImages[i].color = new Color(1f, 0.2f, 0.2f, 1f);
            }
        }
    }

    private PlayerMovement GetPlayerMovementByID(ulong clientId)
    {
        PlayerMovement[] allPlayers = FindObjectsByType<PlayerMovement>(FindObjectsInactive.Include);
        foreach (var player in allPlayers)
        {
            if (player != null && player.OwnerClientId == clientId) return player;
        }
        return null;
    }

    public void ReportPlayerDeath(ulong clientId)
    {
        if (!IsServer) return;

        if (alivePlayers.Contains(clientId))
        {
            alivePlayers.Remove(clientId);

                        PlayerMovement player = GetPlayerMovementByID(clientId);
            if (player != null)
            {
                                player.isSpectator = true;

                                if (spectatorSpawnPoint != null)
                {
                                        StartCoroutine(SafeTeleportToSpectatorRoutine(player, spectatorSpawnPoint.position));
                }
                else
                {
                                        Vector3 defaultSpectatorPos = new Vector3(player.transform.position.x, 30f, player.transform.position.z);
                    StartCoroutine(SafeTeleportToSpectatorRoutine(player, defaultSpectatorPos));
                    Debug.LogWarning("");
                }
            }

                        if (alivePlayers.Count == 0)
            {
                if (gameRoutine != null) StopCoroutine(gameRoutine);
                FinishQuizGame();
            }
        }
    }

    private IEnumerator SafeTeleportToSpectatorRoutine(PlayerMovement player, Vector3 spectatorPos)
    {
                player.SetGameplayStateClientRpc(false);
        yield return new WaitForSeconds(0.1f);

                player.SyncSpawnPositionClientRpc(spectatorPos);
        yield return new WaitForSeconds(0.1f);

                player.SetGameplayStateClientRpc(true);
    }

    private void UpdateLeaderboard()
    {
        string lbText = "ТАБЛИЦА ЛИДЕРОВ:\n";
        foreach (var entry in playerScores)
        {
            string displayName = playerNamesDatabase.ContainsKey(entry.Key) ? playerNamesDatabase[entry.Key] : $"Игрок {entry.Key}";
            lbText += $"{displayName}: {entry.Value} PTS\n";
        }
        UpdateLeaderboardClientRpc(lbText);
    }

    [ClientRpc]
    private void UpdateLeaderboardClientRpc(string fullText)
    {
        if (leaderboardText != null) leaderboardText.text = fullText;
    }

    public void SetPlayerNickname(string nickname)
    {
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        SetPlayerNickname(localId, nickname);
    }

    public void SetPlayerNickname(ulong clientId)
    {
        SetPlayerNickname(clientId, $"Игрок {clientId}");
    }

    public void SetPlayerNickname(ulong clientId, string nickname)
    {
        if (playerNamesDatabase == null) playerNamesDatabase = new Dictionary<ulong, string>();
        playerNamesDatabase[clientId] = nickname;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNicknameServerRpc(ulong clientId, string nickname)
    {
        SetPlayerNickname(clientId, nickname);
        SyncNicknameClientRpc(clientId, nickname);
    }

    [ClientRpc]
    private void SyncNicknameClientRpc(ulong clientId, string nickname)
    {
        if (!IsServer)
        {
            if (playerNamesDatabase == null) playerNamesDatabase = new Dictionary<ulong, string>();
            playerNamesDatabase[clientId] = nickname;
        }
    }
}