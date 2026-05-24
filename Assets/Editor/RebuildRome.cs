using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class RebuildRome : EditorWindow
{
    [MenuItem("Tools/Rebuild Rome Level")]
    public static void Rebuild()
    {
        string romePath = "Assets/Scenes/Rome.unity";
        
        // Rome sahnesini ac
        var scene = EditorSceneManager.OpenScene(romePath, OpenSceneMode.Single);

        var tm = Object.FindObjectOfType<Tilemap>();
        if (tm == null) { Debug.LogError("Tilemap bulunamadi!"); return; }

        // Rome icin Tile(17)_0 (veya benzeri) asset'ini bul
        TileBase romeTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Tiles/Tile (17)_0.asset");
        if (romeTile == null) romeTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiles/Tiles/Tile (30)_0.asset");
        if (romeTile == null) { Debug.LogError("Rome tile asset bulunamadi!"); return; }

        tm.ClearAllTiles();

        CleanUpOldLevel();

        BuildTerrain(tm, romeTile);
        PlaceMechanics();

        // CoinManager ayarini Rome icin dogrula (levelIndex = 2)
        var cm = Object.FindObjectOfType<CoinManager>();
        if (cm != null) { cm.levelIndex = 2; cm.totalCoins = 20; cm.levelScene = "Rome"; }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Rome sahnesi basariyla OZGUN bir harita olarak olusturuldu!");
    }

    static GameObject spikeTemplate;

    static void CleanUpOldLevel()
    {
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            if (go == null) continue;
            
            if (go.name.Contains("Enemy") || go.name.StartsWith("Coin_") || go.name.Contains("Spike") || 
                go.name.Contains("Checkpoint") || go.name.Contains("Bouncer") || go.name.Contains("MovingPlatform") ||
                go.name.Contains("MagicTorch") || go.name.Contains("PressurePlate") || go.name.Contains("ArenaGate") ||
                go.name.Contains("HintZone") || go.name.Contains("LevelEnd") || go.name.Contains("CaveManager"))
            {
                Object.DestroyImmediate(go);
            }
        }
    }

    static void BuildTerrain(Tilemap tm, TileBase tile)
    {
        int floorY = -5;

        // --- ZONE 1: Colosseum Ruins (X: -10 to 40) ---
        PaintRect(tm, tile, -10, 5, floorY - 5, floorY); // Baslangic platformu
        // Merdiven seklinde yikintilar (Dikey zikzak ziplamalar)
        PaintRect(tm, tile, 8, 12, floorY, floorY + 1);
        PaintRect(tm, tile, 15, 20, floorY + 5, floorY + 6);
        PaintRect(tm, tile, 23, 28, floorY + 10, floorY + 11);
        PaintRect(tm, tile, 31, 40, floorY + 14, floorY + 15); // Zirveye ulasis

        // --- ZONE 2: Broken Aqueducts (X: 40 to 100) ---
        // Genis ucurum, sadece MovingPlatformlar ve ara guvenli stunlar var
        PaintRect(tm, tile, 55, 57, floorY + 14, floorY + 15); // Havada kucuk sutun
        PaintRect(tm, tile, 77, 80, floorY + 10, floorY + 11); // Havada ikinci sutun
        PaintRect(tm, tile, 100, 110, floorY + 5, floorY + 6); // Inis ve toparlanma

        // --- ZONE 3: The Emperor's Trap (X: 110 to 160) ---
        // Zemin tamamen Spike, uzerinde minik basamaklar
        PaintRect(tm, tile, 110, 160, floorY - 10, floorY - 3); // Spike altı zemin
        
        // Zıplama taşları
        PaintRect(tm, tile, 113, 116, floorY, floorY + 1);
        PaintRect(tm, tile, 120, 123, floorY + 2, floorY + 3);
        PaintRect(tm, tile, 128, 131, floorY + 4, floorY + 5);
        PaintRect(tm, tile, 136, 139, floorY, floorY + 1);
        PaintRect(tm, tile, 144, 147, floorY + 3, floorY + 4);
        PaintRect(tm, tile, 152, 155, floorY + 6, floorY + 7);
        PaintRect(tm, tile, 158, 165, floorY + 5, floorY + 6); // Cikis

        // --- ZONE 4: Catacombs (X: 165 to 220) ---
        PaintRect(tm, tile, 165, 220, floorY + 5, floorY + 6); // Magara zemini
        PaintRect(tm, tile, 165, 220, floorY + 15, floorY + 20); // Magara tavani
        PaintRect(tm, tile, 165, floorY + 7, 167, floorY + 14); // Magara giris duvari (ustten kapali)
        // Ortada engel
        PaintRect(tm, tile, 185, 188, floorY + 7, floorY + 10);

        // --- ZONE 5: The Gladiator's Bouncers (X: 220 to 260) ---
        // Dev tirmianis duvari
        PaintRect(tm, tile, 222, 226, floorY + 5, floorY + 6); // Ilk bouncer platformu
        PaintRect(tm, tile, 230, 234, floorY + 15, floorY + 16); // Ikinci bouncer platformu
        PaintRect(tm, tile, 238, 242, floorY + 25, floorY + 26); // Ucuncu bouncer platformu
        PaintRect(tm, tile, 246, 260, floorY + 35, floorY + 36); // Tepe ulasis

        // --- ZONE 6: Triumphal Arch (X: 260 to 320) ---
        PaintRect(tm, tile, 260, 310, floorY + 35, floorY + 36);
        PaintRect(tm, tile, 290, 292, floorY + 37, floorY + 42); // Gate icin engel kemeri
        PaintRect(tm, tile, 310, 320, floorY + 35, floorY + 45); // Bitis duvari arkasi
    }

    static void PaintRect(Tilemap tm, TileBase tile, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
        for (int y = yMin; y <= yMax; y++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    static void PlaceMechanics()
    {
        int floorY = -5;

        // Player baslangicini guncelle
        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0, floorY + 2, 0);

        // --- ZONE 2: Broken Aqueducts (Moving Platforms) ---
        SpawnMovingPlatform("MP_1", new Vector3(42, floorY + 14, 0), new Vector3(53, floorY + 14, 0), 4f);
        SpawnMovingPlatform("MP_2", new Vector3(59, floorY + 14, 0), new Vector3(75, floorY + 10, 0), 3.5f);
        SpawnMovingPlatform("MP_3", new Vector3(82, floorY + 10, 0), new Vector3(98, floorY + 5, 0), 4f);
        
        SpawnCheckpoint("Checkpoint_1", new Vector3(105, floorY + 7, 0));

        // --- ZONE 3: The Emperor's Trap (Spikes) ---
        SpawnSpike("SpikeFloor_1", new Vector3(112, floorY - 2.4f, 0), 46); // Zemin komple diken

        // --- ZONE 4: Catacombs (Darkness & Secret) ---
        var caveMgr = new GameObject("CaveManager").AddComponent<CaveManager>();
        var caveCol = caveMgr.gameObject.AddComponent<BoxCollider2D>();
        caveCol.isTrigger = true;
        caveCol.size = new Vector2(55f, 20f);
        caveMgr.transform.position = new Vector3(192f, floorY + 10f, 0f);
        
        SpawnTorch("Torch_1", new Vector3(175, floorY + 7, 0), null);
        SpawnSecretWall("SecretWall_Rome", new Vector3(200, floorY + 7, 0), new Vector2(3, 4));
        SpawnCheckpoint("Checkpoint_2", new Vector3(215, floorY + 7, 0));

        // --- ZONE 5: Gladiator's Bouncers ---
        SpawnBouncer("Bouncer_1", new Vector3(224, floorY + 7, 0), 19f);
        SpawnBouncer("Bouncer_2", new Vector3(232, floorY + 17, 0), 19f);
        SpawnBouncer("Bouncer_3", new Vector3(240, floorY + 27, 0), 19f);

        // --- ZONE 6: Triumphal Arch (Puzzle) ---
        SpawnGate("ArenaGate_1", new Vector3(291, floorY + 39.5f, 0));
        SpawnPressurePlate("Plate_1", new Vector3(275, floorY + 36.15f, 0), "ArenaGate_1");
        
        SpawnLevelEnd("LevelEnd", new Vector3(305, floorY + 37, 0));

        // --- ENEMIES ---
        SpawnEnemy("Enemy_Colosseum", new Vector3(35, floorY + 15, 0)); // Bolge 1 sonu
        SpawnEnemy("Enemy_Aqueduct", new Vector3(105, floorY + 6, 0)); // Bolge 2 sonu
        SpawnEnemy("Enemy_Catacomb1", new Vector3(180, floorY + 6, 0)); // Magara ici
        SpawnEnemy("Enemy_Catacomb2", new Vector3(210, floorY + 6, 0)); // Magara cikisi
        SpawnEnemy("Enemy_Gladiator", new Vector3(255, floorY + 36, 0)); // Bouncer tirmansi sonu
        SpawnEnemy("Enemy_ArchGuard", new Vector3(285, floorY + 36, 0)); // Kapi oncesi muhafiz

        // --- COINS (Total 20) ---
        int coinIdx = 1;
        // Zone 1
        SpawnCoin("Coin_" + coinIdx++, new Vector3(10, floorY + 3, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(18, floorY + 8, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(26, floorY + 13, 0));
        
        // Zone 2
        SpawnCoin("Coin_" + coinIdx++, new Vector3(47, floorY + 16, 0)); // mp1 havasi
        SpawnCoin("Coin_" + coinIdx++, new Vector3(56, floorY + 16, 0)); // pillar 1
        SpawnCoin("Coin_" + coinIdx++, new Vector3(67, floorY + 13, 0)); // mp2 havasi
        SpawnCoin("Coin_" + coinIdx++, new Vector3(78, floorY + 12, 0)); // pillar 2
        SpawnCoin("Coin_" + coinIdx++, new Vector3(90, floorY + 8, 0));  // mp3 havasi

        // Zone 3
        SpawnCoin("Coin_" + coinIdx++, new Vector3(115, floorY + 3, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(130, floorY + 7, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(145, floorY + 6, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(153, floorY + 9, 0));

        // Zone 4
        SpawnCoin("Coin_" + coinIdx++, new Vector3(175, floorY + 12, 0)); // magara ici havada
        SpawnCoin("Coin_" + coinIdx++, new Vector3(186, floorY + 11, 0)); // engel uzeri
        SpawnCoin("Coin_" + coinIdx++, new Vector3(205, floorY + 7, 0)); // secret wall arkasi

        // Zone 5
        SpawnCoin("Coin_" + coinIdx++, new Vector3(228, floorY + 12, 0)); // bouncer atlayisi
        SpawnCoin("Coin_" + coinIdx++, new Vector3(236, floorY + 22, 0)); // bouncer atlayisi
        SpawnCoin("Coin_" + coinIdx++, new Vector3(244, floorY + 32, 0)); // bouncer atlayisi

        // Zone 6
        SpawnCoin("Coin_" + coinIdx++, new Vector3(265, floorY + 37, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(295, floorY + 37, 0)); // gate arkasi
    }

    // --- Helper Methods ---

    static void SpawnSpike(string name, Vector3 pos, float width)
    {
        // Temel killzone objesi
        var zone = new GameObject(name);
        zone.transform.position = pos;
        var col = zone.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 1f);
        col.offset = new Vector2(width / 2f - 0.5f, 0); // Sola dayali hizalama icin offset
        col.isTrigger = true;
        zone.AddComponent<SpikeKillZone>();

        // Gorselleri yukle (Desert Tile (1).png)
        Sprite spikeSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Desert Tile/Tile (1).png");

        for (int i = 0; i < width; i++)
        {
            var vis = new GameObject(name + "_Visual_" + i);
            vis.transform.SetParent(zone.transform);
            vis.transform.localPosition = new Vector3(i, 0, 0);
            var sr = vis.AddComponent<SpriteRenderer>();
            sr.sprite = spikeSprite;
            if (spikeSprite == null) sr.color = Color.red; // Fallback
        }
    }

    static void SpawnMovingPlatform(string name, Vector3 posA, Vector3 posB, float speed)
    {
        var go = new GameObject(name);
        go.transform.position = posA;
        go.transform.localScale = new Vector3(3f, 0.5f, 1f);
        go.layer = LayerMask.NameToLayer("Ground");
        
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.8f, 0.4f, 0.2f); // Roma temasi kiremit rengi
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(3f, 0.5f);
        
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        
        var ptA = new GameObject(name + "_A"); ptA.transform.position = posA; ptA.transform.SetParent(go.transform);
        var ptB = new GameObject(name + "_B"); ptB.transform.position = posB; ptB.transform.SetParent(go.transform);
        
        var mp = go.AddComponent<MovingPlatform>();
        mp.pointA = ptA.transform;
        mp.pointB = ptB.transform;
        mp.speed = speed;
        mp.startsMoving = true;
    }

    static void SpawnCheckpoint(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.2f, 0.8f, 0.2f);
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true; col.size = new Vector2(1f, 2f);
        go.AddComponent<Checkpoint>();
    }

    static void SpawnTorch(string name, Vector3 pos, GameObject activates)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.8f, 0.3f, 0.1f);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; col.isTrigger = true;
        var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        go.layer = LayerMask.NameToLayer("Enemy");
        
        var torch = go.AddComponent<MagicTorch>();
        torch.torchRenderer = sr;
        if (activates != null) torch.objectsToActivate = new GameObject[] { activates };
    }

    static void SpawnSecretWall(string name, Vector3 pos, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.5f, 0.3f, 0.2f); // Roma dugme rengi
        
        var col = go.AddComponent<BoxCollider2D>();
        col.size = size; col.isTrigger = true;
        
        var sw = go.AddComponent<SecretWall>();
        var dest = new GameObject(name + "_Dest");
        dest.transform.position = pos + new Vector3(15f, 0f, 0f);
        sw.teleportDestination = dest.transform;
    }

    static void SpawnBouncer(string name, Vector3 pos, float force)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 0.4f, 1f);
        go.layer = LayerMask.NameToLayer("Ground");
        
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.9f, 0.6f, 0.1f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.4f);
        
        var b = go.AddComponent<Bouncer>();
        b.bounceForce = force;
    }

    static void SpawnGate(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.3f, 0.2f, 0.1f);
        sr.sortingOrder = 2;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 5f);
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    static void SpawnPressurePlate(string name, Vector3 pos, string gateTarget)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.6f, 0.6f, 0.6f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.3f);
        go.layer = LayerMask.NameToLayer("Ground");
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        
        var pp = go.AddComponent<PressurePlate>();
        pp.plateRenderer = sr;
        pp.stayOpen = false;
        pp.closeDelay = 3.0f;
        
        EditorApplication.delayCall += () => {
            var gate = GameObject.Find(gateTarget);
            if (gate != null) pp.objectsToToggle = new GameObject[] { gate };
        };
    }

    static void SpawnLevelEnd(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = Color.yellow;
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 3f);
        col.isTrigger = true;
        go.AddComponent<LevelEndTrigger>();
    }

    static void SpawnCoin(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.85f, 0f);
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f; col.isTrigger = true;
        go.AddComponent<Coin>();
    }

    static void SpawnEnemy(string name, Vector3 pos)
    {
        // Level_01 (misir) veya Istanbul prefabindan Minotaur kopyalamak en guvenlisi
        // Eger sahnede onceden varsa onu klonla (CleanUpOldLevel hepsini siliyor gerci)
        // O yuzden Resources'tan veya asset database'den prefab cekecegiz.
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Images/enemies/PNG/Minotaur_03/Enemy_Minotaur.prefab");
        if (prefab == null) prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Minotaur.prefab");

        if (prefab != null)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // Istanbuldaki scale
        }
        else
        {
            Debug.LogWarning("Minotaur prefab bulunamadi: " + name);
        }
    }
}
