using UnityEngine;

public class CaveHintTrigger : MonoBehaviour
{
    public GameObject finalEnemy;
    private bool hintShown = false;
    
    private void Update()
    {
        if (!hintShown && finalEnemy != null && !finalEnemy.activeInHierarchy)
        {
            hintShown = true;
            if (HintDisplay.Instance != null)
            {
                HintDisplay.Instance.Show(
                    "Çıkmaz Yol...", 
                    "Sıradan silahlar bu duvarı aşamaz. Belki antik büyü enerjisi gizli geçidi açabilir.", 
                    Color.white, 20, 6f, 1f, 1.5f);
            }
        }
    }
}
