using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("Элементы UI Настроек")]
    public Slider volumeSlider;           public Slider sensitivitySlider;  
    [Header("Значения по умолчанию")]
    public float defaultVolume = 0.5f;
    public float defaultSensitivity = 2f;

        public static float MouseSensitivity = 2f;

    void Start()
    {
                float savedVolume = PlayerPrefs.GetFloat("GameVolume", defaultVolume);
        if (volumeSlider != null)
        {
            volumeSlider.value = savedVolume;
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
        AudioListener.volume = savedVolume; 
                MouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", defaultSensitivity);
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = MouseSensitivity;
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }
    }

        public void SetVolume(float value)
    {
        AudioListener.volume = value;         PlayerPrefs.SetFloat("GameVolume", value);
        PlayerPrefs.Save();
    }

        public void SetSensitivity(float value)
    {
        MouseSensitivity = value;
        PlayerPrefs.SetFloat("MouseSensitivity", value);
        PlayerPrefs.Save();
    }
}