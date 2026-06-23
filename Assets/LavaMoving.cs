using UnityEngine;
using Unity.Netcode;

public class LavaMoving : NetworkBehaviour
{
    // Сетевая переменная высоты лавы, чтобы клиенты плавно видели её подъем
    private NetworkVariable<float> targetLavaHeight = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public float lerpSpeed = 2f; // Скорость плавного скольжения лавы вверх

    void Start()
    {
        if (IsServer)
        {
            targetLavaHeight.Value = transform.position.y;
        }
    }

    void Update()
    {
        // Плавно перемещаем лаву к её целевой высоте на всех компьютерах
        float newY = Mathf.Lerp(transform.position.y, targetLavaHeight.Value, Time.deltaTime * lerpSpeed);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    // Этот метод вызывается сервером из GameLoopManager в конце каждого раунда
    [ServerRpc(RequireOwnership = false)]
    public void RaiseLavaServerRpc(float amount)
    {
        targetLavaHeight.Value += amount;
        Debug.Log($"[СЕТЬ ЛАВА] Лава поднимается! Новая высота: {targetLavaHeight.Value}");
    }
}