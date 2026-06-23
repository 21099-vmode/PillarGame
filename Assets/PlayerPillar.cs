using UnityEngine;
using Unity.Netcode;

public class PlayerPillar : NetworkBehaviour
{
        public NetworkVariable<ulong> targetPlayerId = new NetworkVariable<ulong>(
        9999,         NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    [Header("Настройки высоты")]
    public float targetHeight = 0f;
    public float changeSpeed = 5f;

    private void Update()
    {
                Vector3 targetPos = new Vector3(transform.position.x, targetHeight, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * changeSpeed);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ChangeHeightServerRpc(float amount)
    {
        if (!IsServer) return;

        targetHeight += amount;
                SyncHeightClientRpc(targetHeight);
    }

    [ClientRpc]
    private void SyncHeightClientRpc(float newHeight)
    {
        targetHeight = newHeight;
    }
}