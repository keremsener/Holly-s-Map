using UnityEngine;
using UnityEditor;

public static class RebuildSection3
{
    [MenuItem("Tools/Rebuild Section 3 Objects")]
    public static void Rebuild()
    {
        float gY = -9.13f;

        System.Type GetT(string name) {
            foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies()) {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }

        var bouncerT = GetT("Bouncer");
        var mpT      = GetT("MovingPlatform");
        var torchT   = GetT("MagicTorch");
        var plateT   = GetT("PressurePlate");
        var hintT    = GetT("HintZoneTrigger");

        // ── 1. Bouncer A ──
        MakeObj("Bouncer_1", new Vector3(207f, gY + 0.25f, 0f), go => {
            go.layer = 6;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 0.5f); col.isTrigger = true;
            MakeSR(go, new Color(1f, 0.4f, 0.1f), 3);
            if (bouncerT != null) { var c = go.AddComponent(bouncerT); bouncerT.GetField("bounceForce").SetValue(c, 20f); }
        });

        // ── 2. Bouncer B ──
        MakeObj("Bouncer_2", new Vector3(215f, gY + 0.25f, 0f), go => {
            go.layer = 6;
            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(2f, 0.5f); col.isTrigger = true;
            MakeSR(go, new Color(1f, 0.4f, 0.1f), 3);
            if (bouncerT != null) { var c = go.AddComponent(bouncerT); bouncerT.GetField("bounceForce").SetValue(c, 20f); }
        });

        // ── 3. MovingPlatform ──
        GameObject mp = null;
        MakeObj("MovingPlatform_1", new Vector3(250f, gY + 1.5f, 0f), go => {
            mp = go; go.layer = 6;
            MakeSR(go, new Color(0.2f, 0.6f, 1f), 2);
            var col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(4f, 0.5f);
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        });
        if (mp != null && mpT != null) {
            var ptA = new GameObject("PointA"); ptA.transform.SetParent(mp.transform);
            ptA.transform.position = new Vector3(250f, gY + 1.5f, 0f);
            var ptB = new GameObject("PointB"); ptB.transform.SetParent(mp.transform);
            ptB.transform.position = new Vector3(250f, gY + 5.5f, 0f);
            var comp = mp.AddComponent(mpT);
            mpT.GetField("pointA").SetValue(comp, ptA.transform);
            mpT.GetField("pointB").SetValue(comp, ptB.transform);
            mpT.GetField("speed").SetValue(comp, 2f);
            mpT.GetField("startsMoving").SetValue(comp, true);
            mpT.GetField("waitTime").SetValue(comp, 0.8f);
            mp.SetActive(false); // Meşale ile aktive olacak
        }

        // ── 4. Arena Kapısı ──
        GameObject gate = null;
        MakeObj("ArenaGate_1", new Vector3(270f, gY + 2.5f, 0f), go => {
            gate = go;
            MakeSR(go, new Color(0.4f, 0.1f, 0.05f), 4);
            var col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(1f, 5f);
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
        });

        // ── 5. Basınç Plakası ──
        MakeObj("PressurePlate_1", new Vector3(263f, gY + 0.15f, 0f), go => {
            go.layer = 6;
            var sr = MakeSR(go, new Color(0.6f, 0.5f, 0.2f), 3);
            var col = go.AddComponent<BoxCollider2D>(); col.size = new Vector2(2f, 0.3f);
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
            if (plateT != null && gate != null) {
                var comp = go.AddComponent(plateT);
                plateT.GetField("plateRenderer").SetValue(comp, sr);
                plateT.GetField("objectsToToggle").SetValue(comp, new GameObject[] { gate });
                plateT.GetField("stayOpen").SetValue(comp, false);
            }
        });

