using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// PlayScene全体のゲーム進行を管理するクラス
///
/// 管理するもの
/// ・ゲーム開始カウントダウン
/// ・制限時間
/// ・現在のお客さん人数
/// ・売上金
/// ・捕まえた泥棒の人数
/// ・逃がした泥棒の人数
/// ・クレーム数
/// ・ゲーム終了処理
/// </summary>
public class PlaySceneManager : MonoBehaviour
{
    [Header("ゲーム開始")]
    [SerializeField]
    private TextMeshProUGUI countdownText;

    // ゲームが開始したか
    private bool isGameStarted = false;

    // カウントダウン中か
    private bool isCountdownStarted = false;


    [SerializeField]
    private TextMeshProUGUI captureResultText;

    [SerializeField]
    private float captureResultDisplayTime = 2.0f;

    [Header("ゲーム時間")]
    [SerializeField]
    private float gameTime = 180.0f;
    [Header("残り時間の演出")]
    [SerializeField]
    private float warningTime = 30.0f;

    [SerializeField]
    private Color normalTimeColor = Color.white;

    [SerializeField]
    private Color warningTimeColor = Color.red;

    [SerializeField]
    private float finalCountdownTime = 5.0f;

    [Header("終了演出")]
    [SerializeField]
    private float zeroDisplayTime = 0.3f;

    [SerializeField]
    private float finishDisplayTime = 2.0f;

    [Header("誤認逮捕ペナルティ")]
    [SerializeField]
    private float wrongArrestTimePenalty = 20.0f;
    [SerializeField]
    private int wrongArrestSalesPenalty = 1000;

    [Header("UI")]
    [SerializeField]
    private TextMeshProUGUI timeText;

    [SerializeField]
    private TextMeshProUGUI customerCountText;

    [SerializeField]
    private TextMeshProUGUI salesText;

    [Header("泥棒残り人数UI")]
    [SerializeField]
    private TextMeshProUGUI thiefCountText;
    [Header("お客さん管理")]
    [SerializeField]
    private CustomerManager CustomerManager;
    [Header("音声認識")]
    [SerializeField]
    private VoiceRecognizer voiceRecognizer;

    [Header("売上設定")]
    [SerializeField]
    private int minSalesIncrease = 100;

    [SerializeField]
    private int maxSalesIncrease = 1000;

    [SerializeField]
    private float salesIncreaseInterval = 3.0f;


    [Header("リザルトシーン")]
    [SerializeField]
    private string resultSceneName = "ResultScene";

   

    // 現在のお客さん人数
    private int currentCustomerCount = 0;

    // 売上
    private int sales = 0;

    // 捕まえた人数
    private int caughtCount = 0;

    // 捕まえた泥棒の人数
    private int caughtThiefCount = 0;

    // 逃がした泥棒
    private int escapedThiefCount = 0;

    // クレーム数
    private int complaintCount = 0;

    // 売上加算用タイマー
    private float salesTimer = 0.0f;

    // ゲームが終了したか
    private bool isGameFinished = false;


    private void Start()
    {

        // 売上タイマー初期化
        salesTimer = salesIncreaseInterval;

        // ゲーム開始前
        isGameStarted = false;
        isCountdownStarted = false;

        if (captureResultText != null)
        {
            captureResultText.gameObject.SetActive(false);
        }

        // 中央に開始メッセージを表示
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.color = normalTimeColor;
            countdownText.text = "Push Space To Start";
        }


