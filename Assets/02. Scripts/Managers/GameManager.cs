using UnityEngine;
using System.Collections;
using DG.Tweening;
public class GameManager : MonoBehaviour
{
    [SerializeField] private WordEater.Core.WordEater wordeater;
    [SerializeField] private GameObject touchblockPanel;

    //인자가 두개씩 필요한 애들

    //Call Panel
    [SerializeField] private RectTransform CallPanel;
    [SerializeField] private RectTransform CallBtn;

    //Message Panel
    [SerializeField] private RectTransform MessagePanel;
    [SerializeField] private RectTransform MessageBtn;

    //Gallery Panel
    [SerializeField] private RectTransform GalleryPanel;
    [SerializeField] private RectTransform GalleryBtn;

    void Start()
    {
        //시작 하면 첫 정답 단어 선정
        wordeater.BeginStage(wordeater.ReturnStage(), initial: true);
    }

    //type 에 따라 게임이 끝났을 때 행동 변화.
    //type이=1 인 경우 <<< 게임 오버.
    //type이=2 인 경우 <<< 게임 클리어
    public void EndingController(int type) {
        //게임 오버
        if (type == 1) {
            // 재시작 하는 동안(애니메이션이 나올 예정이라) 일단 터지 방지
            touchblockPanel.SetActive(true);
            NoticeManager.Instance.ShowTimed("게임 오버!", 3f);
            //재시작
            StartCoroutine(RestartWithDelay(3f));
        }
        //게임 클리어
        else if (type == 2) {
            // 재시작 하는 동안(애니메이션이 나올 예정이라) 일단 터지 방지
            touchblockPanel.SetActive(true);
            NoticeManager.Instance.ShowTimed("게임 클리어!", 3f);
            //도감 등록



            //재시작
            StartCoroutine(RestartWithDelay(3f));
        }
    
    }

    //일단은 N초뒤 시작이지만, 나중에 애니메이션을 넣으면 애니메이션 쪽에서 restart함수 실행으로 변경
    private IEnumerator RestartWithDelay(float delay)
    {


        yield return new WaitForSeconds(delay);
        Restart();
    }

    private void Restart() {
        touchblockPanel.SetActive(false);
        wordeater.BeginStage(wordeater.ReturnStage(), initial: true);
    }





    //화면들 Dotween 으로 띄우기
    public void ShowPanel(RectTransform panel)
    {
        panel.gameObject.SetActive(true);
        panel.localScale = Vector3.zero;

        // DOTween Scale 애니메이션
        panel.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
    }

    public void HidePanel(RectTransform panel)
    {
        // 작아지면서 사라지게
        panel.DOScale(Vector3.zero, 0.2f)
             .SetEase(Ease.InBack)
             .OnComplete(() => panel.gameObject.SetActive(false));
    }


    //인자가 두개씩 있는 애들
    public void ShowPanel_Call()
    {
        CallPanel.gameObject.SetActive(true);

        // 1. 처음 위치: 전화기 아이콘 위치
        CallPanel.position = CallBtn.position;
        CallPanel.localScale = Vector3.zero;

        // 2. 타겟 위치: 화면 중앙
        Vector3 targetPos = new Vector3(Screen.width / 2, Screen.height / 2, 0); // 캔버스 기준 조정 필요
        Vector3 targetScale = Vector3.one;

        // 3. DOTween 애니메이션
        CallPanel.DOMove(targetPos, 0.3f).SetEase(Ease.OutBack);
        CallPanel.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);
    }

    public void HidePanel_Call()
    {
        Vector3 targetPos = CallBtn.position;

        // DOTween 애니메이션: 위치 + 스케일 동시에
        CallPanel.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        CallPanel.DOMove(targetPos, 0.2f).SetEase(Ease.InBack)
             .OnComplete(() => CallPanel.gameObject.SetActive(false));
    }


    public void ShowPanel_Message()
    {
        MessagePanel.gameObject.SetActive(true);

        MessagePanel.position = MessageBtn.position;
        MessagePanel.localScale = Vector3.zero;

        Vector3 targetPos = new Vector3(Screen.width / 2, Screen.height / 2, 0); // 캔버스 기준 조정 필요
        Vector3 targetScale = Vector3.one;

        MessagePanel.DOMove(targetPos, 0.3f).SetEase(Ease.OutBack);
        MessagePanel.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);
    }

    public void HidePanel_Message()
    {
        Vector3 targetPos = MessageBtn.position;

        // DOTween 애니메이션: 위치 + 스케일 동시에
        MessagePanel.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        MessagePanel.DOMove(targetPos, 0.2f).SetEase(Ease.InBack)
             .OnComplete(() => MessagePanel.gameObject.SetActive(false));
    }


    public void ShowPanel_Gallery()
    {
        GalleryPanel.gameObject.SetActive(true);


        GalleryPanel.position = GalleryBtn.position;
        GalleryPanel.localScale = Vector3.zero;

        Vector3 targetPos = new Vector3(Screen.width / 2, Screen.height / 2, 0); // 캔버스 기준 조정 필요
        Vector3 targetScale = Vector3.one;

        GalleryPanel.DOMove(targetPos, 0.3f).SetEase(Ease.OutBack);
        GalleryPanel.DOScale(targetScale, 0.3f).SetEase(Ease.OutBack);
    }

    public void HidePanel_Gallery()
    {
        Vector3 targetPos = GalleryBtn.position;

        // DOTween 애니메이션: 위치 + 스케일 동시에
        GalleryPanel.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        GalleryPanel.DOMove(targetPos, 0.2f).SetEase(Ease.InBack)
             .OnComplete(() => GalleryPanel.gameObject.SetActive(false));
    }
}
