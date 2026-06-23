using UnityEngine;
using Unity.Netcode;

public class PlayerMovement : NetworkBehaviour
{
    [HideInInspector] public bool canAnswer = false;
    [HideInInspector] public bool isGameActive = false;
    [HideInInspector] public bool isSpectator = false;

    [Header("Ссылки")]
    public Camera playerCamera;
    public CharacterController characterController;

    [Header("Настройки движения")]
    public float moveSpeed = 7f;
    public float lookSpeed = 2f;

    private Vector3 velocity;
    private float xRotation = 0f;

    public override void OnNetworkSpawn()
    {
        characterController = GetComponent<CharacterController>();

        if (IsOwner)
        {
            if (characterController != null) characterController.enabled = true;

            if (playerCamera != null)
            {
                playerCamera.gameObject.SetActive(true);
                AudioListener listener = playerCamera.GetComponent<AudioListener>();
                if (listener == null) listener = playerCamera.gameObject.AddComponent<AudioListener>();
                listener.enabled = true;
            }

            isGameActive = false;
            canAnswer = false;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (playerCamera != null) playerCamera.gameObject.SetActive(false);
        }
    }

    [ClientRpc]
    public void SetGameplayStateClientRpc(bool active)
    {
        isGameActive = active;
        canAnswer = active;

        if (IsOwner)
        {
            if (characterController != null)
            {
                characterController.enabled = false;
                characterController.enabled = active;
            }

            if (isSpectator)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return; 
            }

            
            if (active)
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

    private void Update()
    {
        if (!IsOwner) return;

        if (!isGameActive) return;

        float rotationX = Input.GetAxis("Mouse X") * lookSpeed;
        float rotationY = Input.GetAxis("Mouse Y") * lookSpeed;

        transform.Rotate(Vector3.up * rotationX);

        xRotation -= rotationY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        if (playerCamera != null) playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(move * moveSpeed * Time.deltaTime);

            if (characterController.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += Physics.gravity.y * Time.deltaTime;
            characterController.Move(velocity * Time.deltaTime);
        }
        if (canAnswer)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1)) SubmitAnswer(0);
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2)) SubmitAnswer(1);
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3)) SubmitAnswer(2);
            if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4)) SubmitAnswer(3);
        }
    }

    private void SubmitAnswer(int buttonIndex)
    {
        GameLoopManager manager = FindFirstObjectByType<GameLoopManager>();
        if (manager != null)
        {
            manager.SubmitAnswerServerRpc(NetworkManager.Singleton.LocalClientId, buttonIndex);
        }
    }

    [ClientRpc]
    public void SyncSpawnPositionClientRpc(Vector3 newPos)
    {
        if (characterController != null)
        {
            characterController.enabled = false; 
        }
        transform.position = newPos; 
        velocity = Vector3.zero;  
        if (characterController != null && isGameActive)
        {
            characterController.enabled = true;
        }
        Debug.Log($"[ТЕЛЕПОРТ] Игрок {OwnerClientId} успешно перемещен на позицию: {newPos}");
    }
}