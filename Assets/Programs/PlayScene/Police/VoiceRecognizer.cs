using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.Speech;

/// <summary>
/// 音声を認識し、警備員への命令や
/// 捕獲確認の「はい」「いいえ」を通知するクラス
/// </summary>
public class VoiceRecognizer : MonoBehaviour
{
    /// <summary>
    /// 通常の音声命令をPoliceControllerへ送る
    /// </summary>
    public event Action<VoiceCommand> OnCommandRecognized;

    /// <summary>
    /// 捕獲確認の結果をPoliceControllerへ送る
    /// true = はい
    /// false = いいえ
    /// </summary>
    public event Action<bool> OnConfirmationRecognized;

    [Header("スペースを離した後も結果を受け付ける秒数")]
    [SerializeField] private float releaseGraceTime = 1.0f;

    private DictationRecognizer dictationRecognizer;
    private KeywordRecognizer keywordRecognizer;

    private bool isRadioPressed;
    private float radioReleasedTime = -999f;

    /// <summary>
    /// DictationRecognizerが使用できるか
    /// </summary>
    private bool useDictation = true;

    /// <summary>
    /// KeywordRecognizerで使用する単語
    /// </summary>
    private readonly string[] keywords =
    {
        // コーナー
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
        "肉コーナー",

        // 捕獲
        "捕まえろ",
        "捕まえて",
        "確保",

        // 捕獲確認
        "はい",
        "いいえ"
    };

    private void Start()
    {
        StartDictationRecognizer();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        // スペースを押した瞬間
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isRadioPressed = true;

            Debug.Log("音声入力開始");
        }

        // スペースを離した瞬間
        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            isRadioPressed = false;

            radioReleasedTime = Time.time;

