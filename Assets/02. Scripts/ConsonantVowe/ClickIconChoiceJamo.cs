using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class ClickIconChoiceJamo : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private JamoChooserUI chooserPrefab; // 선택창 프리팹
    [SerializeField] private Transform targetPanel;         // 띄울 부모
    [SerializeField] private bool consumeAfterPick = true;
    [SerializeField] private GameObject folderPanel;
    [SerializeField] private GameObject sceneConfirmPanel;
    [SerializeField] private GameObject closePanel;

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

        rect.localScale = Vector3.zero;
        rect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);


        // 여기서  Confirm_Panel 전달
        chooser.InitConfirmPanel(sceneConfirmPanel);

        chooser.OnSelected += (type, jamo) =>
        {
            JamoInventory.Instance.Add(type, jamo);
            if (consumeAfterPick)
            {
                OnCloseChooser();
            }
        };
        closePanel.SetActive(true);
    }

    public void OnCloseChooser()
    {
        // 1. 현재 떠 있는 창(Chooser)을 찾습니다.
        var chooser = targetPanel.GetComponentInChildren<JamoChooserUI>();

        // 만약 창이 없다면 패널만 끄고 리턴
        if (chooser == null)
        {
            closePanel.SetActive(false);
            return;
        }

        // 2. [DOTween] 작아지는 애니메이션 적용
        chooser.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack) // 쏙 들어가는 느낌
            .OnComplete(() =>
            {
                // 3. 애니메이션이 다 끝난 뒤 실행할 코드
                Destroy(chooser.gameObject); // 오브젝트 파괴
                closePanel.SetActive(false); // 배경 패널 끄기
            });
    }
}
