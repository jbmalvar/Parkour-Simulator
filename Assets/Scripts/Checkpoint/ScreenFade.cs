using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Attach to a Canvas (Screen Space - Overlay, sort order 999)
// with a full-screen black Image child. Assign that Image here.
public class ScreenFade : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.35f;

    void Start()
    {
        if (fadeImage != null)
            fadeImage.color = new Color(0, 0, 0, 0);
    }

    public IEnumerator FadeOut() => Fade(0f, 1f);
    public IEnumerator FadeIn()  => Fade(1f, 0f);

    private IEnumerator Fade(float from, float to)
    {
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(from, to, t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }
}
