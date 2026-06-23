using UnityEngine;
using Unity.Netcode;

public class LavaTrigger : NetworkBehaviour
{
    [Header("Настройки лавы")]
    public float riseSpeed = 0.2f;           public float startYPosition = -5f;   
        private NetworkVariable<bool> isRising = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<float> currentY = new NetworkVariable<float>(-5f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Start()
    {
        if (IsServer)
        {
            currentY.Value = startYPosition;
        }
    }

    private void Update()
    {
        if (IsServer && isRising.Value)
        {
                        currentY.Value += riseSpeed * Time.deltaTime;
        }

                Vector3 targetPosition = new Vector3(transform.position.x, currentY.Value, transform.position.z);
        transform.position = targetPosition;
    }

        public void SetLavaRising(bool rising)
    {
        if (!IsServer) return;
        isRising.Value = rising;
        Debug.Log($"[СЕТЬ ЛАВЫ] Подъем лавы установлен в состояние: {rising}");
    }

        public void ResetLava()
    {
        if (!IsServer) return;
        currentY.Value = startYPosition;
        Debug.Log("[СЕТЬ ЛАВЫ] Позиция лавы успешно сброшена на старт.");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player == null)
        {
            player = other.GetComponentInParent<PlayerMovement>();
        }

        if (player != null && !player.isSpectator)
        {
            ulong deadPlayerId = player.OwnerClientId;
            Debug.Log($"[ЛАВА] Игрок с ID {deadPlayerId} коснулся лавы!");

            GameLoopManager gameManager = FindFirstObjectByType<GameLoopManager>();
            if (gameManager != null)
            {
                gameManager.ReportPlayerDeath(deadPlayerId);
            }
        }
    }
}