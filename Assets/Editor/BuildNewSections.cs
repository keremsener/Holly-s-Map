using UnityEngine;
using UnityEditor;
using UnityEngine.Tilemaps;

public static class BuildNewSections
{
    [MenuItem("Tools/Build New Level Sections")]
    public static void Build()
    {
        var grid = UnityEngine.Object.FindObjectOfType<Grid>();
        if (grid == null) { Debug.LogError("Grid bulunamadı!"); return; }
        var tilemap = grid.GetComponentInChildren<Tilemap>();
        if (tilemap == null) { Debug.LogError("Tilemap bulunamadı!"); return; }

        // Mevcut bir tile'ı örnekle (sahne içinde var olan bir koordinattan al)
        TileBase sampleTile = null;
        var bounds = tilemap.cellBounds;
        for (int x = bounds.xMin; x < bounds.xMax && sampleTile == null; x++)
        for (int y = bounds.yMin; y < bounds.yMax && sampleTile == null; y++)
        {
            var t = tilemap.GetTile(new Vector3Int(x, y, 0));
            if (t != null) sampleTile = t;
        }

        if (sampleTile == null) { Debug.LogError("Örnek tile bulunamadı!"); return; }
        Debug.Log("Örnek tile: " + sampleTile.name);

        // ── BÖLGE A: Zıplama Bölümü ──
        // Mağara çıkışından sonra başlar (x ~ 200)
        // Zemin tabanı + boşluklar + bouncer podlar

        // Ana zemin (x:200-205)
        PaintFloor(tilemap, sampleTile, 200, 205, -11);
        // Boşluk (x:206-208) — buraya bouncer koyacağız
        // Uçan platform (x:209-213, y:-8)
        PaintPlatform(tilemap, sampleTile, 209, 213, -9);
        // Boşluk (x:214-216)
        // Uçan platform (x:217-221, y:-6)
        PaintPlatform(tilemap, sampleTile, 217, 221, -7);
        // Ana zemin devam (x:222-240)
        PaintFloor(tilemap, sampleTile, 222, 240, -11);

        // ── BÖLGE B: Hareketli platform & meşale bölümü ──
        // Zemin (x:240-260)
        PaintFloor(tilemap, sampleTile, 241, 260, -11);
        // Üst duvar (koridor hissi için) (x:241-260, y:0)
        PaintPlatform(tilemap, sampleTile, 241, 260, 1);
        // Sol duvar
        PaintWall(tilemap, sampleTile, 241, -11, 1);
        // Sağ duvar
        PaintWall(tilemap, sampleTile, 260, -11, 1);
        // İçeride boşluk (x:242-259) → oyuncu geçecek

        // ── BÖLGE C: Arena + Final checkpoint ──
        PaintFloor(tilemap, sampleTile, 261, 295, -11);

        tilemap.RefreshAllTiles();
        EditorUtility.SetDirty(tilemap);
        Debug.Log("Yeni bölgeler başarıyla eklendi!");

        // Bouncer + hareketli platform + meşale objelerini de kur
        SetupMechanicObjects();

        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
    }

    static void PaintFloor(Tilemap tm, TileBase tile, int xStart, int xEnd, int y)
    {
        for (int x = xStart; x <= xEnd; x++)
        {
            tm.SetTile(new Vector3Int(x, y, 0), tile);
            tm.SetTile(new Vector3Int(x, y - 1, 0), tile); // Zemin kalınlığı
        }
    }

    static void PaintPlatform(Tilemap tm, TileBase tile, int xStart, int xEnd, int y)
    {
        for (int x = xStart; x <= xEnd; x++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    static void PaintWall(Tilemap tm, TileBase tile, int x, int yBottom, int yTop)
    {
        for (int y = yBottom; y <= yTop; y++)
            tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    static void SetupMechanicObjects()
    {
        // ── Bouncer 1 (boşluğun içinde) ──
        SetupBouncer("Bouncer_1", new Vector3(207f, -10f, 0f), 18f);
        SetupBouncer("Bouncer_2", new Vector3(215f, -10f, 0f), 18f);

        // ── Hareketli Platform ──
        SetupMovingPlatform("MovingPlatform_1", new Vector3(250f, -6f, 0f), new Vector3(250f, -2f, 0f), 1.5f);

        // ── Meşale (büyü ile aktive) → hareketli platformu aktive eder ──
        SetupTorch("MagicTorch_1", new Vector3(244f, -9f, 0f), "MovingPlatform_1");

        // ── Basınç plakası → kapı (arena kapısı) ──
        SetupPressurePlate("PressurePlate_1", new Vector3(263f, -10f, 0f), "ArenaGate_1");
        SetupGate("ArenaGate_1", new Vector3(270f, -8.5f, 0f));

        // ── Arena düşmanları ──
        SetupArenaEnemy("ArenaMinotaur_A", new Vector3(275f, -10f, 0f));
        SetupArenaEnemy("ArenaMinotaur_B", new Vector3(283f, -10f, 0f));

        // ── Son Checkpoint ──
        SetupCheckpoint("Checkpoint_Final", new Vector3(290f, -10f, 0f));

        Debug.Log("Tüm mekanik objeler yerleştirildi!");
    }

    static void SetupBouncer(string name, Vector3 pos, float force)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 0.4f, 1f);
        go.layer = 6; // Ground

        // Sprite rengi (zıplama yastığı - turuncu/sarı)
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(1f, 0.4f, 0.1f);
        UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
        UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>());

        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.4f);

