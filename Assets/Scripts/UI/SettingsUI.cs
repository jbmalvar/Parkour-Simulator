using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    [Header("Range")]
    public float minSensitivity = 0.05f;
    public float maxSensitivity = 1.0f;

    public const string SensitivityKey = "MouseSensitivity";
    public const float DefaultSensitivity = 0.2f;

    void OnEnable()
    {
        if (sensitivitySlider == null) return;

        sensitivitySlider.minValue = minSensitivity;
        sensitivitySlider.maxValue = maxSensitivity;
        sensitivitySlider.value = PlayerPrefs.GetFloat(SensitivityKey, DefaultSensitivity);
        UpdateLabel(sensitivitySlider.value);
    }

    // Wire to Slider's OnValueChanged event
    public void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(SensitivityKey, value);
        PlayerPrefs.Save();
        UpdateLabel(value);
    }

    public void GoBack() => UIManager.Instance?.GoBack();

    private void UpdateLabel(float value)
    {
        if (sensitivityValueText != null)
            sensitivityValueText.text = value.ToString("F2");
    }
}
