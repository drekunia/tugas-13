using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

public class FinishUIFeedback : MonoBehaviour
{
    private static Sprite _runtimeWhiteSprite;
    [Header("Screen Flash")] public CanvasGroup flashCanvasGroup; // assign ScreenFlash's CanvasGroup
    public float flashDuration = 0.25f;
    public AnimationCurve flashCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Banner")] public CanvasGroup bannerGroup; // optional: a panel with text
    public float bannerDuration = 3.0f;

    [Header("Counter")]
    [Tooltip("Assign either TextMeshProUGUI or legacy UGUI Text. Prefer TMP.")]
    public TMP_Text counterTextTMP; // TextMeshPro (TMP) text reference
    [FormerlySerializedAs("counterText")] public Text counterTextUGUI;    // Legacy UGUI Text (optional for backward compatibility)

    private Coroutine _flashRoutine;
    private Coroutine _bannerRoutine;

    private int _counter = 0;
    public int Counter => _counter;

    private void Awake()
    {
        // Ensure groups start hidden and are not blocked by parent groups
        if (flashCanvasGroup != null)
        {
            flashCanvasGroup.alpha = 0f;
            flashCanvasGroup.ignoreParentGroups = true;
            if (!flashCanvasGroup.gameObject.activeSelf) flashCanvasGroup.gameObject.SetActive(true);
        }

        if (bannerGroup != null)
        {
            bannerGroup.alpha = 0f;
            bannerGroup.ignoreParentGroups = true;
            if (!bannerGroup.gameObject.activeSelf) bannerGroup.gameObject.SetActive(true);
        }

        // Initialize counter UI if assigned
        UpdateCounterUI();
    }

    private void UpdateCounterUI()
    {
        string s = string.Concat("Summit: ", _counter.ToString());
        if (counterTextTMP != null)
        {
            counterTextTMP.text = s;
        }
        if (counterTextUGUI != null)
        {
            counterTextUGUI.text = s;
        }
    }

    public void IncrementCounter(int amount = 1)
    {
        _counter += amount;
        if (_counter < 0) _counter = 0;
        UpdateCounterUI();
    }

    public void Flash()
    {
        if (flashCanvasGroup == null)
        {
            Debug.LogWarning("[FinishUIFeedback] Flash requested but flashCanvasGroup is not assigned.", this);
            return;
        }

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(DoFlash());
    }

    public void ShowBanner()
    {
        if (bannerGroup == null)
        {
            Debug.LogWarning("[FinishUIFeedback] ShowBanner requested but bannerGroup is not assigned. Assign the UI CanvasGroup in the inspector.", this);
            return;
        }

        if (_bannerRoutine != null) StopCoroutine(_bannerRoutine);
        _bannerRoutine = StartCoroutine(DoBanner());
    }

    private IEnumerator DoFlash()
    {
        if (!flashCanvasGroup.gameObject.activeSelf) flashCanvasGroup.gameObject.SetActive(true);

        Image[] imgs = flashCanvasGroup.GetComponentsInChildren<Image>(true);
        Color[] originalColors = null;
        Sprite[] originalSprites = null;
        if (imgs != null && imgs.Length > 0)
        {
            originalColors = new Color[imgs.Length];
            originalSprites = new Sprite[imgs.Length];
            for (int i = 0; i < imgs.Length; i++)
            {
                var img = imgs[i];
                originalColors[i] = img.color;
                originalSprites[i] = img.sprite;

                if (Mathf.Approximately(originalColors[i].a, 0f))
                {
                    var c = img.color;
                    c.a = 1f;
                    img.color = c;
                }

                if (img.sprite == null)
                {
                    if (_runtimeWhiteSprite == null)
                    {
                        var tex = Texture2D.whiteTexture;
                        _runtimeWhiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        _runtimeWhiteSprite.name = "FinishUIFeedback_RuntimeWhite";
                    }
                    img.sprite = _runtimeWhiteSprite;
                }
            }
        }

        float t = 0f;
        while (t < flashDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = flashCurve.Evaluate(Mathf.Clamp01(t / flashDuration));
            float pulse = k <= 0.5f ? (k * 2f) : (2f - k * 2f);
            flashCanvasGroup.alpha = pulse;
            yield return null;
        }
        flashCanvasGroup.alpha = 0f;
        if (imgs != null && originalColors != null && imgs.Length == originalColors.Length)
        {
            for (int i = 0; i < imgs.Length; i++)
            {
                imgs[i].color = originalColors[i];
            }
        }
        _flashRoutine = null;
    }

    private IEnumerator DoBanner()
    {
        if (!bannerGroup.gameObject.activeSelf) bannerGroup.gameObject.SetActive(true);

        float t = 0f;
        while (t < bannerDuration)
        {
            t += Time.unscaledDeltaTime; // use unscaled time for reliability
            float p = Mathf.Clamp01(t / bannerDuration);
            // fade in first 20%, hold, fade out last 20%
            if (p < 0.2f) bannerGroup.alpha = Mathf.InverseLerp(0f, 0.2f, p);
            else if (p > 0.8f) bannerGroup.alpha = Mathf.InverseLerp(1f, 0.8f, p);
            else bannerGroup.alpha = 1f;
            yield return null;
        }
        bannerGroup.alpha = 0f;
        _bannerRoutine = null;
    }
}