        UpdateCustomerCount();
        UpdateUI();
    }


    private void Update()
    {
        if (isGameFinished)
        {
            return;
        }

        // ==========================================
        // ゲーム開始前
        // ==========================================

        if (!isGameStarted)
        {
            // まだカウントダウンが始まっていない
            if (!isCountdownStarted &&
                Keyboard.current != null)
            {
                bool startButtonPressed =
                    Keyboard.current.spaceKey.wasPressedThisFrame ||
                    Keyboard.current.aKey.wasPressedThisFrame ||
                    Keyboard.current.bKey.wasPressedThisFrame ||
                    Keyboard.current.cKey.wasPressedThisFrame;

                if (startButtonPressed)
                {
                    isCountdownStarted = true;

                    StartCoroutine(
                        StartCountdown()
                    );
                }
            }

            // ゲーム開始前なので
            // 制限時間や売上処理は行わない
            return;
        }


        // ==========================================
        // ここからゲーム中
        // ==========================================

        UpdateGameTimer();

        UpdateCustomerCount();


        // ゲーム終了したら
        // そのフレームの処理も終了
        if (isGameFinished)
        {
            return;
        }


        UpdateSales();

        UpdateUI();
    }


    // ==========================================================
    // ゲーム開始
    // ==========================================================

    /// <summary>
    /// ゲーム開始カウントダウン
    /// </summary>
    private IEnumerator StartCountdown()
    {
        if (countdownText != null)
        {
            countdownText.text = "3";
        }

        yield return new WaitForSeconds(1.0f);


        if (countdownText != null)
        {
            countdownText.text = "2";
        }

        yield return new WaitForSeconds(1.0f);


        if (countdownText != null)
        {
            countdownText.text = "1";
        }

        yield return new WaitForSeconds(1.0f);


        if (countdownText != null)
        {
            countdownText.text = "START!";
        }

        yield return new WaitForSeconds(1.0f);


        // カウントダウンUIを消す
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }


        // ゲーム開始
        isGameStarted = true;

        Debug.Log("ゲーム開始！");
    }


    // ==========================================================
    // 制限時間
    // ==========================================================

    /// <summary>
    /// 制限時間を減らす
    /// </summary>
    private void UpdateGameTimer()
    {
        gameTime = Mathf.Max(0.0f, gameTime - Time.deltaTime);

        UpdateTimeDisplay();

        if (gameTime <= 0.0f)
        {
            FinishGame();
        }
    }

    /// <summary>
    /// 右上の残り時間と、中央のカウントダウンを更新
    /// </summary>
    private void UpdateTimeDisplay()
    {
        int totalSeconds =
            Mathf.CeilToInt(Mathf.Max(0.0f, gameTime));

        // 右上の残り時間
        if (timeText != null)
        {
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            timeText.text =
                $"残り時間 {minutes:00}:{seconds:00}";

            timeText.color = gameTime <= warningTime
                ? warningTimeColor
                : normalTimeColor;
        }

        // 開始演出中・終了演出中は中央の文字を書き換えない
        if (!isGameStarted || isGameFinished || countdownText == null)
        {
            return;
        }

        bool showCountdown = gameTime <= finalCountdownTime;

        countdownText.gameObject.SetActive(showCountdown);

        if (showCountdown)
        {
            countdownText.text = totalSeconds.ToString();
            countdownText.color = warningTimeColor;
        }
    }
    // ==========================================================
    // お客さん人数
    // ==========================================================

    /// <summary>
    /// 現在存在しているお客さんの人数を取得
    /// </summary>
    private void UpdateCustomerCount()
    {
        Customer[] customers =
           FindObjectsByType<Customer>(FindObjectsInactive.Exclude);

        currentCustomerCount =
            customers.Length;
    }


    // ==========================================================
    // 売上
    // ==========================================================

    /// <summary>
    /// 一定時間ごとに売上を増やす
    /// </summary>
    private void UpdateSales()
    {
        salesTimer -= Time.deltaTime;

        if (salesTimer > 0.0f)
        {
            return;
        }


        // ランダムで売上を増やす
        int increase =
            Random.Range(
                minSalesIncrease,
                maxSalesIncrease + 1
            );

        sales += increase;


        // タイマーをリセット
        salesTimer = salesIncreaseInterval;
    }


    // ==========================================================
    // UI
    // ==========================================================

    /// <summary>
    /// UI表示更新
    /// </summary>
    private void UpdateUI()
    {
        // -------------------------
        // 時間
        // -------------------------

        UpdateTimeDisplay();


        // -------------------------
        // お客さん人数
        // -------------------------

        if (customerCountText != null)
        {
            customerCountText.text =
                "残り人数:" +
                currentCustomerCount;
        }
        if (thiefCountText != null)
        {
            thiefCountText.text =
               "泥棒 残り：" + (CustomerManager.GetThiefCount()-caughtThiefCount) + "人";
        }

        // -------------------------
        // 売上
        // -------------------------

        if (salesText != null)
        {
            salesText.text =
                "￥売上金:" +
                sales.ToString("N0");
        }
    }


    // ==========================================================
    // お客さん関係
    // ==========================================================

    /// <summary>
    /// お客さんが店に入った
    /// </summary>
    public void CustomerEntered()
    {
        if (isGameFinished)
        {
            return;
        }
        currentCustomerCount++;

        Debug.Log(
            "お客さん入店：" +
            currentCustomerCount +
            "人"
        );

        UpdateUI();
    }


    /// <summary>
    /// お客さんが店からいなくなった
    /// </summary>
    public void CustomerExited()
    {
        if (isGameFinished)
        {
            return;
        }
        currentCustomerCount--;

        if (currentCustomerCount < 0)
        {
            currentCustomerCount = 0;
        }

        Debug.Log(
            "現在のお客さん：" +
            currentCustomerCount +
            "人"
        );

        UpdateUI();
    }


    // ==========================================================
    // 泥棒関係
    // ==========================================================

    /// <summary>
    /// 捕まえた
    /// </summary>
    public void Caught()
    {
        if (isGameFinished)
        {
            return;
        }
        caughtCount++;

        Debug.Log(
            "捕まえた人数：" +
            caughtCount
        );
    }


    /// <summary>
    /// 泥棒を捕まえた
    /// </summary>
    public void ThiefCaught()
    {
        if (isGameFinished)
        {
            return;
        }
        caughtThiefCount++;

        CustomerExited();

        StartCoroutine(
       ShowCaptureResult("泥棒だった！")
   );

        if (caughtThiefCount ==
            CustomerManager.GetThiefCount())
        {
            FinishGame();
        }


        Debug.Log(
            "捕まえた人数：" +
            caughtCount
        );
    }


    /// <summary>
    /// 泥棒を逃がした
    /// </summary>
    public void ThiefEscaped()
    {
        if (isGameFinished)
        {
            return;
        }

        escapedThiefCount++;

        CustomerExited();

        Debug.Log(
            "泥棒に逃げられました。" +
            " 逃がした人数：" +
            escapedThiefCount
        );
    }

    private IEnumerator ShowCaptureResult(string message)
    {
        if (captureResultText == null)
        {
            yield break;
        }

        captureResultText.gameObject.SetActive(true);
        captureResultText.text = message;

        yield return new WaitForSeconds(captureResultDisplayTime);

        captureResultText.gameObject.SetActive(false);
    }
    // ==========================================================
    // クレーム関係
    // ==========================================================

    /// <summary>
    /// 一般客を間違えて捕まえた
    /// </summary>
    public void AddComplaint()
    {
        if (isGameFinished)
        {
            return;
        }
        complaintCount++;

        // 売上金を減らす
        sales -= wrongArrestSalesPenalty;

        // 売上金がマイナスにならないようにする
        if (sales < 0)
        {
            sales = 0;
        }

        StartCoroutine(
       ShowCaptureResult("一般客だったようだ。。。")
   );

        Debug.Log(
            "クレーム発生！" +
            " クレーム数：" +
            complaintCount
        );
    }


    // ==========================================================
    // 売上
    // ==========================================================

    /// <summary>
    /// 任意の金額を売上に追加
    /// </summary>
    public void AddSales(int amount)
    {
        if (isGameFinished)
        {
            return;
        }
        if (amount <= 0)
        {
            return;
        }

        sales += amount;

        UpdateUI();
    }


    // ==========================================================
    // 時間ペナルティ
    // ==========================================================

    /// <summary>
    /// 誤認逮捕などで制限時間を減らす
    /// </summary>
    public void AddTimePenalty()
    {
        if (isGameFinished)
        {
            return;
        }
        gameTime -= wrongArrestTimePenalty;

        if (gameTime < 0.0f)
        {
            gameTime = 0.0f;
        }

        Debug.Log(
            "誤認逮捕ペナルティ！ -" +
            wrongArrestTimePenalty +
            "秒"
        );

        UpdateUI();


        if (gameTime <= 0.0f)
        {
            FinishGame();
        }
    }


    // ==========================================================
    // ゲーム終了
    // ==========================================================

    /// <summary>
    /// ゲーム終了
    /// </summary>
    private void FinishGame()
    {
        if (isGameFinished)
        {
            return;
        }

        // 終了フラグを先に立てて、重複実行を防ぐ
        isGameFinished = true;
        isGameStarted = false;

        escapedThiefCount =
            CustomerManager.GetThiefCount() - caughtThiefCount;

        // 終了時点の表示に更新
        UpdateUI();

        StartCoroutine(FinishGameCoroutine());
    }

    private IEnumerator FinishGameCoroutine()
    {
        // 終了時点の結果を保存
        GameResultData.caughtCount = caughtCount;
        GameResultData.escapedThiefCount = escapedThiefCount;
        GameResultData.complaintCount = complaintCount;
        GameResultData.sales = sales;

        // ゲーム内の移動・アニメーションなどを停止
        Time.timeScale = 0.0f;

        // 音声入力を停止
        if (voiceRecognizer != null)
        {
            voiceRecognizer.enabled = false;
        }

        // 捕獲結果の文字を隠す
        if (captureResultText != null)
        {
            captureResultText.gameObject.SetActive(false);
        }

        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);
            countdownText.transform.SetAsLastSibling();

            // 時間切れの場合だけ「0」を表示
            if (gameTime <= 0.0f)
            {
                countdownText.color = warningTimeColor;
                countdownText.text = "0";

                yield return new WaitForSecondsRealtime(
                    zeroDisplayTime
                );
            }

            countdownText.color = normalTimeColor;
            countdownText.text = "終了！";
        }

        // timeScaleが0でも待機できる
        yield return new WaitForSecondsRealtime(
            finishDisplayTime
        );

        // 次のシーンに向けて時間を戻す
        Time.timeScale = 1.0f;

        SceneManager.LoadScene(resultSceneName);
    }

    // ==========================================================
    // Getter
    // ==========================================================

    public int GetCurrentCustomerCount()
    {
        return currentCustomerCount;
    }


    public int GetCaughtCount()
    {
        return caughtCount;
    }


    public int GetEscapedThiefCount()
    {
        return escapedThiefCount;
    }


    public int GetComplaintCount()
    {
        return complaintCount;
    }


    public int GetSales()
    {
        return sales;
    }


    public float GetRemainingTime()
    {
        return gameTime;
    }


    /// <summary>
    /// ゲームが開始しているか
    /// </summary>
    public bool IsGameStarted()
    {
        return isGameStarted;
    }
}


/// <summary>
/// PlaySceneからResultSceneへ
/// データを受け渡すためのクラス
/// </summary>
public static class GameResultData
{
    public static int caughtCount;

    public static int escapedThiefCount;

    public static int complaintCount;

    public static int sales;
}