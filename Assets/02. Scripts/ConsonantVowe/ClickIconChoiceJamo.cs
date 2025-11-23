using UnityEngine;
using UnityEngine.EventSystems;

public class ClickIconChoiceJamo : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private JamoChooserUI chooserPrefab; // 선택창 프리팹
    [SerializeField] private Transform targetPanel;         // 띄울 부모
    [SerializeField] private bool consumeAfterPick = true;
    [SerializeField] private GameObject folderPanel;
    [SerializeField] private GameObject sceneConfirmPanel;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (chooserPrefab == null || targetPanel == null)
        {
            Debug.LogWarning("[ClickIconChoiceJamo] chooserPrefab 또는 targetCanvas 미지정");
            return;
        }

        // 기존 UI 숨기기
        folderPanel.SetActive(false);
        var chooser = Instantiate(chooserPrefab, targetPanel);
        var rect = chooser.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero; // 부모 패널 기준 (0,0)
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;

        // 여기서  Confirm_Panel 전달
        chooser.InitConfirmPanel(sceneConfirmPanel);

        chooser.OnSelected += (type, jamo) =>
        {
            JamoInventory.Instance.Add(type, jamo);

            if (consumeAfterPick)
            {
                // Destroy(gameObject);
            }

        };
    }
}
