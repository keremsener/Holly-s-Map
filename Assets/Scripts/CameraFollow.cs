using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // Takip edilecek karakter
    public float smoothSpeed = 0.125f; // Kameranın yumuşaklık hızı
    public Vector3 offset = new Vector3(0f, 2f, -10f); // Kameranın duracağı mesafe

    void FixedUpdate()
    {
        if (target != null)
        {
            // Karakterin pozisyonu ile kameranın durması gereken yeri hesapla
            Vector3 desiredPosition = target.position + offset;
            
            // Yumuşak geçiş (Lerp) ile kamerayı o noktaya kaydır
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }
}