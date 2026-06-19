using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System;
using Cysharp.Threading.Tasks;
using System.IO;

[System.Serializable]
public class ResultData
{
    public List<string> result;
}

public class ResultData2
{
    public float? result;
}

[Serializable]
public class ServerConfig
{
    public string baseUrl;
}

public class PythonConnectManager : MonoBehaviour
{

    private ServerConfig config;
    string configPath;  

    private void Awake()
    {
        configPath =
            Path.Combine(Application.streamingAssetsPath, "ServerConfig.json");

        config = JsonConvert.DeserializeObject<ServerConfig>(
            File.ReadAllText(configPath));
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //테스트용 코드

        /*
        StartCoroutine(MostSimilarty("삼겹살", 5, (result) =>
        {
            foreach (var word in result)
            {
                // Debug.Log("결과: " + word);
            }
        }));
        */

        /*
        StartCoroutine(SimilartyTwoWord("사과", "바나나", (result) =>
        {
            if (result.HasValue)
            {
                // Debug.Log("콜백으로 받은 유사도: " + result.Value);
            }
            else
            {
                // Debug.Log("콜백: 부정확한 단어 또는 요청 실패");
            }
        }));
        */

        //StartCoroutine(SimilartyTwoWord("삼겹살", "장조성"));
    }

    //단어와 몇개의 유사한 단어를 가져올 것인지 입력
    public async UniTask<List<string>> MostSimilarty(string inputWord, int num)
    {

        ServerConfig config =
        JsonConvert.DeserializeObject<ServerConfig>(
        File.ReadAllText(configPath));

        string url = $"{config.baseUrl}/most_similarty";

        var data = new { word = inputWord, num = num };
        string jsonData = JsonConvert.SerializeObject(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest(); // 완료될 때까지 대기

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                // Debug.Log("서버 응답: " + jsonResponse);
                ResultData responseData = JsonConvert.DeserializeObject<ResultData>(jsonResponse);

                if (responseData.result != null && responseData.result.Count > 0)
                {
                    return responseData.result; // 결과 리스트 반환
                }
                else
                {
                    return new List<string> { "부정확한 단어" };
                }
            }
            else
            {
                // Debug.LogError("요청 실패: " + request.error);
                return new List<string> { "요청 실패" };
            }
        }
    }

    //두 단어 사이의 유사도
    public async UniTask<float?> SimilartyTwoWord(string inputWord, string inputWord2)
    {
        ServerConfig config =
        JsonConvert.DeserializeObject<ServerConfig>(
        File.ReadAllText(configPath));

        string url = $"{config.baseUrl}/similarity";

        var data = new { word1 = inputWord, word2 = inputWord2 };
        string jsonData = JsonConvert.SerializeObject(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            await request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                ResultData2 responseData = JsonConvert.DeserializeObject<ResultData2>(jsonResponse);

                if (responseData != null && responseData.result.HasValue)
                {
                    return responseData.result.Value; // 값 반환
                }
                else
                {
                    return null; // 실패 시 null
                }
            }
            else
            {
                // Debug.LogError("요청 실패: " + request.error);
                return null;
            }
        }
    }
}