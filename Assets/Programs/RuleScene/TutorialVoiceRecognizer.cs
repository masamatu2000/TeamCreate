using UnityEngine;
using UnityEngine.InputSystem;
using System.Runtime.InteropServices;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

public class TutorialVoiceRecognizer : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField]
    private TutorialSceneManager tutorialSceneManager;

    [Header("スペースを離した後も結果を受け付ける秒数")]
    [SerializeField]
    private float releaseGraceTime = 3.0f;

    private bool isRadioPressed;
    private float radioReleasedTime = -999f;

    private readonly string[] keywords =
    {
        "鮮魚コーナー",
        "飲料コーナー",
        "お菓子コーナー",
        "惣菜コーナー",
        "捕まえろ"
    };

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private KeywordRecognizer keywordRecognizer;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebVoice_Init(string objectName);

    [DllImport("__Internal")]
    private static extern void WebVoice_Dispose();
#endif

    private void Start()
    {
        if (tutorialSceneManager == null)
        {
            Debug.LogError(
                "TutorialSceneManagerが設定されていません。"
            );

            enabled = false;
            return;
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        keywordRecognizer = new KeywordRecognizer(
            keywords,
            ConfidenceLevel.Medium
        );

        keywordRecognizer.OnPhraseRecognized +=
            OnPhraseRecognized;
#endif

#if UNITY_WEBGL && !UNITY_EDITOR
        WebVoice_Init(gameObject.name);
#endif
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            isRadioPressed = true;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (keywordRecognizer != null &&
                !keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Start();
            }
#endif
        }

        if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            isRadioPressed = false;
            radioReleasedTime = Time.unscaledTime;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (keywordRecognizer != null &&
                keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
#endif
        }
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private void OnPhraseRecognized(
        PhraseRecognizedEventArgs args)
    {
        ProcessRecognizedText(args.text);
    }
#endif

    // WebVoice.jslibから呼び出される
    public void OnWebVoiceResult(string text)
    {
        if (!isActiveAndEnabled)
        {
            return;
        }

        bool canAccept =
            isRadioPressed ||
            Time.unscaledTime - radioReleasedTime
                <= releaseGraceTime;

        if (!canAccept)
        {
            return;
        }

        ProcessRecognizedText(text);
    }

    // WebVoice.jslibから呼び出される
    public void OnWebVoiceError(string message)
    {
        Debug.LogWarning(
            "チュートリアル音声認識：" + message
        );
    }

    // Windows版・Web版で共通の命令処理
    private void ProcessRecognizedText(string text)
    {
        if (string.IsNullOrWhiteSpace(text) ||
            tutorialSceneManager == null)
        {
            return;
        }

        Debug.Log("チュートリアル認識成功：" + text);

        // ブラウザでは句読点や「に行って」などが
        // 付くことがあるため、含まれているかで判定する
        if (text.Contains("捕まえろ"))
        {
            tutorialSceneManager.OnArrestVoiceRecognized();
            return;
        }

        foreach (string keyword in keywords)
        {
            if (keyword == "捕まえろ")
            {
                continue;
            }

            if (text.Contains(keyword))
            {
                // SceneManagerには正式なコーナー名を渡す
                tutorialSceneManager.OnCornerVoiceRecognized(
                    keyword
                );

                return;
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            isRadioPressed = false;
            radioReleasedTime = -999f;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (keywordRecognizer != null &&
                keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }
#endif
        }
    }

    private void OnDestroy()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebVoice_Dispose();
#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (keywordRecognizer != null)
        {
            keywordRecognizer.OnPhraseRecognized -=
                OnPhraseRecognized;

            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }

            keywordRecognizer.Dispose();
            keywordRecognizer = null;
        }
#endif
    }
}