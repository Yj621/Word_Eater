using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class History : MonoBehaviour
{
    public GameManager gamemanager;
    [SerializeField] private WordEater.Core.WordEater wordEater;

    public RectTransform content;
    Vector2 pos = new Vector2(0, 323);
    public ScrollRect scrollrect;
    public GameObject HistoryViewPrafab;
    public GameObject EmptyWord;

    public float heightNow;
    public TMP_Text StateMsg;
    public Button BackBtn;
    public Button NextBtn;

    private readonly List<LockPassword> spawned = new ();
    [SerializeField] private Transform slotsParent;     // 슬롯들이 생성될 부모 Transform
    [SerializeField] private LockPassword slotPrefab;   // 생성할 슬롯 프리팹

    public int page; // 0 -> 유사도 , 1 -> 관련 단어  , 2 -> 초성

    // 0 -> 이전 페이지 , 1 -> 다음 페이지 , 2 -> 처음 킬 때 페이지 0으로 설정
    public void SetHistory(int type)
    {
        content.anchoredPosition = pos;
        RectTransform rts = scrollrect.GetComponent<RectTransform>();
        scrollrect.vertical = false;
        heightNow = 0;
        slotsParent.gameObject.SetActive(false);

        // 스크롤 뷰 초기화
        foreach (Transform child in content.transform)
        {
            Destroy(child.gameObject);

        }

        if (type == 2)
        {
            page = 0;
        }
        else if (type == 0)
        { //이전 페이지
            page--;
        }
        else if (type == 1)
        {
            //다음 페이지 
            page++;
        }

        if (page == 0)
        {
            StateMsg.text = "유사도";

            BackBtn.interactable = false;
            NextBtn.interactable = true;

            // 히스토리 라인 찾아오기
            if (gamemanager.HistoryLIne == "")
            {
                EmptyWord.SetActive(true);
            }

            else
            {
                EmptyWord.SetActive(false);

                //문자열을 가져와서 | 단위로 나누고
                string[] items = gamemanager.HistoryLIne.Split('|');
                float sum = 0;
                foreach (string item in items)
                {

                    if (string.IsNullOrWhiteSpace(item)) continue;

                    // , 단위로 나눠서 각각 저장
                    string[] parts = item.Split(',');

                    string word = parts[0];
                    string sim = parts[1];

                    //프리랩을 만들고
                    GameObject obj = Instantiate(HistoryViewPrafab, content);
                    RectTransform rt = obj.GetComponent<RectTransform>();

                    //문자열 적용
                    TMP_Text wordText = obj.transform.Find("word").GetComponent<TMP_Text>();
                    TMP_Text simText = obj.transform.Find("similarity").GetComponent<TMP_Text>();
                    wordText.text = word;
                    simText.text = sim;

                    heightNow += rt.rect.height;


                    // 프리팹 위치 조정
                    Vector2 pos = rt.anchoredPosition;
                    pos.y = sum * -100f;
                    rt.anchoredPosition = pos;

                    // 컨텐츠 뷰 길이 조정
                    Vector2 size = content.sizeDelta;
                    size.y = 100f + (100f * sum);
                    content.sizeDelta = size;

                    sum++;

                }
                if (heightNow >= rts.rect.height)
                {
                    scrollrect.vertical = true;
                }
            }
        }
        else if (page == 1)// 관련단어
        {
            StateMsg.text = "관련단어";

            BackBtn.interactable = true;
            NextBtn.interactable = true;

            // 히스토리 라인 찾아오기
            if (gamemanager.RelevantLine == "")
            {
                EmptyWord.SetActive(true);
            }

            else
            {
                EmptyWord.SetActive(false);

                //문자열을 가져와서 | 단위로 나누고
                string[] items = gamemanager.RelevantLine.Split('|');
                float sum = 0;
                foreach (string item in items)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;

                    //프리랩을 만들고
                    GameObject obj = Instantiate(HistoryViewPrafab, content);
                    RectTransform rt = obj.GetComponent<RectTransform>();

                    //문자열 적용
                    TMP_Text wordText = obj.transform.Find("word").GetComponent<TMP_Text>();
                    TMP_Text simText = obj.transform.Find("similarity").GetComponent<TMP_Text>();
                    wordText.text = item;
                    simText.text = "";

                    heightNow += rt.rect.height;

                    // 프리팹 위치 조정
                    Vector2 pos = rt.anchoredPosition;
                    pos.y = sum * -100f;
                    rt.anchoredPosition = pos;

                    // 컨텐츠 뷰 길이 조정
                    Vector2 size = content.sizeDelta;
                    size.y = 100f + (100f * sum);
                    content.sizeDelta = size;

                    sum++;
                }
                if (heightNow >= rts.rect.height)
                {
                    scrollrect.vertical = true;
                }
            }
        }
        else if (page == 2)
        {
            StateMsg.text = "초성";

            BackBtn.interactable = true;
            NextBtn.interactable = false;

            // 받은 초성 저장된거 가져오기

            // 아무런 힌트가 없는 경우
            if (!gamemanager.isLength && !gamemanager.isFirst && !gamemanager.isLast && !gamemanager.isChoseongItem) EmptyWord.SetActive(true);


            // 힌트가 하나라도 있단거니까 일단 글자수 띄우고 first나 last가 있으면 추가해서 공개하는 식으로
            else {
                string answer = (wordEater != null) ? wordEater.Answer : "";
                if (string.IsNullOrEmpty(answer)) answer = "?";

                ShowHint(answer);
            }
        }
    }

    public void ShowHint(string answerWord)
    {
        slotsParent.gameObject.SetActive(true);


        // 정답이 비어있으면 예외 처리로 '?' 할당함
        if (string.IsNullOrEmpty(answerWord))
            answerWord = "?";

        // 너무 길어지는 것 방지 (1~50글자 제한)
        int length = Mathf.Clamp(answerWord.Length, 1, 50);

        // 필요한 만큼 슬롯(LockPassword)을 확보함
        EnsureSlots(length);

        // 정답 길이만큼만 슬롯을 켜고, 나머지는 끔
        for (int i = 0; i < spawned.Count; i++)
        {
            bool visible = (i < length);
            spawned[i].gameObject.SetActive(visible);

            // 안 보이는 슬롯은 설정할 필요 없으니 건너뜀
            if (!visible) continue;

            // 일단 모든 슬롯을 '점(●)' 상태로 초기화함
            spawned[i].SetDot(active: true);
        }

        // 모드에 따라 첫 번째 혹은 마지막 슬롯에 초성을 박아줌
        if (gamemanager.isFirst)
        {
            char chosungChar = GetSingleChosung(answerWord, LockHintMode.FirstChosung);
            spawned[0].SetChar(chosungChar.ToString());
        }
        if (gamemanager.isLast)
        {
            char chosungChar = GetSingleChosung(answerWord, LockHintMode.LastChosung);
            spawned[length - 1].SetChar(chosungChar.ToString());
        }
        if (gamemanager.isChoseongItem) {
            string chosung = KoreanUtils.GetChosungString(answerWord);

            for (int i = 0; i < spawned.Count; i++) {
                spawned[i].SetChar(chosung[i].ToString());
            }
        }
    }

    private void EnsureSlots(int required)
    {
        while (spawned.Count < required)
        {
            var slot = Instantiate(slotPrefab, slotsParent);
            spawned.Add(slot);
        }
    }

    /// <summary>
    /// 단어에서 모드에 맞는 초성을 추출하는 로직
    /// </summary>
    private char GetSingleChosung(string word, LockHintMode mode)
    {
        if (string.IsNullOrEmpty(word)) return '?';

        // 첫 글자냐 끝 글자냐 타겟 문자 결정함
        char target = (mode == LockHintMode.FirstChosung) ? word[0] : word[word.Length - 1];

        // 타겟 문자가 한글 범위(0xAC00 ~ 0xD7A3) 안에 있는지 체크함
        if (target >= 0xAC00 && target <= 0xD7A3)
        {
            int uniVal = target - 0xAC00;
            int choIndex = uniVal / (21 * 28); // 초성 인덱스 계산 공식
            return KoreanUtils_OneChar.GetCho(choIndex);
        }

        // 한글 아니면(영어, 숫자 등) 그냥 그대로 반환
        return target;
    }
}
