using System;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.InputSystem;
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

       // keywordRecognizer.Start();

        Debug.Log("スペースを押すと音声認識");
    }
    private float checkTimer;

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // スペースを押した瞬間：認識開始
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
          
                              keywordRecognizer.Start();
                Debug.Log("無線開始：話してください");
            
        }

        // スペースを離した瞬間：認識停止
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
                Debug.Log("無線終了");
            }
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