using System;
using UnityEngine;
using UnityEngine.Windows.Speech;
/// <summary>
/// 音声認識を行うクラス
/// </summary>
public class VoiceRecognizer : MonoBehaviour
{
    public event Action<string> OnCommandRecognized;

    private KeywordRecognizer keywordRecognizer;

    private readonly string[] keywords =
    {
    "野菜コーナー",
    "青果コーナー",

    "お菓子コーナー",
    "菓子コーナー",

    "冷凍食品コーナー",
    "冷凍コーナー",

    "飲料コーナー",
    "飲み物コーナー",

    "惣菜コーナー",
    "おかずコーナー",

    "精肉コーナー",
    "肉コーナー"
    };

    private void Start()
    {
        keywordRecognizer = new KeywordRecognizer(keywords);

        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

        keywordRecognizer.Start();

        Debug.Log("音声認識を開始しました");
    }
    private float checkTimer;

    private void Update()
    {
        checkTimer += Time.deltaTime;

        if (checkTimer >= 3.0f)
        {
            Debug.Log(
                "音声認識中：" +
                (keywordRecognizer != null &&
                 keywordRecognizer.IsRunning)
            );

            checkTimer = 0.0f;
        }
    }


    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log("認識結果：" + args.text);

        // 認識した言葉をほかのスクリプトへ伝える
        OnCommandRecognized?.Invoke(args.text);
    }

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

        keywordRecognizer.OnPhraseRecognized -= OnPhraseRecognized;
        keywordRecognizer.Dispose();
    }
}