using UnityEngine;
using UnityEngine.UI;

public class NicknameValidator : MonoBehaviour
{
    public InputField nicknameInputField;     public Text errorText;                    public Button hostButton;                 public Button clientButton;           
    private void Start()
    {
        if (nicknameInputField != null)
        {
                        nicknameInputField.characterLimit = 21; 
            nicknameInputField.onValueChanged.AddListener(ValidateNickname);
        }

        if (errorText != null) errorText.gameObject.SetActive(false);
    }

    private void ValidateNickname(string text)
    {
        if (text.Length > 20)
        {
            if (errorText != null) errorText.gameObject.SetActive(true);             if (hostButton != null) hostButton.interactable = false;                 if (clientButton != null) clientButton.interactable = false;         }
        else
        {
            if (errorText != null) errorText.gameObject.SetActive(false);             if (hostButton != null) hostButton.interactable = true;                  if (clientButton != null) clientButton.interactable = true;
            
                        GameLoopManager loopManager = FindObjectOfType<GameLoopManager>();
            if (loopManager != null)
            {
                loopManager.SetPlayerNickname(text);
            }
        }
    }
}