            Debug.Log("音声入力終了");
        }
    }

    /// <summary>
    /// DictationRecognizerを開始する
    /// </summary>
    private void StartDictationRecognizer()
    {
        try
        {
            dictationRecognizer =
                new DictationRecognizer();

            dictationRecognizer.DictationResult
                += OnDictationResult;

            dictationRecognizer.DictationError
                += OnDictationError;

            dictationRecognizer.Start();

            useDictation = true;

            Debug.Log(
                "DictationRecognizerを開始しました。" +
                "スペースを押して話してください"
            );
        }
        catch (Exception exception)
        {
            Debug.LogWarning(
                "DictationRecognizerを開始できませんでした：" +
                exception.Message
            );

            SwitchToKeywordRecognizer();
        }
    }

    /// <summary>
    /// DictationRecognizerの認識結果
    /// </summary>
    private void OnDictationResult(
        string text,
        ConfidenceLevel confidence)
    {
        // スペースを押していない場合は基本的に無視
        // ただし離してから少しだけ猶予を持たせる
        if (!CanAcceptVoiceResult())
        {
            return;
        }

        Debug.Log(
            "音声認識結果：" + text
        );

        ProcessRecognizedText(text);
    }

    /// <summary>
    /// DictationRecognizerのエラー
    /// </summary>
    private void OnDictationError(
        string error,
        int hresult)
    {
        Debug.LogWarning(
            "DictationRecognizerエラー：" +
            error +
            " / " +
            hresult
        );

        SwitchToKeywordRecognizer();
    }

    /// <summary>
    /// KeywordRecognizerへ切り替える
    /// </summary>
    private void SwitchToKeywordRecognizer()
    {
        useDictation = false;

        // Dictationを停止
        if (dictationRecognizer != null)
        {
            try
            {
                if (dictationRecognizer.Status
                    == SpeechSystemStatus.Running)
                {
                    dictationRecognizer.Stop();
                }
            }
            catch
            {
                // 停止時の例外は無視
            }
        }

        // すでにKeywordRecognizerが存在しているなら
        // 新しく作らない
        if (keywordRecognizer != null)
        {
            return;
        }

        try
        {
            keywordRecognizer =
                new KeywordRecognizer(
                    keywords,
                    ConfidenceLevel.Low
                );


            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

            keywordRecognizer.Start();

            Debug.Log(
                "KeywordRecognizerに切り替えました。" +
                "スペースを押して話してください"
            );
        }
        catch (Exception exception)
        {
            Debug.LogError(
                "KeywordRecognizerを開始できませんでした：" +
                exception.Message
            );
        }
    }

    /// <summary>
    /// KeywordRecognizerの結果
    /// </summary>
    private void OnPhraseRecognized(
        PhraseRecognizedEventArgs args)
    {
        if (!CanAcceptVoiceResult())
        {
            return;
        }

        string text = args.text;

        Debug.Log(
            "キーワード認識結果：" +
            text
        );

        ProcessRecognizedText(text);
    }

    /// <summary>
    /// Dictation / Keyword 共通の認識結果処理
    /// </summary>
    private void ProcessRecognizedText(
        string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        // ====================================
        // 捕獲確認「はい」「いいえ」
        // ====================================

        if (IsYes(text))
        {
            Debug.Log(
                "確認音声：はい"
            );

            OnConfirmationRecognized
                ?.Invoke(true);

            return;
        }

        if (IsNo(text))
        {
            Debug.Log(
                "確認音声：いいえ"
            );

            OnConfirmationRecognized
                ?.Invoke(false);

            return;
        }

        // ====================================
        // 通常命令
        // ====================================

        VoiceCommand command =
            CreateCommand(text);

        OnCommandRecognized
            ?.Invoke(command);
    }

    /// <summary>
    /// 「はい」かどうか
    /// </summary>
    private bool IsYes(
        string text)
    {
        return
            text.Contains("はい") ||
            text.Contains("ハイ");
    }

    /// <summary>
    /// 「いいえ」かどうか
    /// </summary>
    private bool IsNo(
        string text)
    {
        return
            text.Contains("いいえ") ||
            text.Contains("いいや") ||
            text.Contains("いや");
    }

    /// <summary>
    /// 認識した文章からVoiceCommandを作る
    /// </summary>
    private VoiceCommand CreateCommand(
        string text)
    {
        VoiceCommand command =
            new VoiceCommand();

        // ====================================
        // コーナー
        // ====================================

        if (text.Contains("鮮魚") ||
            text.Contains("さかな"))
        {
            command.corner =
                CornerType.Fish;
        }
        else if (
            text.Contains("野菜") ||
            text.Contains("青果"))
        {
            command.corner =
                CornerType.Vegetable;
        }
        else if (
            text.Contains("お菓子") ||
            text.Contains("菓子"))
        {
            command.corner =
                CornerType.Snack;
        }
        else if (
            text.Contains("冷凍"))
        {
            command.corner =
                CornerType.FrozenFood;
        }
        else if (
            text.Contains("飲料") ||
            text.Contains("飲み物") ||
            text.Contains("ドリンク"))
        {
            command.corner =
                CornerType.Drink;
        }
        else if (
            text.Contains("惣菜") ||
            text.Contains("おかず"))
        {
            command.corner =
                CornerType.PreparedFood;
        }
        else if (
            text.Contains("精肉") ||
            text.Contains("肉コーナー"))
        {
            command.corner =
                CornerType.Meat;
        }

        // ====================================
        // 捕獲命令
        // ====================================

        if (text.Contains("捕まえ")||
            text.Contains("確保")||
            text.Contains("逮捕")||
            text.Contains("いけ")||
            text.Contains("やれ"))
        {
            command.isCaptureCommand =
                true;
        }

        // ====================================
        // 色
        // ====================================

        if (text.Contains("赤"))
        {
            command.clothesColor =
                CustomerColor.Red;
        }
        else if (text.Contains("青"))
        {
            command.clothesColor =
                CustomerColor.Blue;
        }
        else if (text.Contains("緑"))
        {
            command.clothesColor =
                CustomerColor.Green;
        }
        else if (text.Contains("オレンジ"))
        {
            command.clothesColor =
                CustomerColor.Orange;
        }
        else if (text.Contains("黄"))
        {
            command.clothesColor =
                CustomerColor.Yellow;
        }
        else if (text.Contains("紫"))
        {
            command.clothesColor =
                CustomerColor.Purple;
        }
        else if (text.Contains("黒"))
        {
            command.clothesColor =
                CustomerColor.Black;
        }
        else if (text.Contains("白"))
        {
            command.clothesColor =
                CustomerColor.White;
        }

        // ====================================
        // 特徴
        // ====================================

        if (text.Contains("帽子"))
        {
            command.requiresHat =
                true;
        }

        if (text.Contains("眼鏡") ||
            text.Contains("メガネ"))
        {
            command.requiresGlasses =
                true;
        }

        if (text.Contains("バッグ") ||
            text.Contains("鞄") ||
            text.Contains("かばん"))
        {
            command.requiresBag =
                true;
        }

        Debug.Log(
            "命令解析完了：" +
            " Corner=" +
            command.corner +
            " Color=" +
            command.clothesColor +
            " Hat=" +
            command.requiresHat +
            " Glasses=" +
            command.requiresGlasses +
            " Bag=" +
            command.requiresBag +
            " Capture=" +
            command.isCaptureCommand
        );

        return command;
    }

    /// <summary>
    /// 現在音声認識結果を受け取ってよいか
    /// </summary>
    private bool CanAcceptVoiceResult()
    {
        // スペースを現在押している
        if (isRadioPressed)
        {
            return true;
        }

        // スペースを離した直後
        if (Time.time - radioReleasedTime
            <= releaseGraceTime)
        {
            return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        // DictationRecognizer
        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult
                -= OnDictationResult;

            dictationRecognizer.DictationError
                -= OnDictationError;

            if (dictationRecognizer.Status
                == SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }

            dictationRecognizer.Dispose();
            dictationRecognizer = null;
        }

        // KeywordRecognizer
        if (keywordRecognizer != null)
        {

            keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;

            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }

            keywordRecognizer.Dispose();
            keywordRecognizer = null;
        }
    }
}