using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.Speech;

public class TutorialVoiceRecognizer : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField]
    private TutorialSceneManager tutorialSceneManager;


    private KeywordRecognizer keywordRecognizer;


    // =========================================================
    // チュートリアルで使用する言葉
    // =========================================================

    private readonly string[] keywords =
    {
        "鮮魚コーナー",
        "飲料コーナー",
        "お菓子コーナー",
        "惣菜コーナー",
        "捕まえろ"
    };


    private void Start()
    {
        keywordRecognizer =
            new KeywordRecognizer(
                keywords,
                ConfidenceLevel.Medium
            );

        keywordRecognizer.OnPhraseRecognized +=
            OnPhraseRecognized;
    }


    private void Update()
    {
        Keyboard keyboard =
            Keyboard.current;

        if (keyboard == null)
        {
            return;
        }


        // =====================================================
        // スペースを押した瞬間
        // =====================================================

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            StartRecognition();
        }


        // =====================================================
        // スペースを離した瞬間
        // =====================================================

        if (keyboard.spaceKey.wasReleasedThisFrame)
        {
            StopRecognition();
        }
    }


    // =========================================================
    // 音声認識開始
    // =========================================================

    private void StartRecognition()
    {
        if (keywordRecognizer == null)
        {
            return;
        }

        if (!keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Start();

            Debug.Log(
                "チュートリアル音声認識開始"
            );
        }
    }


    // =========================================================
    // 音声認識停止
    // =========================================================

    private void StopRecognition()
    {
        if (keywordRecognizer == null)
        {
            return;
        }

        if (keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();

            Debug.Log(
                "チュートリアル音声認識停止"
            );
        }
    }


    // =========================================================
    // 認識成功
    // =========================================================

    private void OnPhraseRecognized(
        PhraseRecognizedEventArgs args
    )
    {
        string recognizedText =
            args.text;

        Debug.Log(
            $"認識成功 : {recognizedText}"
        );


        // =====================================================
        // 捕まえろ
        // =====================================================

        if (recognizedText == "捕まえろ")
        {
            // 後で捕獲処理を接続
            tutorialSceneManager
                .OnArrestVoiceRecognized();

            return;
        }


        // =====================================================
        // コーナー名
        // =====================================================

        switch (recognizedText)
        {
            case "鮮魚コーナー":
            case "飲料コーナー":
            case "お菓子コーナー":
            case "惣菜コーナー":

                tutorialSceneManager
                    .OnCornerVoiceRecognized(
                        recognizedText
                    );

                break;
        }
    }


    // =========================================================
    // 終了処理
    // =========================================================

    private void OnDestroy()
    {
        if (keywordRecognizer == null)
        {
            return;
        }


        if (keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
        }


        keywordRecognizer
            .OnPhraseRecognized -=
                OnPhraseRecognized;


        keywordRecognizer.Dispose();
    }
}