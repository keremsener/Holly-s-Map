using UnityEngine;
using System.Collections; // Bunu eklemeyi unutma!

public class PlayerCombat : MonoBehaviour
{
    private Animator anim;
    
    [Header("Özel Büyü (F Tuşu - Sınırlı)")]
    public Transform firePoint;
    public GameObject magicPrefab;
    public int maxMagicCharges = 3; // 3 Hak
    private int currentMagicCharges;
    public float manaRechargeTime = 5f; // 5 saniyede 1 dolacak
    private bool isRecharging = false;

    [Header("Ana Kılıç (Sol Tık - Sınırsız)")]
    public Transform meleePoint; // Kılıç vuruş merkezi
    public float meleeRange = 1.2f; // Vuruş menzili
    public int meleeDamage = 40; // Kılıç gücü
    public LayerMask enemyLayers; // Kime vuracağız? (Enemy katmanı)
    
    [Header("Savaş Efektleri (Jilet Hissiyat)")]
    public GameObject slashVFXPrefab; // Havada çıkacak kılıç izi
    public GameObject bloodBurstPrefab; // Düşmana çarpınca çıkacak kan patlaması

    void Start()
    {
        anim = GetComponent<Animator>();
        currentMagicCharges = maxMagicCharges; // Oyuna ful manayla başla
    }

    void Update()
    {
        // --- MANA YENİLENME SİSTEMİ ---
        if (currentMagicCharges < maxMagicCharges && !isRecharging)
        {
            StartCoroutine(RechargeMana());
        }

        // --- KILIÇ / YAKIN DÖVÜŞ (SOL TIK) ---
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            MeleeAttack();
        }

        // --- BÜYÜ FIRLATMA (F TUŞU) ---
        if (Input.GetKeyDown(KeyCode.F))
        {
            if(currentMagicCharges > 0)
            {
                anim.SetTrigger("Attack"); // Yine o ıkınma pozu (büyü hazırlığı gibi)
                ShootMagic();
                currentMagicCharges--; // Manayı azalt
            }
            else
            {
                Debug.Log("Mana Bitti! 5 saniye bekle...");
            }
        }
    }

    void MeleeAttack()
    {
        anim.SetTrigger("Attack"); // Karakter elini uzatsın (kabız pozu)

        // 1. Havada Kılıç İzini (Slash VFX) Patlat! (Hile Burası)
        Instantiate(slashVFXPrefab, meleePoint.position, meleePoint.rotation);

        // 2. Menzildeki düşmanları bul
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(meleePoint.position, meleeRange, enemyLayers);

        // 3. Her düşmana hasar ver ve kan fışkırt
        // MeleeAttack fonksiyonunun içindeki döngüyü şununla değiştir:
        foreach(Collider2D enemy in hitEnemies)
        {
            // Savrulma yönünü hesapla (Oyuncudan canavara doğru)
            Vector2 knockDirection = (enemy.transform.position - transform.position).normalized;
            
            // Canavara HASAR ver ve onu İT! (Hasar: 40, İtme Gücü: 10f)
            enemy.GetComponent<EnemyHealth>().TakeDamage(meleeDamage, knockDirection, 10f);
        
            // Kan Efekti (Demin yazdığımız rastgele konumlu kod buraya gelecek)
            float randomY = Random.Range(0.8f, 1.4f);
            Vector3 bloodPosition = enemy.transform.position + new Vector3(0, randomY, 0);
            Instantiate(bloodBurstPrefab, bloodPosition, Quaternion.identity);
        }
    }

    void ShootMagic()
    {
        Instantiate(magicPrefab, firePoint.position, firePoint.rotation);
    }

    // Mana Yenileme Sayacı
    IEnumerator RechargeMana()
    {
        isRecharging = true;
        yield return new WaitForSeconds(manaRechargeTime);
        currentMagicCharges++;
        Debug.Log("1 Mana Yenilendi! Mevcut Mana: " + currentMagicCharges);
        isRecharging = false;
    }

    // Unity'de kılıç menzilini görme hilesi
    void OnDrawGizmosSelected()
    {
        if (meleePoint == null) return;
        Gizmos.DrawWireSphere(meleePoint.position, meleeRange);
    }
}