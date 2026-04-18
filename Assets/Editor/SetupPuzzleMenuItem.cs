using UnityEngine;
using UnityEditor;
using System.Linq;

public static class SetupPuzzleMenuItem
{
    [MenuItem("Tools/Setup Puzzle Zone")]
    public static void SetupPuzzle()
    {
        var enemies = GameObject.FindObjectsOfType<EnemyAI>().OrderByDescending(e => e.transform.position.x).ToList();
        if (enemies.Count < 2)
        {
            Debug.LogError("Not enough enemies found!");
            return;
        }

        var enemy1 = enemies[0].gameObject;
        var enemy2 = enemies[1].gameObject;

        Debug.Log("Enemy 1 (Rightmost): " + enemy1.name + " at X: " + enemy1.transform.position.x);
        Debug.Log("Enemy 2 (Next Right): " + enemy2.name + " at X: " + enemy2.transform.position.x);

        var pmGo = GameObject.Find("PuzzleManager_RightZone");
        if (pmGo == null) pmGo = new GameObject("PuzzleManager_RightZone");

        var spawner = pmGo.GetComponent<PuzzleBoxSpawner>();
        if (spawner == null) spawner = pmGo.AddComponent<PuzzleBoxSpawner>();

        var spawnerType = spawner.GetType();
        var enemiesArray = new GameObject[] { enemy1, enemy2 };
        spawnerType.GetField("enemiesToDefeat", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(spawner, enemiesArray);

        string prefabPath = "Assets/PushableBox.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (prefab == null)
        {
            var boxGo = new GameObject("PushableBox");
            var sr = boxGo.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            sr.color = new Color(0.55f, 0.35f, 0.15f); // Wooden brown
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(1.5f, 1.5f);

            var rb = boxGo.AddComponent<Rigidbody2D>();
            rb.mass = 3f;
            rb.gravityScale = 2f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.freezeRotation = true;

            var col = boxGo.AddComponent<BoxCollider2D>();
            col.size = new Vector2(1.5f, 1.5f);

            PhysicsMaterial2D boxMat = new PhysicsMaterial2D("BoxFriction");
            boxMat.friction = 0.5f;
            boxMat.bounciness = 0f;
            AssetDatabase.CreateAsset(boxMat, "Assets/BoxFriction.physicsMaterial2D");
            col.sharedMaterial = boxMat;

            boxGo.layer = LayerMask.NameToLayer("Default");
            
            // Add a small bounce script so it looks good when falling
            
            prefab = PrefabUtility.SaveAsPrefabAsset(boxGo, prefabPath);
            Object.DestroyImmediate(boxGo);
        }

        spawnerType.GetField("pushableBoxPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(spawner, prefab);

        float spawnX = (enemy1.transform.position.x + enemy2.transform.position.x) / 2f;
        float spawnY = Mathf.Max(enemy1.transform.position.y, enemy2.transform.position.y) + 8f;

        var spawnPoint = GameObject.Find("BoxSpawnPoint");
        if (spawnPoint == null) spawnPoint = new GameObject("BoxSpawnPoint");
        
        spawnPoint.transform.position = new Vector3(spawnX, spawnY, 0f);
        spawnPoint.transform.SetParent(pmGo.transform);

        spawnerType.GetField("spawnPoint", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(spawner, spawnPoint.transform);

        Debug.Log("Puzzle Setup Complete!");
    }
}
