using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;

[System.Serializable]
public class ResultData
{
    public List<string> result;
}

public class ResultData2
{
    public float? result;
}

public class PythonConnectManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //테스트용 코드
        //StartCoroutine(MostSimilarty("삼겹살", 5));

        //StartCoroutine(MostSimilarty("장조성", 5));

        //StartCoroutine(SimilartyTwoWord("튤립", "꽃"));

        //StartCoroutine(SimilartyTwoWord("삼겹살", "장조성"));
    }

    //단어와 몇개의 유사한 단어를 가져올 것인지 입력
    IEnumerator MostSimilarty(string inputWord, int num)
    {
        string url = "http://34.64.202.6:5000/most_similarty";

        // 익명 객체를 JSON 문자열로 변환
        var data = new { word = inputWord, num = num };
        string jsonData = JsonConvert.SerializeObject(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                // Newtonsoft.Json으로 바로 파싱
                ResultData responseData = JsonConvert.DeserializeObject<ResultData>(jsonResponse);

                if (responseData.result != null && responseData.result.Count > 0)
                {
                    foreach (string word in responseData.result)
                    {
                        Debug.Log("결과: " + word);
                    }
                }
                else
                {
                    Debug.Log("부정확한 단어");
                }

            }
            else
            {
                Debug.LogError("요청 실패: " + request.error);
            }
        }
    }

    //두 단어 사이의 유사도
    IEnumerator SimilartyTwoWord(string inputWord, string inputWord2)
    {
        string url = "http://34.64.202.6:5000/similarity";

        // 익명 객체를 JSON 문자열로 변환
        var data = new { word1 = inputWord, word2 = inputWord2 };
        string jsonData = JsonConvert.SerializeObject(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                // Newtonsoft.Json으로 바로 파싱
                ResultData2 responseData = JsonConvert.DeserializeObject<ResultData2>(jsonResponse);

                if (responseData != null && responseData.result.HasValue)
                {
                    Debug.Log("유사도: " + responseData.result.Value);
                }
                else {
                    Debug.Log("부정확한 단어");
                }
      

            }
            else
            {
                Debug.LogError("요청 실패: " + request.error);
            }
        }
    }
}
