using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Ana Butonlar")]
    public Button PlayButton;
    public Button SettingsButton;
    public Button QuitButton;

    [Header("Ayarlar Paneli")]
    public GameObject settingsPanel; 
    public Button closeSettingsButton; 

    void Start()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        if (PlayButton != null)
            PlayButton.onClick.AddListener(PlayGame);

        if (SettingsButton != null)
            SettingsButton.onClick.AddListener(OpenSettings);

        if (QuitButton != null)
            QuitButton.onClick.AddListener(QuitGame);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(CloseSettings);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene(1);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
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