        System.Type bt = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            bt = asm.GetType("Bouncer");
            if (bt != null) break;
        }
        if (bt != null)
        {
            var comp = go.AddComponent(bt);
            bt.GetField("bounceForce").SetValue(comp, force);
        }
    }

    static void SetupMovingPlatform(string name, Vector3 posA, Vector3 posB, float speed)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = posA;
        go.transform.localScale = new Vector3(4f, 0.5f, 1f);
        go.layer = 6;

        UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshRenderer>());
        UnityEngine.Object.DestroyImmediate(go.GetComponent<MeshFilter>());
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.3f, 0.6f, 1f);

        var col = go.GetComponent<BoxCollider2D>();
        if (col == null) col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(4f, 0.5f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        var ptA = new GameObject(name + "_PointA");
        ptA.transform.position = posA;
        ptA.transform.SetParent(go.transform);

        var ptB = new GameObject(name + "_PointB");
        ptB.transform.position = posB;
        ptB.transform.SetParent(go.transform);

        System.Type mt = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            mt = asm.GetType("MovingPlatform");
            if (mt != null) break;
        }
        if (mt != null)
        {
            var comp = go.AddComponent(mt);
            mt.GetField("pointA").SetValue(comp, ptA.transform);
            mt.GetField("pointB").SetValue(comp, ptB.transform);
            mt.GetField("speed").SetValue(comp, speed);
            mt.GetField("startsMoving").SetValue(comp, false); // Başta duruyor
        }
    }

    static void SetupTorch(string name, Vector3 pos, string activates)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var go = new GameObject(name);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.5f, 0.3f, 0.1f); // Sönük meşale rengi

        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;
        col.isTrigger = true;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
        go.layer = LayerMask.NameToLayer("Enemy");

        System.Type tt = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            tt = asm.GetType("MagicTorch");
            if (tt != null) break;
        }
        if (tt != null)
        {
            var comp = go.AddComponent(tt);
            var platform = GameObject.Find(activates);
            if (platform != null)
            {
                var objs = new GameObject[] { platform };
                tt.GetField("objectsToActivate").SetValue(comp, objs);

                // Platfor başta kapalı (inactive)
                platform.SetActive(false);
            }
            tt.GetField("hintLine1").SetValue(comp, "Meşale tutuştu!");
            tt.GetField("hintLine2").SetValue(comp, "Hareketli platform aktive oldu.");
            tt.GetField("torchRenderer").SetValue(comp, sr);
        }
    }

    static void SetupPressurePlate(string name, Vector3 pos, string gateTarget)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var go = new GameObject(name);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(2f, 0.3f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.6f, 0.5f, 0.2f);

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(2f, 0.3f);
        go.layer = 6;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        System.Type pt = null;
        foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            pt = asm.GetType("PressurePlate");
            if (pt != null) break;
        }
        if (pt != null)
        {
            var comp = go.AddComponent(pt);
            pt.GetField("plateRenderer").SetValue(comp, sr);

            var gate = GameObject.Find(gateTarget);
            if (gate != null)
            {
                var objs = new GameObject[] { gate };
                pt.GetField("objectsToToggle").SetValue(comp, objs);
                pt.GetField("stayOpen").SetValue(comp, false);
            }
        }
    }

    static void SetupGate(string name, Vector3 pos)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var go = new GameObject(name);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = new Color(0.4f, 0.1f, 0.05f); // Koyu kahve kapı
        sr.sortingOrder = 2;

        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(1f, 4f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;
    }

    static void SetupArenaEnemy(string name, Vector3 pos)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var template = GameObject.Find("Enemy_Minotaur (8)");
        if (template == null) {
            Debug.LogWarning("Kopyalanacak düşman bulunamadı.");
            return;
        }

        var clone = GameObject.Instantiate(template, pos, Quaternion.identity);
        clone.name = name;

        var health = clone.GetComponent<EnemyHealth>();
        if (health != null) health.ResetHealth();
        var ai = clone.GetComponent<EnemyAI>();
        if (ai != null) { ai.enabled = true; }
        clone.SetActive(true);
    }

    static void SetupCheckpoint(string name, Vector3 pos)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;

        var template = GameObject.Find("Checkpoint");
        if (template == null) {
            // Basit bir checkpoint objesi oluştur
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.8f, 0.2f);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1f, 2f);

            System.Type ct = null;
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                ct = asm.GetType("Checkpoint");
                if (ct != null) break;
            }
            if (ct != null) go.AddComponent(ct);
        } else {
            var clone = GameObject.Instantiate(template, pos, Quaternion.identity);
            clone.name = name;
        }
    }
}
