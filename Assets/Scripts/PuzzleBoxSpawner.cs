using UnityEngine;

/// <summary>
/// Monitors a list of enemies. When all are defeated, spawns/drops a pushable box from the sky.
/// </summary>
public class PuzzleBoxSpawner : MonoBehaviour
{
    [Header("Puzzle Settings")]
    [Tooltip("The enemies that must be defeated to spawn the box.")]
    [SerializeField] private GameObject[] enemiesToDefeat;
    
    [Tooltip("The prefab or existing scene object for the pushable box.")]
    [SerializeField] private GameObject pushableBoxPrefab;
    
    [Tooltip("Where the box should spawn (usually high above the platform).")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Effects")]
    [SerializeField] private AudioClip spawnSound;
    [SerializeField] private ParticleSystem spawnVFX;
    
    private bool isSolved = false;

    private void Update()
    {
        if (isSolved) return;

        bool allDead = true;
        foreach (GameObject enemy in enemiesToDefeat)
        {
            if (enemy != null && enemy.activeInHierarchy)
            {
                allDead = false;
                break;
            }
        }

        if (allDead)
        {
            SpawnBox();
        }
    }

    private void SpawnBox()
    {
        isSolved = true;
        
        Debug.Log("🧩 Puzzle solved! Spawning pushable box.");
        
        if (pushableBoxPrefab != null && spawnPoint != null)
        {
            GameObject box = Instantiate(pushableBoxPrefab, spawnPoint.position, Quaternion.identity);
            
            // Make sure it has a Rigidbody2D to fall and be pushed
            Rigidbody2D rb = box.GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = box.AddComponent<Rigidbody2D>();
                rb.mass = 3f; // Heavy enough to not bounce wildly, but pushable
                rb.gravityScale = 2f;
                rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                rb.freezeRotation = true; // Box shouldn't roll
            }
            else
            {
                rb.freezeRotation = true;
            }

            // Box needs a collider
            if (box.GetComponent<Collider2D>() == null)
            {
                var col = box.AddComponent<BoxCollider2D>();
                // A friction material would be nice so it slides nicely when pushed
            }

            // Box needs to be on Ground layer so player can jump on it
            box.layer = LayerMask.NameToLayer("Ground");
        }

        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, spawnPoint.position);
        }

        if (spawnVFX != null)
        {
            Instantiate(spawnVFX, spawnPoint.position, Quaternion.identity);
        }
    }
}
