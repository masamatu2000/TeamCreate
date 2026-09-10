using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// ルール説明画面を管理する
///
/// ・スペース短押し：次のページ
/// ・スペース長押し：前のページ
/// ・最終ページで短押し：確認画面
/// ・確認画面で短押し：PlaySceneへ
/// ・確認画面で長押し：ルール画面へ戻る
/// </summary>
public class RuleSceneManager : MonoBehaviour
{
    [Header("ルール画像")]
    [SerializeField]
    private Image ruleImage;

    [SerializeField]
    private Sprite[] ruleSprites;


    [Header("確認画面")]
    [SerializeField]
    private GameObject confirmPanel;


    [Header("長押し判定")]
    [SerializeField]
    private float longPressTime = 0.6f;


    [Header("遷移先")]
    [SerializeField]
    private string playSceneName = "PlayScene";


    // 現在表示しているページ
    private int currentPage = 0;

    // スペースを押している時間
    private float spacePressTimer = 0.0f;

    // スペースを押しているか
    private bool isSpacePressed = false;

    // 長押し処理済みか
    private bool longPressExecuted = false;

    // 確認画面を表示しているか
    private bool isConfirming = false;


    private void Start()
    {
        // ============================
        // 最初のルール画像を表示
        // ============================

        currentPage = 0;

        ShowCurrentPage();


        // ============================
        // 確認画面を非表示
        // ============================

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }


    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }


        // ========================================
        // どれかのボタンが押されているか
        // ========================================

        bool isAnyButtonPressed =
            keyboard.spaceKey.isPressed ||
            keyboard.aKey.isPressed ||
            keyboard.bKey.isPressed ||
            keyboard.cKey.isPressed;


        // ========================================
        // ボタンを押した瞬間
        // ========================================

        if (isAnyButtonPressed &&
            !isSpacePressed)
        {
            isSpacePressed = true;

            spacePressTimer = 0.0f;

            longPressExecuted = false;
        }


        // ========================================
        // ボタンの長押し判定
        // ========================================

        if (isSpacePressed &&
            isAnyButtonPressed)
        {
            spacePressTimer +=
                Time.unscaledDeltaTime;

            if (spacePressTimer >= longPressTime &&
                !longPressExecuted)
            {
                longPressExecuted = true;

                OnLongPress();
            }
        }


        // ========================================
        // すべてのボタンを離した瞬間
        // ========================================

        if (!isAnyButtonPressed &&
            isSpacePressed)
        {
            // 長押しが発動していない場合だけ
            // 短押しとして扱う
            if (!longPressExecuted)
            {
                OnShortPress();
            }

            isSpacePressed = false;

            spacePressTimer = 0.0f;

            longPressExecuted = false;
        }
    }


    /// <summary>
    /// スペース短押し
    /// </summary>
    private void OnShortPress()
    {
        // ============================
        // 確認画面の場合
        // ============================

        if (isConfirming)
        {
            StartGame();

            return;
        }


        // ============================
        // 最後のページの場合
        // ============================

        if (currentPage >= ruleSprites.Length - 1)
        {
            ShowConfirmPanel();

            return;
        }


        // ============================
        // 次のページへ
        // ============================

        currentPage++;

        ShowCurrentPage();
    }


    /// <summary>
    /// スペース長押し
    /// </summary>
    private void OnLongPress()
    {
        // ============================
        // 確認画面の場合
        // ============================

        if (isConfirming)
        {
            HideConfirmPanel();

            return;
        }


        // ============================
        // 最初のページなら戻らない
        // ============================

        if (currentPage <= 0)
        {
            return;
        }


        // ============================
        // 前のページへ
        // ============================

        currentPage--;

        ShowCurrentPage();
    }


    /// <summary>
    /// 現在のルール画像を表示
    /// </summary>
    private void ShowCurrentPage()
    {
        if (ruleImage == null)
        {
            Debug.LogWarning(
                "RuleImageが設定されていません"
            );

            return;
        }


        if (ruleSprites == null ||
            ruleSprites.Length == 0)
        {
            Debug.LogWarning(
                "ルール画像が設定されていません"
            );

            return;
        }


        ruleImage.sprite =
            ruleSprites[currentPage];


        Debug.Log(
            $"ルールページ：{currentPage + 1}" +
            $" / {ruleSprites.Length}"
        );
    }


    /// <summary>
    /// 確認画面を表示
    /// </summary>
    private void ShowConfirmPanel()
    {
        isConfirming = true;

        // スライドを隠す
        if (ruleImage != null)
        {
            ruleImage.gameObject.SetActive(false);
        }

        // 黒背景付き確認画面を表示
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }

        Debug.Log(
            "ゲーム開始確認画面を表示"
        );
    }


    /// <summary>
    /// 確認画面を閉じる
    /// </summary>
    private void HideConfirmPanel()
    {
        isConfirming = false;

        // 確認画面を消す
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }

        // スライドを再表示
        if (ruleImage != null)
        {
            ruleImage.gameObject.SetActive(true);
        }

        // 念のため現在ページを再表示
        ShowCurrentPage();

        Debug.Log(
            "確認画面を閉じてルール画面に戻りました"
        );
    }


    /// <summary>
    /// PlaySceneへ移動
    /// </summary>
    private void StartGame()
    {
        Debug.Log(
            "PlaySceneへ移動します"
        );


        SceneManager.LoadScene(
            playSceneName
        );
    }
}