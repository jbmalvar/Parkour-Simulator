using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System;

[Serializable]
public class ScoreData 
{ 
    public int levelNumber; 
    public string playerName; 
    public float timeSpent; 
}

[Serializable]
public class ScoreListWrapper 
{ 
    public ScoreData[] scores; 
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }
    
    // Using your secure live production server
    public string baseServerUrl = "https://mage-parkour.viewdns.net/api/scores";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SubmitScore(int levelNumber, string playerName, float timeSpent)
    {
        Debug.Log($"[API] Initiating SubmitScore for {playerName} on Level {levelNumber}...");
        StartCoroutine(PostScoreRoutine(levelNumber, playerName, timeSpent));
    }

    public void FetchLeaderboard(int levelNumber, Action<ScoreData[]> callback)
    {
        Debug.Log($"[API] Requesting Top 5 times for Level {levelNumber}...");
        StartCoroutine(GetLeaderboardRoutine(levelNumber, callback));
    }

    private IEnumerator PostScoreRoutine(int level, string player, float time)
    {
        ScoreData payload = new ScoreData { levelNumber = level, playerName = player, timeSpent = time };
        string jsonPayload = JsonUtility.ToJson(payload);
        
        Debug.Log($"[API - POST] Formatted JSON Payload: {jsonPayload}");
        Debug.Log($"[API - POST] Sending to URL: {baseServerUrl}");

        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

        using (UnityWebRequest request = new UnityWebRequest(baseServerUrl, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("<color=green>[API - POST] Success! Data saved to MongoDB.</color>");
                Debug.Log($"[API - POST] Server Response: {request.downloadHandler.text}");
            }
            else 
            {
                Debug.LogError($"<color=red>[API - POST] Submission Failed!</color> Error: {request.error}");
                Debug.LogError($"[API - POST] Server Error Message: {request.downloadHandler.text}");
            }
        }
    }

    private IEnumerator GetLeaderboardRoutine(int level, Action<ScoreData[]> callback)
    {
        string targetUrl = $"{baseServerUrl}/{level}";
        Debug.Log($"[API - GET] Hitting URL: {targetUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(targetUrl))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string rawText = request.downloadHandler.text;
                Debug.Log($"<color=green>[API - GET] Success! Retrieved data:</color> {rawText}");
                
                string wrappedJson = "{\"scores\":" + rawText + "}";
                ScoreListWrapper parsedData = JsonUtility.FromJson<ScoreListWrapper>(wrappedJson);
                
                Debug.Log($"[API - GET] Successfully parsed {parsedData.scores.Length} records into Unity C# objects.");
                callback?.Invoke(parsedData.scores);
            }
            else 
            {
                Debug.LogError($"<color=red>[API - GET] Retrieval Failed!</color> Error: {request.error}");
                callback?.Invoke(null);
            }
        }
    }
}