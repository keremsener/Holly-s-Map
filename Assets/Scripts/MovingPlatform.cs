using UnityEngine;

/// <summary>
/// Hareketli platform. İki nokta arasında gidip gelir.
/// Oyuncu üzerine binerse onunla beraber hareket eder.
/// </summary>
public class MovingPlatform : MonoBehaviour
{
    [Header("Movement")]
    public Transform pointA;
    public Transform pointB;
    public float speed = 2f;
    public float waitTime = 0.5f;

    [Header("Activation")]
    public bool startsMoving = true;
    
    private Transform target;
    private bool waiting = false;
    private float waitTimer = 0f;
    private Vector3 lastPos;
    private Transform rider;

    private void Start()
    {
        lastPos = transform.position;
        if (pointB != null)
            target = startsMoving ? pointB : pointA;
    }

    private void Update()
    {
        if (pointA == null || pointB == null) return;
        if (waiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f) waiting = false;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // Rider'ı da taşı
        if (rider != null)
        {
            Vector3 delta = transform.position - lastPos;
            rider.position += delta;
        }

        if (Vector3.Distance(transform.position, target.position) < 0.05f)
        {
            target = (target == pointB) ? pointA : pointB;
            waiting = true;
            waitTimer = waitTime;
        }

        lastPos = transform.position;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
            rider = other.transform;
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
            rider = null;
    }

    public void SetMoving(bool active) => enabled = active;
}
