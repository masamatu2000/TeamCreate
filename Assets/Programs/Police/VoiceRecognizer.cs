using System;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceRecognizer : MonoBehaviour
{
    public event Action<string> OnCommandRecognized;

    private KeywordRecognizer keywordRecognizer;

    private readonly string[] keywords =
    {
        "魚コーナー",
        "肉コーナー",
        "野菜コーナー"
    };

    private void Start()
    {
        keywordRecognizer = new KeywordRecognizer(keywords);

        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

        keywordRecognizer.Start();

        Debug.Log("音声認識を開始しました");
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