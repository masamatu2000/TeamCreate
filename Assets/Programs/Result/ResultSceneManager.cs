using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// リザルト画面を管理するクラス
/// </summary>
public class ResultSceneManager : MonoBehaviour
{
    [Header("リザルト表示")]
    [SerializeField] private TMP_Text capturedText;
    [SerializeField] private TMP_Text escapedText;
    [SerializeField] private TMP_Text complaintText;
    [SerializeField] private TMP_Text moneyText;

    [Header("タイトルへ戻る設定")]
    [SerializeField] private float returnTime = 10.0f;
    
    [Header("誤認逮捕ペナルティ")]
    [SerializeField]
    private float wrongArrestTimePenalty = 10.0f;
    private float timer;

    private void Start()
    {
        Debug.Log(
        "★ ResultScene：Start開始 " +
        Time.realtimeSinceStartup
    );

        ShowResult();

        Debug.Log(
            "★ ResultScene：Start終了 " +
            Time.realtimeSinceStartup
        );

        timer = returnTime;
    }

    private void Update()
    {
        // 時間を減らす
        timer -= Time.deltaTime;

        // スペースを押した、または10秒経過したらタイトルへ
        if ((Keyboard.current != null &&
             Keyboard.current.spaceKey.wasPressedThisFrame)
            || timer <= 0.0f)
        {
            ReturnTitle();
        }
    }

    /// <summary>
    /// リザルトを表示
    /// </summary>
    private void ShowResult()
    {
        // ↓ここは既存のGameResultDataに合わせて変更
        capturedText.text =
            "Capture：" + GameResultData.caughtCount;

        escapedText.text =
            "Escaped：" + GameResultData.escapedThiefCount ;

        complaintText.text =
            "Complain：" + GameResultData.complaintCount;

        moneyText.text =
            "Sales：" + GameResultData.sales.ToString("N0");
    }


    /// <summary>
    /// タイトルへ戻る
    /// </summary>
    private void ReturnTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}