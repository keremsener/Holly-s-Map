using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio; // Mikseri kullanmak için bu kütüphane şart

public class MainMenu : MonoBehaviour
{
    [Header("UI Objeleri")]
    public GameObject buttonContainer; // Ana butonların içinde olduğu klasör obje
    public GameObject settingsPanel; 

    [Header("Butonlar")]
    public Button PlayButton;
    public Button SettingsButton;
    public Button QuitButton;
    public Button closeSettingsButton; 

    [Header("Ses Ayarları")]
    public AudioMixer mainMixer; // Yarattığımız ses mikseri
    public Slider volumeSlider;  // Ses çubuğu

    void Start()
    {
        // Başlangıçta paneli gizle, ana butonları göster
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (buttonContainer != null) buttonContainer.SetActive(true);

        // Buton Görevleri
        if (PlayButton != null) PlayButton.onClick.AddListener(PlayGame);
        if (SettingsButton != null) SettingsButton.onClick.AddListener(OpenSettings);
        if (QuitButton != null) QuitButton.onClick.AddListener(QuitGame);
        if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);

        // Slider değiştiğinde SetVolume fonksiyonunu çalıştır
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
        // Ayarlar açılınca butonları gizle, paneli göster
        if (buttonContainer != null) buttonContainer.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        // Ayarlar kapanınca paneli gizle, butonları geri getir
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (buttonContainer != null) buttonContainer.SetActive(true);
    }

    // Ses seviyesini ayarlayan logaritmik fonksiyon
    public void SetVolume(float volume)
    {
        // Unity sesleri desibel (dB) olarak alır, 0.0001 - 1 arası değeri dB'ye çeviriyoruz
        if (mainMixer != null)
        {
            mainMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
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