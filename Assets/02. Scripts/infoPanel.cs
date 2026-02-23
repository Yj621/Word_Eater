using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class infoPanel : MonoBehaviour
{
    public Image BGPanel;
    public Image EXPPanel;

    public RectTransform DebuGImage;
    public RectTransform target;


    public PhoneSwiper phoneSwiper;


    public Image ExImg;
    public TextMeshProUGUI ExText;


    public void showExplainPanel() {
        // 우선 슬라이드를 막아
        phoneSwiper.isUsingTab = true;
        GameManager.Instance.temp = this.gameObject;

        Vector3 worldPos = transform.position;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            DebuGImage.parent as RectTransform,
            Camera.main.WorldToScreenPoint(worldPos),
            Camera.main,
            out Vector2 uiPos
        );

        GameManager.Instance.InfoDebug = uiPos;


        BGPanel.gameObject.SetActive(true);

        //아이콘을 시작 위치로 이동
        DebuGImage.anchoredPosition = uiPos;
        this.gameObject.SetActive(false);


        DebuGImage.gameObject.SetActive(true);

        BGPanel.DOFade(0.9f, 0.5f)
    .OnComplete(() =>
    {
        // 마무리 보정
        Color c = BGPanel.color;
        c.a = 0.9f;
        BGPanel.color = c;


        RectTransformUtility.ScreenPointToLocalPointInRectangle(
    DebuGImage.parent as RectTransform,
    Camera.main.WorldToScreenPoint(target.position),
    Camera.main,
    out Vector2 targetUIPos
);

        // DOTween으로 아이콘 이동
        DebuGImage.DOAnchorPos(targetUIPos, 0.5f)
            .SetEase(Ease.OutCubic).OnComplete(() =>
            {
                //마무리 보정
                DebuGImage.anchoredPosition = targetUIPos;

                var parent = EXPPanel.rectTransform.parent as RectTransform;
                // 버튼(Canvas A)의 위치를 패널 부모(Canvas B)의 로컬좌표로 변환
                Vector2 startLocal = CanvasUtil.ConvertBetweenCanvases(target, parent);

                // 시작 상태
                EXPPanel.rectTransform.anchoredPosition = startLocal;
                EXPPanel.rectTransform.localScale = Vector3.zero;
                EXPPanel.gameObject.SetActive(true);

                // 목표: 부모 중앙(앵커/피벗이 Center라면 Vector2.zero)
                Vector2 targetLocal = Vector2.zero;

                // 애니메이션
                EXPPanel.rectTransform.DOAnchorPos(targetLocal, 0.4f).SetEase(Ease.OutBack);
                EXPPanel.rectTransform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            });
    });

    }

    public void OffExplainPanel()
    {
        var parent = EXPPanel.rectTransform.parent as RectTransform;
        Vector2 endLocal = CanvasUtil.ConvertBetweenCanvases(target, parent);
        EXPPanel.rectTransform.DOScale(Vector3.zero, 0.4f)
                     .SetEase(Ease.InBack)
                     .SetUpdate(true);

        //설명 패널 숨기고
        EXPPanel.rectTransform.DOAnchorPos(endLocal, 0.4f)
             .SetEase(Ease.InBack)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 EXPPanel.gameObject.SetActive(false);


                 //아이콘 원래 위치로 이동 시키고
                 DebuGImage.DOAnchorPos(GameManager.Instance.InfoDebug, 0.5f).OnComplete(() =>
                 {
                     GameManager.Instance.temp.SetActive(true);
                     DebuGImage.gameObject.SetActive(false);


                     //뒷 배경 지우기
                     BGPanel.DOFade(0f, 0.5f)
                        .OnComplete(() =>
                    {
                        // 마무리 보정
                        Color c = BGPanel.color;
                        c.a = 0.0f;
                        BGPanel.color = c;

                        BGPanel.gameObject.SetActive(false);

                        // 다 끝나면 다시 슬라이드 켜기
                        phoneSwiper.isUsingTab = false;


                    });
                 });
             });
    }



    // 기능들 별로 설명문 & 이미지 들어가는게 다를텐데  아래 함수를
    // 기능들 만큼 만들어서 따로 지정해 주는게 편할듯? 게임 메니저의 아이콘 패널 띄우는 것 처럼ㅇㅇ
    // 아이콘의 button에 이 함수 넣으면 작동 할 거임
    public void SetContent(int type) {
        //ExImg.sprite = selfimge; // << 여긴 고민을 좀 해봐야 할 듯?

        switch (type) {

            case 0: // 전화
                ExText.text = "응답 버튼을 눌러 베터리를 소모하여 관련 단어의 힌트를 받을 수 있습니다.\n최대 7개의 단어를 받을 수 있고 남은 단어는 중앙에 표시됩니다.";
                break;

            case 1:
                ExText.text = "메세지 기능을 사용하면 베터리를 소모하여 최대 10회 만큼 단어를 입력해 유사도를 확인할 수 있습니다.";
                break; // 메세지

            case 2: // 자물쇠
                ExText.text = "자물쇠 기능을 사용하면 베터리를 소모하여 글자 수, 첫 번째 글자 초성, 마지막 글자 초성 중에 하나를 확인할 수 있습니다.\n단, 마지막 글자 초성은 정답 단어가 4 글자 이상일 때만 등장하며 모든 종류의 힌트를 확인하면 다시 접속 할 수 없습니다.";
                break;

            case 3: // 제출
                ExText.text = "자음과 모음을 꾹 눌러 보드에 드래그 한 뒤, 자음과 모음을 합쳐서 단어를 완성할 수 있습니다.\n제출 버튼을 누르면 입력한 단어와 정답 단어의 유사도를 알려주며, 단어를 맞추면 워드이터가 다음 단계로 진화합니다.";
                break;

            default:
                ExText.text = "설명문";
                break;
        }
    }
}
