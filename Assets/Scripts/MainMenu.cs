using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio; // Mikser library

public class MainMenu : MonoBehaviour
{
    [Header("UI Objeleri")]
    public GameObject buttonContainer;
    public GameObject settingsPanel; 

    [Header("Butonlar")]
    public Button PlayButton;
    public Button SettingsButton;
    public Button QuitButton;
    public Button closeSettingsButton; 

    [Header("Ses Ayarları")]
    public AudioMixer mainMixer;
    public Slider volumeSlider;

    void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (buttonContainer != null) buttonContainer.SetActive(true);

        if (PlayButton != null) PlayButton.onClick.AddListener(PlayGame);
        if (SettingsButton != null) SettingsButton.onClick.AddListener(OpenSettings);
        if (QuitButton != null) QuitButton.onClick.AddListener(QuitGame);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

        if (volumeSlider != null)
        {
            volumeSlider.onValueChanged.AddListener(SetVolume);
        }
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void OpenSettings()
    {
        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (buttonContainer != null) buttonContainer.SetActive(true);
    }

    public void SetVolume(float volume)
    {
        if (mainMixer != null)
        {
            float safeVolume = Mathf.Clamp(volume, 0.0001f, 1f);
            mainMixer.SetFloat("MusicVolume", Mathf.Log10(safeVolume) * 20);
        }
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çıkış Yapıldı!");
        Application.Quit(); 

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}