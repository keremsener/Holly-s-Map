using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class RebuildIstanbul : EditorWindow
{
    [MenuItem("Tools/Rebuild Istanbul Level")]
    public static void Rebuild()
    {
        string level1Path = "Assets/Scenes/Level_01.unity";
        string istanbulPath = "Assets/Scenes/Istanbul.unity";

        // 1. Level_01'i kopyalayarak Istanbul sahnesinin ustune yaz
        // (Boylece Player, Camera, GameManager'lar hazir gelir)
        if (!AssetDatabase.CopyAsset(level1Path, istanbulPath))
        {
            // Eger zaten varsa silip tekrar kopyalayalim
            AssetDatabase.DeleteAsset(istanbulPath);
            AssetDatabase.CopyAsset(level1Path, istanbulPath);
        }

        // 2. Istanbul sahnesini ac
        var scene = EditorSceneManager.OpenScene(istanbulPath, OpenSceneMode.Single);

        // 3. Tilemap'i temizle
        var tm = Object.FindObjectOfType<Tilemap>();
        if (tm == null) { Debug.LogError("Tilemap bulunamadi!"); return; }
        
        // Örnek tile'i al (herhangi bir dolu hucreden)
        TileBase sampleTile = null;
        var bounds = tm.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax && sampleTile == null; x++)
        for (int y = bounds.yMin; y < bounds.yMax && sampleTile == null; y++)
        {
            sampleTile = tm.GetTile(new Vector3Int(x, y, 0));
        }

        // Simdi tamamen temizle
        tm.ClearAllTiles();

        // 4. Eski mekanikleri ve dusmanlari temizle
        CleanUpOldLevel();

        // 5. YENI HARITAYI INSA ET
        BuildTerrain(tm, sampleTile);
        PlaceMechanics();

        // LevelManager (CoinManager) LevelIndex ayarini yap
        var cm = Object.FindObjectOfType<CoinManager>();
        if (cm != null) { cm.levelIndex = 1; cm.totalCoins = 20; }

        EditorSceneManager.SaveScene(scene);
        Debug.Log("Istanbul sahnesi basariyla olusturuldu!");
    }

    static void CleanUpOldLevel()
    {
        // Temizlenecek eski objelerin listesi (Enemy, Bouncer, SpikeZone, Coin, vb.)
        string[] tagsToDestroy = { "Enemy", "Coin", "Spike", "Checkpoint", "Bouncer", "MovingPlatform", "MagicTorch", "PressurePlate", "Gate", "HintZone", "LevelEnd" };
        
        // Tum objeleri gez
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            if (go == null) continue;
            
            if (go.name.Contains("Enemy") || go.name.Contains("Coin") || go.name.Contains("Spike") || 
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

        // --- BOLGE 1: Baslangic ve Isinma (x: -10 to 30) ---
        PaintRect(tm, tile, -10, 10, floorY - 5, floorY); // Safe start
        PaintRect(tm, tile, 13, 18, floorY - 5, floorY); // Kucuk atlayis (Gap: 2)
        PaintRect(tm, tile, 21, 30, floorY - 5, floorY); // Atlayis (Gap: 2)

        // --- BOLGE 2: SpikeZone ve Alternatif Yol (x: 30 to 70) ---
        // Alt yol (Riskli ama duz)
        PaintRect(tm, tile, 30, 70, floorY - 15, floorY - 10);
        
        // Ust yol (Platformlar) - Gaps max 3 birim
        PaintRect(tm, tile, 33, 37, floorY + 2, floorY + 3);
        PaintRect(tm, tile, 40, 44, floorY + 3, floorY + 4);
        PaintRect(tm, tile, 47, 51, floorY + 4, floorY + 5);
        PaintRect(tm, tile, 54, 58, floorY + 5, floorY + 6);
        PaintRect(tm, tile, 61, 70, floorY + 2, floorY + 5);

        // --- BOLGE 3: Hareketli Platform Boslugu (x: 70 to 110) ---
        // Zemin yok, hareketli platformlar koyacagiz
        PaintRect(tm, tile, 105, 120, floorY - 5, floorY); // Inis platformu (Gap kapatildi)
        
        // --- BOLGE 4: Karanlik Magara ve Mesale Puzzle (x: 120 to 160) ---
        PaintRect(tm, tile, 120, 160, floorY - 5, floorY); // Zemin
        PaintRect(tm, tile, 120, 160, floorY + 10, floorY + 15); // Tavan
        PaintRect(tm, tile, 120, floorY, 122, floorY + 10); // Sol duvar
        
        // --- BOLGE 5: Bouncer Zinciri (x: 160 to 200) ---
        PaintRect(tm, tile, 160, 165, floorY - 5, floorY); // Atlayis noktasi
        // Bouncer adaciklari birbirine cok daha yakin
        PaintRect(tm, tile, 169, 171, floorY - 10, floorY - 8);
        PaintRect(tm, tile, 175, 177, floorY - 5, floorY - 3);
        PaintRect(tm, tile, 181, 183, floorY, floorY + 2);
        PaintRect(tm, tile, 187, 210, floorY - 5, floorY); // Inis

        // --- BOLGE 6: Kapi Puzzle ve Kosu (x: 210 to 260) ---
        PaintRect(tm, tile, 210, 260, floorY - 5, floorY);
        // Duvar engeli (Gate buraya gelecek)
        PaintRect(tm, tile, 240, 241, floorY + 1, floorY + 6);

        // --- BOLGE 7: Bitis Alani (x: 260 to 280) ---
        PaintRect(tm, tile, 260, 280, floorY - 5, floorY);
        // Bitis duvar arkasi
        PaintRect(tm, tile, 280, 290, floorY - 5, floorY + 15);
    }

    static void PaintRect(Tilemap tm, TileBase tile, int xMin, int xMax, int yMin, int yMax)
    {
        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                tm.SetTile(new Vector3Int(x, y, 0), tile);
            }
        }
    }

    static void PlaceMechanics()
    {
        int floorY = -5;

        // Player baslangicini guncelle
        var player = GameObject.Find("Player");
        if (player != null) player.transform.position = new Vector3(0, floorY + 2, 0);

        // -- Spikes (Bolge 2) --
        // Spike'lar tile hizasinin ustunde, net gorunur (y = floorY - 9.5)
        SpawnSpike("Spike_1", new Vector3(40, floorY - 9.4f, 0), 6);
        SpawnSpike("Spike_2", new Vector3(50, floorY - 9.4f, 0), 6);
        SpawnSpike("Spike_3", new Vector3(60, floorY - 9.4f, 0), 6);

        // -- Moving Platforms (Bolge 3) --
        // Ilk platform x=72 baslar, x=82 gider. Ikinci platform x=85 baslar x=100 gider. Gaps makul.
        SpawnMovingPlatform("MP_1", new Vector3(73, floorY, 0), new Vector3(82, floorY, 0), 4f);
        SpawnMovingPlatform("MP_2", new Vector3(85, floorY - 1, 0), new Vector3(102, floorY + 2, 0), 3.5f);

        // -- Checkpoint 1 --
        SpawnCheckpoint("Checkpoint_1", new Vector3(110, floorY + 2, 0));

        // -- Cave Manager & Torch (Bolge 4) --
        var caveMgr = new GameObject("CaveManager").AddComponent<CaveManager>();
        var caveCol = caveMgr.gameObject.AddComponent<BoxCollider2D>();
        caveCol.isTrigger = true;
        caveCol.size = new Vector2(40f, 20f);
        caveMgr.transform.position = new Vector3(140f, floorY + 5f, 0f);
        
        SpawnTorch("Torch_1", new Vector3(140, floorY + 2, 0), null); // Sadece aydinlatma
        
        // Magara icinde gizli duvar
        SpawnSecretWall("SecretWall_1", new Vector3(150, floorY + 1, 0), new Vector2(3, 4));

        // -- Bouncers (Bolge 5) --
        SpawnBouncer("Bouncer_1", new Vector3(170, floorY - 7, 0), 18f);
        SpawnBouncer("Bouncer_2", new Vector3(176, floorY - 2, 0), 18f);
        SpawnBouncer("Bouncer_3", new Vector3(182, floorY + 3, 0), 18f);

        // -- Checkpoint 2 --
        SpawnCheckpoint("Checkpoint_2", new Vector3(195, floorY + 2, 0));

        // -- Pressure Plate & Gate (Bolge 6) --
        SpawnGate("Gate_1", new Vector3(240.5f, floorY + 3.5f, 0));
        SpawnPressurePlate("Plate_1", new Vector3(220, floorY + 0.15f, 0), "Gate_1");

        // -- Level End (Bolge 7) --
        SpawnLevelEnd("LevelEnd", new Vector3(275, floorY + 2, 0));

        // -- Coins (Bolge 1'den Bolge 7'ye 25 adet) --
        int coinIdx = 1;
        // Bolge 1
        SpawnCoin("Coin_" + coinIdx++, new Vector3(5, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(15, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(25, floorY + 2, 0));
        
        // Bolge 2 (Ust yol)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(35f, floorY + 4, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(42f, floorY + 5, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(49f, floorY + 6, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(56f, floorY + 7, 0));

        // Bolge 2 (Alt yol riskli - Spike arkasi)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(40, floorY - 7, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(50, floorY - 7, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(60, floorY - 7, 0));

        // Bolge 3 (Platform ustleri)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(77, floorY + 3, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(95, floorY + 5, 0));

        // Bolge 4 (Magara)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(125, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(135, floorY + 2, 0));
        // Gizli duvar arkasi
        SpawnCoin("Coin_" + coinIdx++, new Vector3(150, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(151, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(152, floorY + 2, 0));

        // Bolge 5 (Bouncer havadaki)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(170, floorY, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(176, floorY + 5, 0));

        // Bolge 6 (Kosu)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(225, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(230, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(235, floorY + 2, 0));
        
        // Bolge 7 (Kapi arkasi odul)
        SpawnCoin("Coin_" + coinIdx++, new Vector3(245, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(255, floorY + 2, 0));
        SpawnCoin("Coin_" + coinIdx++, new Vector3(265, floorY + 2, 0));
    }

    // --- Helper Methods to Spawn Mechanics ---

    static void SpawnSpike(string name, Vector3 pos, float width)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = Color.red; // Dikeni temsil eder
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(width, 1f);
        col.isTrigger = true;
        go.AddComponent<SpikeKillZone>();
    }

    static void SpawnMovingPlatform(string name, Vector3 posA, Vector3 posB, float speed)
    {
        var go = new GameObject(name);
        go.transform.position = posA;
        go.transform.localScale = new Vector3(3f, 0.5f, 1f);
        go.layer = LayerMask.NameToLayer("Ground");
        
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.2f, 0.6f, 1f);
        var col = go.GetComponent<BoxCollider2D>();
        if(col == null) col = go.AddComponent<BoxCollider2D>();
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
        sr.color = new Color(0.5f, 0.3f, 0.1f);
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
        sr.color = new Color(0.4f, 0.4f, 0.4f); // Duvara benzer renk
        
        var col = go.GetComponent<BoxCollider2D>();
        if(col == null) col = go.AddComponent<BoxCollider2D>();
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
        sr.color = new Color(1f, 0.4f, 0.1f);
        var col = go.GetComponent<BoxCollider2D>();
        if(col == null) col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.4f);
        
        var b = go.AddComponent<Bouncer>();
        b.bounceForce = force;
    }

    static void SpawnGate(string name, Vector3 pos)
    {
        var go = new GameObject(name);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.4f, 0.1f, 0.05f);
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
        sr.color = new Color(0.6f, 0.5f, 0.2f);
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.3f);
        go.layer = LayerMask.NameToLayer("Ground");
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        
        var pp = go.AddComponent<PressurePlate>();
        pp.plateRenderer = sr;
        pp.stayOpen = false;
        pp.closeDelay = 2.0f; // Kapi 2 saniye acik kalsin
        
        // Gate link (Sonradan baglayalim)
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
}