        // ── 6. MagicTorch ──
        MakeObj("MagicTorch_1", new Vector3(244f, gY + 1f, 0f), go => {
            go.layer = LayerMask.NameToLayer("Enemy");
            var sr = MakeSR(go, new Color(0.5f, 0.3f, 0.05f), 3);
            var col = go.AddComponent<CircleCollider2D>(); col.radius = 0.7f; col.isTrigger = true;
            var rb = go.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Static;
            if (torchT != null) {
                var comp = go.AddComponent(torchT);
                torchT.GetField("torchRenderer").SetValue(comp, sr);
                torchT.GetField("hintLine1").SetValue(comp, "Meşale tutuştu!");
                torchT.GetField("hintLine2").SetValue(comp, "Hareketli platform aktive oldu, yukarıya çık!");
                if (mp != null) {
                    torchT.GetField("objectsToActivate").SetValue(comp, new GameObject[] { mp });
                }
            }
        });

        // ── 7. Arena Düşmanları ──
        var template = GameObject.Find("Enemy_Minotaur (8)") 
                    ?? GameObject.Find("Enemy_Minotaur (7)")
                    ?? GameObject.Find("Enemy_Minotaur (6)");
        if (template != null) {
            SpawnEnemy("ArenaMinotaur_A", new Vector3(278f, gY + 0.5f, 0f), template);
            SpawnEnemy("ArenaMinotaur_B", new Vector3(285f, gY + 0.5f, 0f), template);
        }

        // ── 8. Hint Zonları ──
        if (hintT != null) {
            MakeHint("Section3HintZone", new Vector3(199f, gY + 2f, 0f), 
                     "Dikkat!", "Turuncu yastıklara atla — seni yükseklere fırlatacak!", hintT);
            MakeHint("TorchHintZone", new Vector3(242f, gY + 2f, 0f), 
                     "Karanlık bir geçit...", "Meşaleyi büyü ile aydınlat.", hintT);
            MakeHint("PlateTutorialZone", new Vector3(260f, gY + 2f, 0f), 
                     "Antik bir kapı...", "Yerdeki altın plakaya bas ve geçit açılacak.", hintT);
        }

        // ── 9. Son Checkpoint ──
        var checkpoints = Object.FindObjectsOfType<MonoBehaviour>();
        MonoBehaviour cpTemplate = null;
        foreach (var c in checkpoints) if (c.GetType().Name == "Checkpoint") { cpTemplate = c; break; }
        if (cpTemplate != null && GameObject.Find("Checkpoint_Final") == null) {
            var clone = Object.Instantiate(cpTemplate.gameObject, new Vector3(290f, gY + 0.5f, 0f), Quaternion.identity);
            clone.name = "Checkpoint_Final";
        }

        EditorUtility.SetDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects()[0]);
        UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
        Debug.Log("Section 3 tam olarak yeniden oluşturuldu!");
    }

    static void MakeObj(string name, Vector3 pos, System.Action<GameObject> setup)
    {
        var existing = GameObject.Find(name);
        if (existing != null) return;
        var go = new GameObject(name);
        go.transform.position = pos;
        setup(go);
    }

    static SpriteRenderer MakeSR(GameObject go, Color col, int order)
    {
        var sr = go.AddComponent<SpriteRenderer>();
        sr.color = col; sr.sortingOrder = order;
        return sr;
    }

    static void SpawnEnemy(string name, Vector3 pos, GameObject template)
    {
        if (GameObject.Find(name) != null) return;
        var clone = Object.Instantiate(template, pos, Quaternion.identity);
        clone.name = name; clone.SetActive(true);
        var h = clone.GetComponent<EnemyHealth>(); if (h != null) h.ResetHealth();
        var ai = clone.GetComponent<EnemyAI>(); if (ai != null) ai.enabled = true;
    }

    static void MakeHint(string name, Vector3 pos, string l1, string l2, System.Type hintT)
    {
        if (GameObject.Find(name) != null) return;
        var go = new GameObject(name);
        go.transform.position = pos;
        var col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true; col.size = new Vector2(4f, 10f);
        var comp = go.AddComponent(hintT);
        hintT.GetField("line1").SetValue(comp, l1);
        hintT.GetField("line2").SetValue(comp, l2);
        hintT.GetField("duration").SetValue(comp, 5f);
    }
}
