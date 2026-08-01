using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.Speech;

/// <summary>
/// 文章を音声認識し、必要な命令だけを抜き出すクラス
/// </summary>
public class VoiceRecognizer : MonoBehaviour
{
    public event Action<VoiceCommand> OnCommandRecognized;

    [Header("スペースを離した後も結果を受け付ける秒数")]
    [SerializeField] private float releaseGraceTime = 1.0f;

    private DictationRecognizer dictationRecognizer;

    private bool isRadioPressed;
    private float radioReleasedTime = -999f;

    void Start()
    {
        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationResult += OnDictationResult;
        dictationRecognizer.DictationError += OnDictationError;

        // 起動は一度だけ行う
        dictationRecognizer.Start();

        Debug.Log("音声認識を開始しました。スペースを押して話してください");
    }

     void Update()
    {
        if (Keyboard.current == null)
        {
            Debug.LogWarning("キーボードが見つかりません");
            return;
        }

        var spaceKey = Keyboard.current.spaceKey;

        // 押している間は true
        isRadioPressed = spaceKey.isPressed;

        if (spaceKey.wasPressedThisFrame)
        {
            radioReleasedTime = -999f;
            Debug.Log("無線開始：話してください");
        }

        if (spaceKey.wasReleasedThisFrame)
        {
            isRadioPressed = false;
            radioReleasedTime = Time.time;
            Debug.Log("無線終了");
        }
    }

     void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        Debug.Log("認識した文章：" + text);

        // 押している間、または離してから少しの間だけ命令を有効にする
        bool canSendCommand =
        isRadioPressed ||
        Time.time - radioReleasedTime <= releaseGraceTime;
        Debug.Log("命令を送信できるか：" + canSendCommand);
        if (canSendCommand)
        {
            VoiceCommand command = ExtractCommand(text);
            OnCommandRecognized?.Invoke(command);
            Debug.Log(
            "コーナー：" + command.corner +
            " / 色：" + command.clothesColor +
            " / 帽子：" + command.requiresHat +
            " / メガネ：" + command.requiresGlasses +
            " / バッグ：" + command.requiresBag +
            " / 捕獲：" + command.isCaptureCommand
        );
        }
        else
        {
            Debug.Log("無線が押されていないため、命令は無効です");
        }
    }

    /// <summary>
    /// 音声認識した文章から、必要な言葉を抜き出す
    /// </summary>
    private VoiceCommand ExtractCommand(string text)
    {
        VoiceCommand command = new VoiceCommand();

        // 空白を消して判定しやすくする
        string commandText = text
            .Replace(" ", "")
            .Replace("　", "");

        // コーナー判定
        if (commandText.Contains("鮮魚") ||
            commandText.Contains("魚") ||
            commandText.Contains("さかな"))
        {
            command.corner = CornerType.Fish;
        }
        else if (commandText.Contains("野菜") ||
                 commandText.Contains("青果"))
        {
            command.corner = CornerType.Vegetable;
        }
        else if (commandText.Contains("お菓子") ||
                 commandText.Contains("菓子"))
        {
            command.corner = CornerType.Snack;
        }
        else if (commandText.Contains("冷凍"))
        {
            command.corner = CornerType.FrozenFood;
        }
        else if (commandText.Contains("飲料") ||
                 commandText.Contains("飲み物") ||
                 commandText.Contains("ドリンク") ||
                 commandText.Contains("ジュース"))
        {
            command.corner = CornerType.Drink;
        }
        else if (commandText.Contains("惣菜") ||
                 commandText.Contains("おかず") ||
                 commandText.Contains("弁当"))
        {
            command.corner = CornerType.PreparedFood;
        }
        else if (commandText.Contains("精肉") ||
                 commandText.Contains("肉"))
        {
            command.corner = CornerType.Meat;
        }

        // 服の色判定
        if (commandText.Contains("オレンジ"))
        {
            command.clothesColor = CustomerColor.Orange;
        }
        else if (commandText.Contains("赤"))
        {
            command.clothesColor = CustomerColor.Red;
        }
        else if (commandText.Contains("青"))
        {
            command.clothesColor = CustomerColor.Blue;
        }
        else if (commandText.Contains("緑"))
        {
            command.clothesColor = CustomerColor.Green;
        }
        else if (commandText.Contains("黄色") ||
                 commandText.Contains("イエロー"))
        {
            command.clothesColor = CustomerColor.Yellow;
        }
        else if (commandText.Contains("紫"))
        {
            command.clothesColor = CustomerColor.Purple;
        }
        else if (commandText.Contains("黒"))
        {
            command.clothesColor = CustomerColor.Black;
        }
        else if (commandText.Contains("白"))
        {
            command.clothesColor = CustomerColor.White;
        }

        // 持ち物・見た目
        command.requiresHat =
            commandText.Contains("帽子") ||
            commandText.Contains("キャップ");

        command.requiresGlasses =
            commandText.Contains("メガネ") ||
            commandText.Contains("眼鏡");

        command.requiresBag =
            commandText.Contains("バッグ") ||
            commandText.Contains("かばん") ||
            commandText.Contains("カバン");

        // 捕獲命令
        command.isCaptureCommand =
            commandText.Contains("捕まえ") ||
            commandText.Contains("確保") ||
            commandText.Contains("逮捕");

        return command;
    }

    private void OnDictationError(string error, int hresult)
    {
        Debug.LogError("音声認識エラー：" + error);
    }

    private void OnDestroy()
    {
        if (dictationRecognizer == null)
        {
            return;
        }

        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        dictationRecognizer.DictationResult -= OnDictationResult;
        dictationRecognizer.DictationError -= OnDictationError;
        dictationRecognizer.Dispose();
    }
}