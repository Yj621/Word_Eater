using UnityEngine;
using System.Collections;

public class TutorialUIManager : MonoBehaviour
{
    public static TutorialUIManager Instance;

    [Header("UI 연결")]
    public GameObject panel1;
    public GameObject panel2;
    public GameObject text1;
    public GameObject text2;
    public PhoneSwiper phoneSwiper;

    [Header("테스트용 설정")]
    public bool alwaysShowTutorial = false; // true로 체크하면 무조건 튜토리얼이 실행됨

    public bool isTutorialRunning = false; // 튜토리얼이 진행 중인지 여부 (바깥에서 키보드 닫기 등을 막을 때 사용)

    private bool isKeyboardOpened = false;
    private bool isWordSuccess = false;

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        // 강제 실행이 켜져있다면 시작하자마자 튜토리얼 호출
        if (alwaysShowTutorial)
        {
            StartTutorialFlow();
        }
    }

    public void StartTutorialFlow()
    {
        if (!alwaysShowTutorial)
        {
            // 1번 완료했으면 튜토리얼 스킵
            if (PlayerPrefs.GetInt("FirstStartTutorialDone", 0) == 1) return;
        }

        StartCoroutine(TutorialSequence());
    }

    private IEnumerator TutorialSequence()
    {
        isTutorialRunning = true;

        // 1. 초기 셋업
        if (phoneSwiper) phoneSwiper.isUsingTab = true; // 스와이프 잠금
        
        if (panel1 != null) panel1.SetActive(true);
        if (panel2 != null) panel2.SetActive(false);
        if (text1 != null) text1.SetActive(false);
        if (text2 != null) text2.SetActive(false);

        // 2. 키보드 오픈 대기
        yield return new WaitUntil(() => isKeyboardOpened);
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(true);
        if (text1 != null) text1.SetActive(true);

        // 3. 단어(음절) 합성 성공 대기
        yield return new WaitUntil(() => isWordSuccess);
        if (text1 != null) text1.SetActive(false);
        if (text2 != null) text2.SetActive(true);

        // 4. 2초 대기
        yield return new WaitForSeconds(2f);
        if (panel2 != null) panel2.SetActive(false);

        // 5. 종료
        if (phoneSwiper) phoneSwiper.isUsingTab = false;
        
        // 튜토리얼 완료 처리
        PlayerPrefs.SetInt("FirstStartTutorialDone", 1);
        PlayerPrefs.Save();
        
        isTutorialRunning = false;
        // Debug.Log("튜토리얼 완료됨");
    }

    // 다른 스크립트에서 이벤트 알림
    public void NotifyKeyboardOpened() 
    { 
        isKeyboardOpened = true; 
    }

    public void NotifyWordSuccess() 
    { 
        isWordSuccess = true; 
    }
}
