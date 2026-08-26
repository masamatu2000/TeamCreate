using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// PlayScene全体のゲーム進行を管理するクラス
/// 
/// 管理するもの
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
    [Header("ゲーム時間")]
    [SerializeField]
    private float gameTime = 180.0f;

    [Header("誤認逮捕ペナルティ")]
    [SerializeField]
    private float wrongArrestTimePenalty = 50.0f;

    [Header("UI")]
    [SerializeField]
    private TextMeshProUGUI timeText;

    [SerializeField]
    private TextMeshProUGUI customerCountText;

    [SerializeField]
    private TextMeshProUGUI salesText;


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
        // 初期化
        salesTimer = salesIncreaseInterval;

        UpdateUI();
    }


    private void Update()
    {
        if (isGameFinished)
        {
            return;
        }

        UpdateGameTimer();
        Customer[] customers =
       FindObjectsByType<Customer>(
           FindObjectsSortMode.None
       );

        currentCustomerCount =
            customers.Length;
        // ゲーム終了したら、そのフレームの処理もここで終了
        if (isGameFinished)
        {
            return;
        }

        UpdateSales();
        UpdateUI();
    }


    /// <summary>
    /// 制限時間を減らす
    /// </summary>
    private void UpdateGameTimer()
    {
        gameTime -= Time.deltaTime;

        if (gameTime <= 0.0f)
        {
            gameTime = 0.0f;

            FinishGame();
        }
    }


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


        //Debug.Log(
        //    "売上 +" +
        //    increase +
        //    "円"
        //);


        // タイマーをリセット
        salesTimer = salesIncreaseInterval;
    }


    /// <summary>
    /// UI表示更新
    /// </summary>
    private void UpdateUI()
    {
        // -------------------------
        // 時間
        // -------------------------

        if (timeText != null)
        {
            int totalSeconds =
                Mathf.CeilToInt(gameTime);

            int minutes =
                totalSeconds / 60;

            int seconds =
                totalSeconds % 60;

            timeText.text =
                $"{minutes:00}:{seconds:00}";
        }


        // -------------------------
        // お客さん人数
        // -------------------------

        if (customerCountText != null)
        {
            customerCountText.text =
                "Costomer:" +
                currentCustomerCount;
        }


        // -------------------------
        // 売上
        // -------------------------

        if (salesText != null)
        {
            salesText.text =
                "Sales:" +
                sales.ToString("N0") ;
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
    /// 泥棒を捕まえた
    /// </summary>
    public void Caught()
    {
        caughtCount++;

        CustomerExited();

        Debug.Log(
            " 捕まえた人数：" +
            caughtCount
        );
    }


    /// <summary>
    /// 泥棒を逃がした
    /// </summary>
    public void ThiefEscaped()
    {
        escapedThiefCount++;

        CustomerExited();

        Debug.Log(
            "泥棒に逃げられました。" +
            " 逃がした人数：" +
            escapedThiefCount
        );
    }


    // ==========================================================
    // クレーム関係
    // ==========================================================


    /// <summary>
    /// 一般客を間違えて捕まえた
    /// </summary>
    public void AddComplaint()
    {
        complaintCount++;

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
        if (amount <= 0)
        {
            return;
        }

        sales += amount;

        UpdateUI();
    }

    /// <summary>
    /// 誤認逮捕などで制限時間を減らす
    /// </summary>
    public void AddTimePenalty()
    {
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

        // ペナルティで0秒になった場合もゲーム終了
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

        isGameFinished = true;


        Debug.Log(
            "ゲーム終了！" +
            "\n捕まえた泥棒：" +
            caughtCount +
            "\n逃がした泥棒：" +
            escapedThiefCount +
            "\nクレーム：" +
            complaintCount +
            "\n売上：" +
            sales
        );


        // リザルト画面へ渡す
        GameResultData.caughtCount =
            caughtCount;

        GameResultData.escapedThiefCount =
            escapedThiefCount;

        GameResultData.complaintCount =
            complaintCount;

        GameResultData.sales =
            sales;


        // ResultSceneへ
        Debug.Log(
     "★ FinishGame：ResultScene読み込み直前 " +
     Time.realtimeSinceStartup
 );

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