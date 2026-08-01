
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.Speech;

public class VoiceRecognizer : MonoBehaviour
{
    public event Action<string> OnCommandRecognized;

    private KeywordRecognizer keywordRecognizer;

    private bool isRadioPressed;
    private float radioReleasedTime;

    // スペースを離してからも、この秒数だけ認識結果を受け付ける
    [SerializeField] private float releaseGraceTime = 1.0f;

    private readonly string[] keywords =
    {

        // 認識したい言葉を追加する
        //下からは捕獲
        "ほかく",
        "つかまえろ",
        "とらえろ",
        "確保",
        "逮捕",
        "行け",
        "鮮魚コーナー",
        "さかなコーナー",
        "野菜コーナー",
        "青果コーナー",
        "お菓子コーナー",
        "菓子コーナー",
        "冷凍食品コーナー",
        "冷凍コーナー",
        "飲料コーナー",
        "飲み物コーナー",
        "ドリンクコーナー",
        "惣菜コーナー",
        "おかずコーナー",
        "精肉コーナー",
        "肉コーナー"
    };

    private void Start()
    {
        keywordRecognizer = new KeywordRecognizer(keywords);
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

        // 起動時に一度だけ開始する
        keywordRecognizer.Start();

        Debug.Log("音声認識を開始しました");
    }

    void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        isRadioPressed = Keyboard.current.spaceKey.isPressed;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Debug.Log("無線開始：話してください");
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            radioReleasedTime = Time.time;
            Debug.Log("無線終了");
        }
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log("認識結果：" + args.text);

        // 押している間、または離してから1秒以内なら命令として送る
        bool canSendCommand =
            isRadioPressed ||
            Time.time - radioReleasedTime <= releaseGraceTime;

        if (canSendCommand)
        {
            OnCommandRecognized?.Invoke(args.text);
        }
        else
        {
            Debug.Log("無線ボタンを押していないため命令を無視しました");
        }
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