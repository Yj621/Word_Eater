using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using WordEater.Systems;

public class ClickIconChoiceJamo : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private JamoChooserUI chooserPanel; // 선택창 오브젝트 (프리팹이 아닌 인스펙터에 배치된 씬 오브젝트)
    [SerializeField] private Transform targetPanel;         // 띄울 부모
    [SerializeField] private bool consumeAfterPick = true;
    [SerializeField] private GameObject folderPanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject closePanel;

    // 핸들러 보관(중복 구독 방지용)
    private Action<JamoDefsType, string> _selectedHandler;
    private Action<bool> _requestCloseHandler;

    // [추가] 동적 생성을 위한 초기화 함수
    public void Initialize(JamoChooserUI chooser, Transform target, GameObject folder, GameObject confirm, GameObject close)
    {
        this.chooserPanel = chooser;
        this.targetPanel = target;
        this.folderPanel = folder;
        this.confirmPanel = confirm;
        this.closePanel = close;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (chooserPanel == null || targetPanel == null)
        {
            Debug.LogWarning("[ClickIconChoiceJamo] chooserPanel 또는 targetCanvas 미지정");
            return;
        }

        // 기존 UI 숨기기
        folderPanel.SetActive(false);

        // 프리팹 대신 인스펙터에 배치한 오브젝트를 사용한다.
        var chooser = chooserPanel;

        // 부모를 targetPanel로 설정. worldPositionStays=false로 로컬값 유지
        chooser.transform.SetParent(targetPanel, false);
        var rect = chooser.GetComponent<RectTransform>();
        rect.anchoredPosition = Vector2.zero; // 부모 패널 기준 (0,0)
        rect.localRotation = Quaternion.identity;

        // 활성화 전 초기 스케일 세팅
        chooser.gameObject.SetActive(true);
        chooser.consumeAfterPick = this.consumeAfterPick; // [추가] 소모 여부 전달
        rect.localScale = Vector3.zero;
        rect.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

        // 기존 구독 해제(안전하게 중복 제거)
        chooser.OnSelected -= _selectedHandler;
        chooser.OnRequestClose -= _requestCloseHandler;
        chooser.OnCheckCanReceive = null;

        // [추가] 받을 수 있는지 체크하는 로직 연결
        chooser.OnCheckCanReceive = (jamo) =>
        {
            if (JamoInventory.Instance != null)
            {
                return JamoInventory.Instance.CanAdd(jamo);
            }
            return true;
        };

        // 선택 이벤트 처리(인스펙터 오브젝트 재사용 시 중복 구독 방지)
        _selectedHandler = (type, jamo) =>
        {
            JamoInventory.Instance.Add(type, jamo);
            // 선택 후 창을 닫을지 여부는 confirm에서 전달되는 OnRequestClose를 통해 제어
        };
        chooser.OnSelected += _selectedHandler;

        // confirm의 Yes/No 버튼에서 닫기 요청을 받을 핸들러
        _requestCloseHandler = (closeChooser) =>
        {
            if (closeChooser)
            {
                // 창 전체 닫기(애니메이션 포함)
                OnCloseChooser();
            }
            else
            {
                // 배경(예: closePanel)만 닫기
                closePanel.SetActive(false);
            }
        };
        chooser.OnRequestClose += _requestCloseHandler;

        closePanel.SetActive(true);
        // 팝업이 배경(Image)보다 위에 오도록 순서 보장
        chooser.transform.SetAsLastSibling();
    }

    public void OnCloseChooser()
    {
        chooserPanel.gameObject.SetActive(false);
        closePanel.SetActive(false);

        // [DOTween] 작아지는 애니메이션 적용 (Destroy 대신 비활성화)
        chooserPanel.transform.DOScale(Vector3.zero, 0.2f)
            .SetEase(Ease.InBack) // 쏙 들어가는 느낌
            .OnComplete(() =>
            {
                // 애니메이션이 다 끝난 뒤 오브젝트 비활성화
                chooserPanel.gameObject.SetActive(false);
                closePanel.SetActive(false); // 배경 패널 끄기
            });
    }
}
