using UnityEngine;
using TMPro;

public class SpeedrunTimer : MonoBehaviour
{
    public TextMeshProUGUI timerText;
    private float elapsedTime;
    private bool isRunning;

    private void Start() { elapsedTime = 0f; isRunning = true; }

    private void Update()
    {
        if (!isRunning) return;
        elapsedTime += Time.deltaTime;
        int min = Mathf.FloorToInt(elapsedTime / 60F);
        int sec = Mathf.FloorToInt(elapsedTime % 60F);
        int ms = Mathf.FloorToInt((elapsedTime * 100F) % 100F);
        timerText.text = string.Format("{0:00}:{1:00}.{2:00}", min, sec, ms);
    }

    public float StopTimer() { isRunning = false; return elapsedTime; }
}