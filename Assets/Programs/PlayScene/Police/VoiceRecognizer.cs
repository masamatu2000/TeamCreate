using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows.Speech;
using TMPro;

/// <summary>
/// 音声を認識し、警備員への命令や
/// 捕獲確認の「はい」「いいえ」を通知するクラス。
///
/// 音声認識に使用するキーワードはCSVから読み込む。
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
    [SerializeField]
    private bool forceKeywordRecognizer = false;
    [Header("キーワードCSV")]
    [SerializeField]
    private TextAsset keywordCsv;

    [Header("スペースを離した後も結果を受け付ける秒数")]
    [SerializeField]
    private float releaseGraceTime = 1.0f;

    [Header("UI")]
    [SerializeField]
    private TextMeshProUGUI BottonText;
    [Header("音声認識結果UI")]
    [SerializeField]
    private TextMeshProUGUI recognizedText;

    [SerializeField]
    private float recognizedTextDisplayTime = 2.0f;

    private Coroutine recognizedTextCoroutine;
    private DictationRecognizer dictationRecognizer;
    private KeywordRecognizer keywordRecognizer;

    private bool isRadioPressed;
    private float radioReleasedTime = -999f;

    /// <summary>
    /// DictationRecognizerを使用しているか
    /// </summary>
    private bool useDictation = true;

    // =========================================================
    // CSVから読み込んだキーワード
    // =========================================================

    /// <summary>
    /// KeywordRecognizerに登録する全単語
    /// </summary>
    private readonly List<string> allKeywords =
        new List<string>();

    /// <summary>
    /// コーナー
    /// キーワード → CornerType
    /// </summary>
    private readonly Dictionary<string, CornerType> cornerKeywords =
        new Dictionary<string, CornerType>();

    /// <summary>
    /// 色
    /// キーワード → CustomerColor
    /// </summary>
    private readonly Dictionary<string, CustomerColor> colorKeywords =
        new Dictionary<string, CustomerColor>();

    /// <summary>
    /// 捕獲命令
    /// </summary>
    private readonly List<string> captureKeywords =
        new List<string>();

    /// <summary>
    /// 帽子
    /// </summary>
    private readonly List<string> hatKeywords =
        new List<string>();

    /// <summary>
    /// メガネ
    /// </summary>
    private readonly List<string> glassesKeywords =
        new List<string>();

    /// <summary>
    /// バッグ
    /// </summary>
    private readonly List<string> bagKeywords =
        new List<string>();

    /// <summary>
    /// 「はい」
    /// </summary>
    private readonly List<string> yesKeywords =
        new List<string>();

    /// <summary>
    /// 「いいえ」
    /// </summary>
    private readonly List<string> noKeywords =
        new List<string>();

    private readonly List<string> stopKeywords =
    new List<string>();

    [SerializeField]
    private PlaySceneManager playSceneManager;
    private void Start()
    {
        // ========================================
        // 音声認識結果は最初は非表示
        // ========================================
        if (recognizedText != null)
        {
            recognizedText.gameObject.SetActive(false);
        }
        LoadKeywordsFromCsv();

        if (allKeywords.Count == 0)
        {
            Debug.LogError(
                "音声認識用のキーワードが1つもありません。" +
                "VoiceRecognizerのkeywordCsvを確認してください。"
            );

            return;
        }

        if (forceKeywordRecognizer)
        {
            Debug.Log(
                "KeywordRecognizer固定モードで開始します"
            );

            SwitchToKeywordRecognizer();
        }
        else
        {
            Debug.Log(
                "DictationRecognizerを試します"
            );

            StartDictationRecognizer();
        }
    }


    private void Update()
    {
        if (playSceneManager != null &&
            !playSceneManager.IsGameStarted())
        {
            return;
        }

        
        // 以下今まで通り
        if (Keyboard.current == null)
        {
            return;
        }

        // ========================================
        // スペースを押した瞬間
        // ========================================

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            isRadioPressed = true;
            if (BottonText != null)
            {
                BottonText.text = "マイクに話しかけてください";
            }
            Debug.Log(
                "音声入力開始"
            );
        }

        // ========================================
        // スペースを離した瞬間
        // ========================================

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
        {
            isRadioPressed = false;

            radioReleasedTime = Time.time;
            if (BottonText != null)
            {
                BottonText.text = "ボタンを押してください";
            }
            Debug.Log(
                "音声入力終了"
            );
        }
    }


    // =========================================================
    // CSV
    // =========================================================

    /// <summary>
    /// CSVから音声認識キーワードを読み込む
    /// </summary>
    private void LoadKeywordsFromCsv()
    {
        if (keywordCsv == null)
        {
            Debug.LogError(
                "VoiceRecognizerにCSVが設定されていません。"
            );

            return;
        }

        // 念のため既存データを削除
        allKeywords.Clear();

        cornerKeywords.Clear();
        colorKeywords.Clear();

        captureKeywords.Clear();

        hatKeywords.Clear();
        glassesKeywords.Clear();
        bagKeywords.Clear();

        yesKeywords.Clear();
        noKeywords.Clear();
        stopKeywords.Clear();

        string[] lines =
            keywordCsv.text.Split(
                new[]
                {
                    '\r',
                    '\n'
                },
                StringSplitOptions.RemoveEmptyEntries
            );


        for (int i = 0; i < lines.Length; i++)
        {
            string line =
                lines[i].Trim();

            if (string.IsNullOrEmpty(line))
            {
                continue;
            }


            // ========================================
            // 1行目のヘッダーを無視
            // Type,Keyword,Value
            // ========================================

            if (i == 0 &&
                line.StartsWith("Type"))
            {
                continue;
            }


            string[] columns =
                line.Split(',');


            if (columns.Length < 3)
            {
                Debug.LogWarning(
                    "CSVの形式が正しくありません：" +
                    line
                );

                continue;
            }


            string type =
                columns[0].Trim();

            string keyword =
                columns[1].Trim();

            string value =
                columns[2].Trim();


            // キーワードが空なら無視
            if (string.IsNullOrEmpty(keyword))
            {
                continue;
            }


            // ========================================
            // KeywordRecognizer用
            // ========================================

            if (!allKeywords.Contains(keyword))
            {
                allKeywords.Add(keyword);
            }


            // ========================================
            // 種類ごとに登録
            // ========================================

            switch (type)
            {
                // ------------------------------------
                // コーナー
                // ------------------------------------

                case "Corner":

                    AddCornerKeyword(
                        keyword,
                        value
                    );

                    break;


                // ------------------------------------
                // 色
                // ------------------------------------

                case "Color":

                    AddColorKeyword(
                        keyword,
                        value
                    );

                    break;


                // ------------------------------------
                // 捕獲
                // ------------------------------------

                case "Capture":

                    if (!captureKeywords.Contains(keyword))
                    {
                        captureKeywords.Add(keyword);
                    }

                    break;


                // ------------------------------------
                // 特徴
                // ------------------------------------

                case "Feature":

                    AddFeatureKeyword(
                        keyword,
                        value
                    );

                    break;


                // ------------------------------------
                // はい / いいえ
                // ------------------------------------

                case "Confirmation":

                    AddConfirmationKeyword(
                        keyword,
                        value
                    );

                    break;

                case "Stop":

                    if (!stopKeywords.Contains(keyword))
                    {
                        stopKeywords.Add(keyword);
                    }

                    break;

                default:

                    Debug.LogWarning(
                        "不明なTypeがあります：" +
                        type
                    );

                    break;
            }
        }


        Debug.Log(
            "CSV読み込み完了：" +
            allKeywords.Count +
            "個の音声キーワードを登録しました。"
        );
    }


    /// <summary>
    /// コーナーのキーワードを登録
    /// </summary>
    private void AddCornerKeyword(
        string keyword,
        string value)
    {
        CornerType corner;


        if (!Enum.TryParse(
            value,
            true,
            out corner))
        {
            Debug.LogWarning(
                "CornerTypeに存在しない値です：" +
                value
            );

            return;
        }


        if (!cornerKeywords.ContainsKey(keyword))
        {
            cornerKeywords.Add(
                keyword,
                corner
            );
        }
    }


    /// <summary>
    /// 色キーワードを登録
    /// </summary>
    private void AddColorKeyword(
        string keyword,
        string value)
    {
        CustomerColor color;


        if (!Enum.TryParse(
            value,
            true,
            out color))
        {
            Debug.LogWarning(
                "CustomerColorに存在しない値です：" +
                value
            );

            return;
        }


        if (!colorKeywords.ContainsKey(keyword))
        {
            colorKeywords.Add(
                keyword,
                color
            );
        }
    }


    /// <summary>
    /// 特徴キーワード登録
    /// </summary>
    private void AddFeatureKeyword(
        string keyword,
        string value)
    {
        switch (value)
        {
            case "Hat":

                if (!hatKeywords.Contains(keyword))
                {
                    hatKeywords.Add(keyword);
                }

                break;


            case "Glasses":

                if (!glassesKeywords.Contains(keyword))
                {
                    glassesKeywords.Add(keyword);
                }

                break;


            case "Bag":

                if (!bagKeywords.Contains(keyword))
                {
                    bagKeywords.Add(keyword);
                }

                break;


            default:

                Debug.LogWarning(
                    "不明なFeatureです：" +
                    value
                );

                break;
        }
    }


    /// <summary>
    /// はい / いいえを登録
    /// </summary>
    private void AddConfirmationKeyword(
        string keyword,
        string value)
    {
        switch (value)
        {
            case "Yes":

                if (!yesKeywords.Contains(keyword))
                {
                    yesKeywords.Add(keyword);
                }

                break;


            case "No":

                if (!noKeywords.Contains(keyword))
                {
                    noKeywords.Add(keyword);
                }

                break;


            default:

                Debug.LogWarning(
                    "不明なConfirmationです：" +
                    value
                );

                break;
        }
    }


    // =========================================================
    // DictationRecognizer
    // =========================================================

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
        if (!CanAcceptVoiceResult())
        {
            return;
        }


        Debug.Log(
            "音声認識結果：" +
            text
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


    // =========================================================
    // KeywordRecognizer
    // =========================================================

    /// <summary>
    /// KeywordRecognizerへ切り替える
    /// </summary>
    private void SwitchToKeywordRecognizer()
    {
        useDictation = false;


        // ========================================
        // Dictation停止
        // ========================================

        if (dictationRecognizer != null)
        {
            try
            {
                if (dictationRecognizer.Status ==
                    SpeechSystemStatus.Running)
                {
                    dictationRecognizer.Stop();
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    "DictationRecognizer停止時エラー：" +
                    exception.Message
                );
            }
        }


        // ========================================
        // KeywordRecognizer
        // ========================================

        if (keywordRecognizer != null)
        {
            // すでに動いているなら何もしない
            if (keywordRecognizer.IsRunning)
            {
                return;
            }

            // 存在するが停止している場合は破棄
            keywordRecognizer.OnPhraseRecognized
                -= OnPhraseRecognized;

            keywordRecognizer.Dispose();

            keywordRecognizer = null;
        }


        try
        {
            keywordRecognizer =
                new KeywordRecognizer(
                    allKeywords.ToArray(),
                    ConfidenceLevel.Low
                );


            keywordRecognizer.OnPhraseRecognized
                += OnPhraseRecognized;


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


        string text =
            args.text;


        Debug.Log(
            "キーワード認識結果：" +
            text
        );


        ProcessRecognizedText(text);
    }


    // =========================================================
    // 共通処理
    // =========================================================

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
        // ========================================
        // 認識した音声を画面に表示
        // ========================================

        ShowRecognizedText(text);


        // ========================================
        // はい
        // ========================================

        if (ContainsAny(
            text,
            yesKeywords))
        {
            Debug.Log(
                "確認音声：はい"
            );


            OnConfirmationRecognized
                ?.Invoke(true);


            return;
        }


        // ========================================
        // いいえ
        // ========================================

        if (ContainsAny(
            text,
            noKeywords))
        {
            Debug.Log(
                "確認音声：いいえ"
            );


            OnConfirmationRecognized
                ?.Invoke(false);


            return;
        }


        // ========================================
        // 通常命令
        // ========================================

        VoiceCommand command =
            CreateCommand(text);


        OnCommandRecognized
            ?.Invoke(command);
    }


    /// <summary>
    /// 認識した文章からVoiceCommandを作る
    /// </summary>
    private VoiceCommand CreateCommand(
        string text)
    {
        VoiceCommand command =
            new VoiceCommand();


        // ========================================
        // コーナー
        // ========================================

        foreach (
            KeyValuePair<string, CornerType> pair
            in cornerKeywords)
        {
            if (text.Contains(pair.Key))
            {
                command.corner =
                    pair.Value;

                break;
            }
        }


        // ========================================
        // 捕獲命令
        // ========================================

        if (ContainsAny(
            text,
            captureKeywords))
        {
            command.isCaptureCommand =
                true;
        }


        // ========================================
        // 色
        // ========================================

        foreach (
            KeyValuePair<string, CustomerColor> pair
            in colorKeywords)
        {
            if (text.Contains(pair.Key))
            {
                command.clothesColor =
                    pair.Value;

                break;
            }
        }


        // ========================================
        // 帽子
        // ========================================

        if (ContainsAny(
            text,
            hatKeywords))
        {
            command.requiresHat =
                true;
        }


        // ========================================
        // メガネ
        // ========================================

        if (ContainsAny(
            text,
            glassesKeywords))
        {
            command.requiresGlasses =
                true;
        }


        // ========================================
        // バッグ
        // ========================================

        if (ContainsAny(
            text,
            bagKeywords))
        {
            command.requiresBag =
                true;
        }

        // ========================================
        // 停止命令
        // ========================================

        if (ContainsAny(
            text,
            stopKeywords))
        {
            command.isStopCommand = true;
        }
        // ========================================
        // 特徴が指定されていたら
        // 自動的に捕獲命令として扱う
        // ========================================

        bool hasTargetFeature =
            command.clothesColor != CustomerColor.None ||
            command.requiresHat ||
            command.requiresGlasses ||
            command.requiresBag;

        if (hasTargetFeature)
        {
            command.isCaptureCommand = true;
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
    /// textの中に指定されたキーワードが
    /// 1つでも含まれているか
    /// </summary>
    private bool ContainsAny(
        string text,
        List<string> words)
    {
        foreach (string word in words)
        {
            if (text.Contains(word))
            {
                return true;
            }
        }


        return false;
    }
    // =========================================================
    // 音声認識結果UI
    // =========================================================

    /// <summary>
    /// 認識した音声を画面に一定時間表示する
    /// </summary>
    private void ShowRecognizedText(
        string text)
    {
        if (recognizedText == null)
        {
            return;
        }

        // 前回の表示処理が残っていたら停止
        if (recognizedTextCoroutine != null)
        {
            StopCoroutine(
                recognizedTextCoroutine
            );
        }

        recognizedTextCoroutine =
            StartCoroutine(
                ShowRecognizedTextCoroutine(text)
            );
    }


    /// <summary>
    /// 認識結果を表示して一定時間後に消す
    /// </summary>
    private IEnumerator ShowRecognizedTextCoroutine(
        string text)
    {
        recognizedText.gameObject.SetActive(true);

        recognizedText.text =
            "認識しました！\n" +
            "「" + text + "」";

        yield return new WaitForSeconds(
            recognizedTextDisplayTime
        );

        recognizedText.gameObject.SetActive(false);

        recognizedTextCoroutine = null;
    }

    // =========================================================
    // Push To Talk
    // =========================================================

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
   
    

    // =========================================================
    // 終了処理
    // =========================================================

    private void OnDestroy()
    {
        // ========================================
        // DictationRecognizer
        // ========================================

        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationResult
                -= OnDictationResult;


            dictationRecognizer.DictationError
                -= OnDictationError;


            if (dictationRecognizer.Status ==
                SpeechSystemStatus.Running)
            {
                dictationRecognizer.Stop();
            }


            dictationRecognizer.Dispose();

            dictationRecognizer = null;
        }


        // ========================================
        // KeywordRecognizer
        // ========================================

        if (keywordRecognizer != null)
        {
            keywordRecognizer.OnPhraseRecognized
                -= OnPhraseRecognized;


            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }


            keywordRecognizer.Dispose();

            keywordRecognizer = null;
        }
    }
}