using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using WordEater.Systems;

public class AlgorithmPanel : MonoBehaviour
{
    Animator ani;
    public PhoneSwiper phoneSwiper;
    public bool Mode; // true : Easy, false : Hard
    public GameObject GameTab;
    public GameObject ResultTab;
    public bool IsOpen => GameTab.activeSelf || ResultTab.activeSelf;
    public Button[] Buttons;

    // 캐시
    private MiniGameController _mini;

    void Start()
    {
        ani = GetComponent<Animator>();
        GameTab.SetActive(false);
        ResultTab.SetActive(false);
        if (GameTab) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
    }

    private void Update()
    {
            for(int i = 0; i < Buttons.Length; i++)
            {
                Buttons[i].interactable = !IsOpen;
            }
    }

    public void OpenEasyMode()
    {
        if (!_mini.CanStartMiniGame()) return;
        
        // [수정] 배터리 체크(결제) 먼저 시도 -> 성공 시에만 패널 오픈
        _mini.CheckPayment(() => 
        {
            Mode = true;
            StartCoroutine(OpenPageTab());
        });
    }

    public void OpenHardMode()
    {
        if (!_mini.CanStartMiniGame()) return; // Hard mode doesn't allow start if cannot start? 
        // Wait, CanStartMiniGame checks isDead. 
        // Logic same as EasyMode
        
        _mini.CheckPayment(() => 
        {
            Mode = false;
            StartCoroutine(OpenPageTab());
        });
    }

    public void CloseMode()
    {
        // [수정] 수동으로 닫기(포기)를 누르면 결과창 없이 닫음
        StartCoroutine(CloasePageTab(showResult: false));
    }

    public IEnumerator OpenPageTab()
    {
        phoneSwiper.isUsingTab = true;
        ani.SetTrigger("Open");
        yield return new WaitForSecondsRealtime(0.5f);

        GameTab.SetActive(true);
        // 미니게임 시작
        if (_mini == null) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
        _mini?.StartGame(); // Begin() 대신 StartGame() 사용
    }

    public IEnumerator CloasePageTab(bool showResult = true)
    {
        // 미니게임 정리
        if (_mini == null) _mini = GameTab.GetComponentInChildren<MiniGameController>(true);
        _mini?.StopAllGames();

        GameTab.SetActive(false);
        // 메인 패널 닫기 애니메이션 실행
        ani.SetTrigger("Close");
        yield return new WaitForSecondsRealtime(0.5f);

        // 결과 패널 준비 (기존 ResultTab 대신 UIManager 팝업 사용)
        
        // [수정] 플레이어가 사망 상태(배터리 방전 등)라면 결과 팝업을 띄우지 않고 즉시 종결
        if (_mini != null && _mini.wordeater != null && _mini.wordeater.isDead)
        {
            CloseResultTab();
            yield break;
        }

        // [수정] 결과 표시 여부 체크
        if (!showResult)
        {
            CloseResultTab();
            yield break;
        }

        // 1. 보상 확인
        ItemType rewardItems = (ItemType)(-1);
        if (_mini != null)
        {
            rewardItems = _mini.CheckItemReward();
        }

        // 2. 메시지 구성
        string modeStr = (Mode ? "이지" : "하드") + " 모드";
        int clearCnt = (_mini != null) ? _mini.ClearCount : 0;
        string scoreStr = $"{clearCnt} 개의 미니게임 클리어!";
        string itemStr = "";

        if ((int)rewardItems >= 0) // 유효한 아이템
        {
            string kName = ItemUtils.GetItemNameKR(rewardItems);
            itemStr = $"\n아이템 획득!\n<color=#FF00FF>[{kName}]</color>";
        }
        else
        {
            itemStr = "\n(아이템 획득 실패)";
        }

        string finalMsg = $"{modeStr}\n{scoreStr}{itemStr}";

        // 3. UI 매니저 호출 (ResultTab 흐름 대체)
        if (UIManager.Instance != null)
        {
            // CloseResultTab을 콜백으로 넘겨서 팝업 닫히면 정리되게 함
            UIManager.Instance.Show(finalMsg, () => 
            {
                CloseResultTab();
            });
        }
        else
        {
            // fallback
            CloseResultTab();
        }
    }


    public void CloseResultTab()
    {
        phoneSwiper.isUsingTab = false;

        if (_mini != null) _mini.ClearCount = 0;
        
        // 필요하다면 다시 복귀시키거나, 굳이 안해도 됨 (다음에 열릴 때 로직 확인 필요)
        // 일단 Close 상태로 종료.
    }

}
