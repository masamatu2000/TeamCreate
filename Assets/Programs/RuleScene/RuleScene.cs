using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialSceneManager : MonoBehaviour
{
    public enum TutorialState
    {
        PoliceIntroduction,
        CustomerIntroduction,
        CornerMove,
        Arrest,
        Clear
    }

    [Header("現在のチュートリアル状態")]
    [SerializeField]
    private TutorialState currentState =
        TutorialState.PoliceIntroduction;


    // =========================================================
    // Camera
    // =========================================================

    [Header("Camera")]
    [SerializeField]
    private Camera tutorialCamera;

    [SerializeField]
    private Transform normalCameraPosition;

    [SerializeField]
    private Transform policeCloseUpPosition;

    [SerializeField]
    private Transform customerCloseUpPosition;

    [SerializeField]
    private float cameraMoveTime = 1.0f;
    [Header("Customer Camera")]
    [SerializeField]
    private float customerCameraHeight = 3.0f;

    [SerializeField]
    private float customerCameraDistance = 6.0f;

    // =========================================================
    // Character
    // =========================================================

    [Header("Characters")]
    [SerializeField]
    private GameObject police;

    [SerializeField]
    private TutorialCustomer normalCustomer;

    [SerializeField]
    private TutorialCustomer thiefCustomer;


    // =========================================================
    // Corner
    // =========================================================

    [Header("Corners")]
    [SerializeField]
    private Transform[] corners;


    // =========================================================
    // UI
    // =========================================================

    [Header("Mission UI")]
    [SerializeField]
    private GameObject missionPanel;

   

    [SerializeField]
    private TMP_Text missionDescriptionText;


    [SerializeField]
    private TMP_Text operationGuideText;


    // =========================================================
    // その他
    // =========================================================

    private bool isCameraMoving = false;

    private Transform thiefCorner;


    private void Start()
    {
        InitializeTutorial();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
        {
            return;
        }

        // チュートリアル進行
        if (keyboard.aKey.wasPressedThisFrame)
        {
            OnHardwareButtonPressed();
        }

        // 音声認識フェーズ中だけ
        if (currentState == TutorialState.CornerMove ||
            currentState == TutorialState.Arrest)
        {
            if (keyboard.spaceKey.isPressed)
            {
                operationGuideText.text =
                    "マイクに話しかけよう！";
            }
            else
            {
                if (currentState == TutorialState.CornerMove)
                {
                    operationGuideText.text =
                        "スペースキーを押しながら話そう！";
                }
                else if (currentState == TutorialState.Arrest)
                {
                    operationGuideText.text =
                        "スペースキーを押しながら「捕まえろ！」と言おう！";
                }
            }
        }
    }
    // =========================================================
    // 初期化
    // =========================================================

    private void InitializeTutorial()
    {
        // --------------------------
        // 泥棒を設定
        // --------------------------

        normalCustomer.SetThief(false);

        thiefCustomer.SetThief(true);


        // --------------------------
        // 必ずコーナーを設定
        // --------------------------

        SetCustomerCorners();


        // --------------------------
        // 最初のチュートリアル
        // --------------------------

        currentState =
            TutorialState.PoliceIntroduction;


        StartCoroutine(
            StartPoliceIntroduction()
        );
    }


    // =========================================================
    // 客を必ずコーナーへ移動させる
    // =========================================================

    private void SetCustomerCorners()
    {
        if (corners == null ||
            corners.Length < 2)
        {
            Debug.LogError(
                "TutorialSceneManager：" +
                "コーナーを2つ以上設定してください"
            );

            return;
        }


        // =====================================================
        // 泥棒のコーナーをランダム決定
        // =====================================================

        int thiefCornerIndex =
            Random.Range(
                0,
                corners.Length
            );


        thiefCorner =
            corners[thiefCornerIndex];


        thiefCustomer.SetCorner(
            thiefCorner
        );


        // =====================================================
        // 一般客は別のコーナーへ
        // =====================================================

        int normalCornerIndex;


        do
        {
            normalCornerIndex =
                Random.Range(
                    0,
                    corners.Length
                );

        }
        while (
            normalCornerIndex ==
            thiefCornerIndex
        );


        normalCustomer.SetCorner(
            corners[
                normalCornerIndex
            ]
        );
    }


    // =========================================================
    // STEP 1
    // 警備員紹介
    // =========================================================

    private IEnumerator StartPoliceIntroduction()
    {
        missionPanel.SetActive(true);

        
        missionDescriptionText.gameObject.SetActive(true);

        // 最初は操作案内を隠す
        operationGuideText.gameObject.SetActive(false);

      

        missionDescriptionText.text =
            "こちらが警備員！\n" +
            "あなたの指示に従って店内を移動します。";

        yield return StartCoroutine(
            MoveCamera(
                policeCloseUpPosition
            )
        );

        // カメラ移動後に表示
        operationGuideText.text =
            "Aボタンで次へ";

        operationGuideText.gameObject.SetActive(true);
    }


    // =========================================================
    // STEP1 → STEP2
    // =========================================================

    private IEnumerator MoveToCustomerIntroduction()
    {
        operationGuideText.gameObject.SetActive(false);

        // 一旦通常カメラへ戻る
        yield return StartCoroutine(
            MoveCamera(
                normalCameraPosition
            )
        );

        currentState =
            TutorialState.CustomerIntroduction;

        missionDescriptionText.text =
            "どちらかが泥棒！\n" +
            "きょろきょろしている、\n\n" +
            "しゃがみこんでいる人は怪しいぞ！";

        // 一般客と泥棒の2人が見える位置へ移動
        yield return StartCoroutine(
            MoveCameraToCustomers()
        );

        operationGuideText.text =
            "Aボタンで次へ";

        operationGuideText.gameObject.SetActive(true);
    }
    private IEnumerator MoveCameraToCustomers()
    {
        if (tutorialCamera == null ||
            normalCustomer == null ||
            thiefCustomer == null)
        {
            yield break;
        }


        isCameraMoving = true;


        // =====================================================
        // 一般客と泥棒の中央位置
        // =====================================================

        Vector3 customerCenter =
            (
                normalCustomer.transform.position +
                thiefCustomer.transform.position
            )
            / 2.0f;


        // =====================================================
        // カメラ位置
        // =====================================================

        Vector3 targetPosition =
            customerCenter +
            new Vector3(
                0.0f,
                customerCameraHeight,
                -customerCameraDistance
            );


        // =====================================================
        // カメラが見る位置
        // 少し上を見ることでキャラクターの上半身を映す
        // =====================================================

        Vector3 lookTarget =
            customerCenter +
            Vector3.up * 1.5f;


        Quaternion targetRotation =
            Quaternion.LookRotation(
                lookTarget -
                targetPosition
            );


        // =====================================================
        // 現在位置
        // =====================================================

        Vector3 startPosition =
            tutorialCamera
                .transform
                .position;


        Quaternion startRotation =
            tutorialCamera
                .transform
                .rotation;


        float timer =
            0.0f;


        // =====================================================
        // カメラ移動
        // =====================================================

        while (
            timer <
            cameraMoveTime
        )
        {
            timer +=
                Time.deltaTime;


            float t =
                timer /
                cameraMoveTime;


            t =
                Mathf.SmoothStep(
                    0.0f,
                    1.0f,
                    t
                );


            tutorialCamera
                .transform
                .position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t
                    );


            tutorialCamera
                .transform
                .rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        t
                    );


            yield return null;
        }


        tutorialCamera
            .transform
            .position =
                targetPosition;


        tutorialCamera
            .transform
            .rotation =
                targetRotation;


        isCameraMoving =
            false;
    }
    // =========================================================
    // STEP2 → STEP3
    // =========================================================

    private IEnumerator MoveToVoiceTutorial()
    {
        yield return StartCoroutine(
            MoveCamera(
                normalCameraPosition
            )
        );

        currentState =
            TutorialState.CornerMove;

       

        string cornerName =
            GetCornerDisplayName(
                thiefCorner
            );

        missionDescriptionText.text =
            "ボタンを押しながら\n" +
            $"「{cornerName}」\n" +
            "と言ってみよう！";

        operationGuideText.text =
            "音声ボタンを押しながら話そう！";
    }


    // =========================================================
    // 音声認識成功
    // =========================================================

    public void OnCornerVoiceRecognized()
    {
        if (currentState !=
            TutorialState.CornerMove)
        {
            return;
        }


        currentState =
            TutorialState.Arrest;


     

        missionDescriptionText.text =
            "警備員が移動を開始した！\n" +
            "泥棒に近づいたら\n" +
            "「捕まえろ！」と言ってみよう！";


        // ================================================
        // 警備員を泥棒のコーナーへ
        // ================================================

        TutorialPoliceController
            policeController =
                police.GetComponent<
                    TutorialPoliceController
                >();


        if (policeController != null)
        {
            policeController.MoveTo(
                thiefCorner.position
            );
        }
    }


    // =========================================================
    // 捕獲成功
    // =========================================================

    //public void OnArrestSuccess()
    //{
    //    if (currentState !=
    //        TutorialState.Arrest)
    //    {
    //        return;
    //    }


    //    currentState =
    //        TutorialState.Clear;


    //    thiefCustomer.Arrest();


    //    missionTitleText.text =
    //        "確保成功！";


    //    missionDescriptionText.text =
    //        "見事、泥棒を捕まえました！\n\n" +
    //        "これで基本操作は完璧です。\n" +
    //        "本番では怪しい行動を見逃さず、\n" +
    //        "警備員に的確な指示を出そう！";


    //    nextButton.gameObject.SetActive(
    //        false
    //    );
    //}


    // =========================================================
    // カメラ移動
    // =========================================================

    private IEnumerator MoveCamera(
        Transform target
    )
    {
        if (target == null ||
            tutorialCamera == null)
        {
            yield break;
        }


        isCameraMoving =
            true;


        Vector3 startPosition =
            tutorialCamera
                .transform
                .position;


        Quaternion startRotation =
            tutorialCamera
                .transform
                .rotation;


        float timer =
            0.0f;


        while (
            timer <
            cameraMoveTime
        )
        {
            timer +=
                Time.deltaTime;


            float t =
                timer /
                cameraMoveTime;


            t =
                Mathf.SmoothStep(
                    0.0f,
                    1.0f,
                    t
                );


            tutorialCamera
                .transform
                .position =
                    Vector3.Lerp(
                        startPosition,
                        target.position,
                        t
                    );


            tutorialCamera
                .transform
                .rotation =
                    Quaternion.Slerp(
                        startRotation,
                        target.rotation,
                        t
                    );


            yield return null;
        }


        tutorialCamera
            .transform
            .position =
                target.position;


        tutorialCamera
            .transform
            .rotation =
                target.rotation;


        isCameraMoving =
            false;
    }


    // =========================================================
    // コーナー表示名
    // =========================================================

    private string GetCornerDisplayName(
        Transform corner
    )
    {
        if (corner == null)
        {
            return "コーナー";
        }


        // 必要に応じて追加
        switch (corner.name)
        {
            case "FishCouner":
                return "鮮魚コーナー";

            case "DrinkCorner":
                return "飲料コーナー";

            case "SnackCorner":
                return "お菓子コーナー";
            case "PreparedFoodCouner":
                return "惣菜コーナー";
            default:
                return corner.name;
        }
    }
    private void OnHardwareButtonPressed()
    {
        if (isCameraMoving)
        {
            return;
        }

        switch (currentState)
        {
            case TutorialState.PoliceIntroduction:
                StartCoroutine(
                    MoveToCustomerIntroduction()
                );
                break;

            case TutorialState.CustomerIntroduction:
                StartCoroutine(
                    MoveToVoiceTutorial()
                );
                break;
        }
    }
}