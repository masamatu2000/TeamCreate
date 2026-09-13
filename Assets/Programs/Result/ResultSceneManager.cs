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

    // ★追加：前のシーンからの押しっぱなしを防ぐためのフラグ
    private bool hasReleasedAllKeys = false;

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

        // 初期状態は「キーが離されていない」判定にする
        hasReleasedAllKeys = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // ========================================
        // 1. まず、プレイシーンからの押しっぱなしが離されたかチェック
        // ========================================
        if (!hasReleasedAllKeys)
        {
            bool isAnyKeyPressed =
                keyboard.spaceKey.isPressed ||
                keyboard.aKey.isPressed ||
                keyboard.bKey.isPressed ||
                keyboard.cKey.isPressed;

            // まだどれか押しっぱなしなら、ここで処理を止めてキーが離されるのを待つ
            if (isAnyKeyPressed)
            {
                return;
            }
            else
            {
                // すべてのキーが離された！ここから入力を有効化する
                hasReleasedAllKeys = true;
            }
        }

        // ========================================
        // 2. キーが離されたあとの通常のボタン判定
        // ========================================
        bool isAnyButtonPressed =
            keyboard.spaceKey.wasPressedThisFrame || // wasPressedThisFrameに変更するとより安全です
            keyboard.aKey.wasPressedThisFrame ||
            keyboard.bKey.wasPressedThisFrame ||
            keyboard.cKey.wasPressedThisFrame;

        if (isAnyButtonPressed)
        {
            ReturnTitle();
        }
    }

    /// <summary>
    /// リザルトを表示
    /// </summary>
    private void ShowResult()
    {
        capturedText.text =
            "捕まえた人数：" + GameResultData.caughtCount;

        escapedText.text =
            "逃がした泥棒の数：" + GameResultData.escapedThiefCount;

        complaintText.text =
            "間違えた数：" + GameResultData.complaintCount;

        moneyText.text =
            "売上金：" + GameResultData.sales.ToString("N0");
    }

    /// <summary>
    /// タイトルへ戻る
    /// </summary>
    private void ReturnTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }
}