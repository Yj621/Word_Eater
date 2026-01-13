using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class History : MonoBehaviour
{
    public GameManager gamemanager;
    public RectTransform content;
    public GameObject HistoryViewPrafab;
    public GameObject EmptyWord;

    public TMP_Text StateMsg;
    public Button BackBtn;
    public Button NextBtn;

    public int page; // 0 -> 유사도 , 1 -> 관련 단어  , 2 -> 초성

    // 0 -> 이전 페이지 , 1 -> 다음 페이지 , 2 -> 처음 킬 때 페이지 0으로 설정
    public void SetHistory(int type)
    {

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

                foreach (string item in items)
                {

                    if (string.IsNullOrWhiteSpace(item)) continue;

                    // , 단위로 나눠서 각각 저장
                    string[] parts = item.Split(',');

                    string word = parts[0];
                    string sim = parts[1];

                    //프리랩을 만들고
                    GameObject obj = Instantiate(HistoryViewPrafab, content);

                    //문자열 적용
                    TMP_Text wordText = obj.transform.Find("word").GetComponent<TMP_Text>();
                    TMP_Text simText = obj.transform.Find("similarity").GetComponent<TMP_Text>();
                    wordText.text = word;
                    simText.text = sim;

                    /*
                    // 프리팹 위치 조정
                    RectTransform rt = obj.GetComponent<RectTransform>();
                    Vector2 pos = rt.anchoredPosition;
                    pos.y = sum * -100f;
                    rt.anchoredPosition = pos;

                    // 스크롤 뷰 길이 조정
                    Vector2 size = content.sizeDelta;
                    size.y = 100f + (100f * sum);
                    content.sizeDelta = size;

                    sum++;
                    */
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

                foreach (string item in items)
                {
                    if (string.IsNullOrWhiteSpace(item)) continue;

                    //프리랩을 만들고
                    GameObject obj = Instantiate(HistoryViewPrafab, content);

                    //문자열 적용
                    TMP_Text wordText = obj.transform.Find("word").GetComponent<TMP_Text>();
                    TMP_Text simText = obj.transform.Find("similarity").GetComponent<TMP_Text>();
                    wordText.text = item;
                    simText.text = "";
                }
            }
        }
        else if (page == 2)
        {
            StateMsg.text = "초성";

            BackBtn.interactable = true;
            NextBtn.interactable = false;

            // 받은 초성 저장된거 가져오기
            EmptyWord.SetActive(true);
        }
    }
}
