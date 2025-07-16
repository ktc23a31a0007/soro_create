using UnityEngine;

public class SavePoint : MonoBehaviour
{
    [Header("セーブポイント設定")]
    public string requiredItemName = "キャンプキット"; // セーブに必要なアイテム名
    public int requiredItemCount = 1; // 必要なアイテム数

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (Input.GetKeyDown(KeyCode.F)) // Fキーでインタラクト
            {
                AttemptSave();
            }
        }
    }

    void AttemptSave()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerが見つかりません！");
            return;
        }
        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManagerが見つかりません！");
            return;
        }

        if (GameManager.Instance.isInCombat)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("戦闘中はセーブできません！", 2f);
            Debug.LogWarning("戦闘中はセーブできません。");
            return;
        }
        if (GameManager.Instance.isGamePaused)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ポーズ中はセーブできません！", 2f);
            Debug.LogWarning("ポーズ中はセーブできません。");
            return;
        }

        if (InventoryManager.Instance.HasItem(requiredItemName, requiredItemCount))
        {
            InventoryManager.Instance.RemoveItem(requiredItemName, requiredItemCount);
            GameManager.Instance.SaveGame(); // GameManagerのSaveGameを呼び出す (上書きのみ)
            Debug.Log("ゲームをセーブしました！");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage("ゲームをセーブしました！", 2f);
        }
        else
        {
            Debug.Log($"セーブするには「{requiredItemName}」が{requiredItemCount}個必要です。");
            if (UIManager.Instance != null) UIManager.Instance.ShowMessage($"「{requiredItemName}」が足りません！", 2f);
        }
    }
}