using System.Collections.Generic;
using UnityEngine;
using WordEater.Systems;

public class JamoTicketDisplay : MonoBehaviour
{
    [SerializeField] private GameObject ticketPrefab; // ClickIconChoiceJamo가 붙은 프리팹
    [SerializeField] private ItemType targetItem = ItemType.JamoSelectionTicket;

    [Header("Scene References for Spawned Button")]
    [SerializeField] private JamoChooserUI chooserPanel;
    [SerializeField] private Transform targetPanel;
    [SerializeField] private GameObject folderPanel;
    [SerializeField] private GameObject iconGroup; // [추가] 직접 할당 (이름 검색 지양)
    [SerializeField] private GameObject jamoConfirmPanel;
    [SerializeField] private GameObject closePanel;

    private List<GameObject> _spawnedTickets = new List<GameObject>();

    private void Start()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnInventoryChanged += RefreshUI;
            RefreshUI();
        }
    }

    private void OnDestroy()
    {
        if (ItemManager.Instance != null)
        {
            ItemManager.Instance.OnInventoryChanged -= RefreshUI;
        }
    }

    // 인벤토리 상태에 맞춰 버튼 개수 동기화
    public void RefreshUI()
    {
        if (ItemManager.Instance == null) return;

        int count = ItemManager.Instance.GetCount(targetItem);

        // 현재 스폰된 개수와 비교
        int currentSpawned = _spawnedTickets.Count;

        if (currentSpawned < count)
        {
            // 부족하면 추가
            int diff = count - currentSpawned;
            for (int i = 0; i < diff; i++)
            {
                SpawnTicket();
            }
        }
        else if (currentSpawned > count)
        {
            // 많으면 삭제
            int diff = currentSpawned - count;
            for (int i = 0; i < diff; i++)
            {
                RemoveTicket();
            }
        }
    }

    private void SpawnTicket()
    {
        if (ticketPrefab == null) return;
        
        GameObject go = Instantiate(ticketPrefab, transform);
        go.SetActive(true);
        
        var comp = go.GetComponent<ClickIconChoiceJamo>();
        if (comp != null)
        {
            // [수정] 직접 할당된 iconGroup 사용
            comp.Initialize(chooserPanel, targetPanel, folderPanel, jamoConfirmPanel, closePanel, iconGroup);
        }

        _spawnedTickets.Add(go);
    }

    private void RemoveTicket()
    {
        if (_spawnedTickets.Count == 0) return;

        // 리스트의 마지막(혹은 첫번째) 삭제
        int lastIdx = _spawnedTickets.Count - 1;
        GameObject go = _spawnedTickets[lastIdx];
        _spawnedTickets.RemoveAt(lastIdx);
        
        Destroy(go);
    }
}
