using UnityEngine;

public class UISway : MonoBehaviour
{
    [Header("Nefes Ayarları")]
    public float swayAmount = 15f; // Ekranda kaç piksel kayacağı (UI olduğu için sayılar büyük olmalı)
    public float swaySpeed = 1f;   // Nefes alma hızı 

    private RectTransform rectTransform;
    private Vector2 startPos;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startPos = rectTransform.anchoredPosition; // UI'ın başlangıç noktasını hafızaya al
    }

    void Update()
    {
        // Sinüs dalgasıyla pürüzsüz kayma
        float newY = startPos.y + Mathf.Sin(Time.time * swaySpeed) * swayAmount;
        float newX = startPos.x + Mathf.Cos(Time.time * (swaySpeed * 0.5f)) * (swayAmount * 0.5f); 

        // Objeyi yeni piksel pozisyonuna yerleştir
        rectTransform.anchoredPosition = new Vector2(newX, newY);
    }
}