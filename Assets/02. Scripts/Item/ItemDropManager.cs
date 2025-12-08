using UnityEngine;
using System.Collections.Generic;
using WordEater.Systems; // InventoryManager, ItemType 네임스페이스 확인 필요

[System.Serializable]
public struct ItemDropRate
{
    public ItemType type;
    [Range(0, 100)] public int weight; // 가중치 (높을수록 잘 나옴)
}

public class ItemDropManager : MonoBehaviour
{
    public static ItemDropManager Instance;

    [Header("아이템 확률 테이블")]
    [SerializeField] private List<ItemDropRate> dropTable;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 가중치 기반 랜덤 아이템 획득
    /// </summary>
    /// <param name="showUI">획득 시 UI 팝업 띄울지 여부</param>
    public void ObtainRandomItem(bool showUI = true)
    {
        if (dropTable == null || dropTable.Count == 0) return;

        // 1. 가중치 총합 계산
        int totalWeight = 0;
        foreach (var item in dropTable) totalWeight += item.weight;

        // 2. 랜덤 값 뽑기
        int randomValue = Random.Range(0, totalWeight);

        // 3. 어떤 아이템인지 판별
        ItemType selectedType = dropTable[0].type;
        int currentWeight = 0;

        foreach (var item in dropTable)
        {
            currentWeight += item.weight;
            if (randomValue < currentWeight)
            {
                selectedType = item.type;
                break;
            }
        }

        // 4. 인벤토리에 지급
        ItemManager.Instance.AddItem(selectedType, 1);
        Debug.Log($"[Drop] 아이템 획득: {selectedType}");

        // 5. 획득 알림 UI (UIManager에 ShowToast 혹은 ShowGetItemPopup 함수가 있다고 가정)
        if (showUI)
        {
            // 예: UIManager.Instance.ShowRewardPopup(selectedType);
            // 간단하게는:
            UIManager.Instance.Show($"아이템 획득!\n[{selectedType}]");
        }
    }
}