using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 監視カメラに発生するハプニングを管理する
///
/// ・一定時間ごとにランダムでハプニング発生
/// ・ノイズ演出
/// ・クモが画面を横切る演出
/// </summary>
public class CameraHappeningController : MonoBehaviour
{
    [Header("ハプニング発生間隔")]
    [SerializeField]
    private float minHappeningInterval = 15.0f;

    [SerializeField]
    private float maxHappeningInterval = 30.0f;


    [Header("ノイズ")]
    [SerializeField]
    private GameObject noiseObject;

    [SerializeField]
    private Image noiseImage;

    [SerializeField]
    private Sprite[] noiseSprites;

    [SerializeField]
    private float noiseDuration = 2.0f;

    [SerializeField]
    private float noiseChangeInterval = 0.05f;


    [Header("クモ")]
    [SerializeField]
    private GameObject spiderObject;

    [SerializeField]
    private RectTransform spiderRect;

    [SerializeField]
    private RectTransform canvasRect;

    [SerializeField]
    private float spiderMoveDuration = 3.0f;


    // 現在ハプニング中かどうか
    private bool isHappening = false;


    private void Start()
    {
        // 最初は非表示
        if (noiseObject != null)
        {
            noiseObject.SetActive(false);
        }

        if (spiderObject != null)
        {
            spiderObject.SetActive(false);
        }

        StartCoroutine(
            HappeningLoop()
        );
    }


    /// <summary>
    /// ハプニングを一定間隔で発生させる
    /// </summary>
    private IEnumerator HappeningLoop()
    {
        while (true)
        {
            float waitTime =
                Random.Range(
                    minHappeningInterval,
                    maxHappeningInterval
                );

            yield return new WaitForSeconds(
                waitTime
            );


            if (isHappening)
            {
                continue;
            }


            int randomHappening =
                Random.Range(
                    0,
                    2
                );


            switch (randomHappening)
            {
                case 0:

                    yield return StartCoroutine(
                        NoiseHappening()
                    );

                    break;


                case 1:

                    yield return StartCoroutine(
                        SpiderHappening()
                    );

                    break;
            }
        }
    }


    /// <summary>
    /// ノイズ演出
    /// </summary>
    private IEnumerator NoiseHappening()
    {
        if (noiseObject == null ||
            noiseImage == null ||
            noiseSprites == null ||
            noiseSprites.Length == 0)
        {
            yield break;
        }


        isHappening = true;

        noiseObject.SetActive(
            true
        );


        float timer = 0.0f;


        while (timer < noiseDuration)
        {
            int randomIndex =
                Random.Range(
                    0,
                    noiseSprites.Length
                );


            noiseImage.sprite =
                noiseSprites[randomIndex];


            // 少し透明度もランダムにする
            Color color =
                noiseImage.color;

            color.a =
                Random.Range(
                    0.5f,
                    1.0f
                );

            noiseImage.color =
                color;


            yield return new WaitForSeconds(
                noiseChangeInterval
            );


            timer +=
                noiseChangeInterval;
        }


        noiseObject.SetActive(
            false
        );

        isHappening = false;
    }


    /// <summary>
    /// クモが画面を横切る演出
    /// </summary>
    private IEnumerator SpiderHappening()
    {
        if (spiderObject == null ||
            spiderRect == null ||
            canvasRect == null)
        {
            yield break;
        }


        isHappening = true;

        spiderObject.SetActive(
            true
        );


        Vector2 startPosition;
        Vector2 endPosition;


        int direction =
            Random.Range(
                0,
                4
            );


        float halfWidth =
            canvasRect.rect.width / 2.0f;

        float halfHeight =
            canvasRect.rect.height / 2.0f;


        float randomX =
            Random.Range(
                -halfWidth,
                halfWidth
            );

        float randomY =
            Random.Range(
                -halfHeight,
                halfHeight
            );


        // どこからどこへ移動するか
        switch (direction)
        {
            // 左 → 右
            case 0:

                startPosition =
                    new Vector2(
                        -halfWidth - 100.0f,
                        randomY
                    );

                endPosition =
                    new Vector2(
                        halfWidth + 100.0f,
                        Random.Range(
                            -halfHeight,
                            halfHeight
                        )
                    );

                break;


            // 右 → 左
            case 1:

                startPosition =
                    new Vector2(
                        halfWidth + 100.0f,
                        randomY
                    );

                endPosition =
                    new Vector2(
                        -halfWidth - 100.0f,
                        Random.Range(
                            -halfHeight,
                            halfHeight
                        )
                    );

                break;


            // 下 → 上
            case 2:

                startPosition =
                    new Vector2(
                        randomX,
                        -halfHeight - 100.0f
                    );

                endPosition =
                    new Vector2(
                        Random.Range(
                            -halfWidth,
                            halfWidth
                        ),
                        halfHeight + 100.0f
                    );

                break;


            // 上 → 下
            default:

                startPosition =
                    new Vector2(
                        randomX,
                        halfHeight + 100.0f
                    );

                endPosition =
                    new Vector2(
                        Random.Range(
                            -halfWidth,
                            halfWidth
                        ),
                        -halfHeight - 100.0f
                    );

                break;
        }


        spiderRect.anchoredPosition =
            startPosition;


        // クモの向きを進行方向へ向ける
        Vector2 moveDirection =
            endPosition -
            startPosition;

        float angle =
            Mathf.Atan2(
                moveDirection.y,
                moveDirection.x
            ) * Mathf.Rad2Deg;

        spiderRect.localRotation =
            Quaternion.Euler(
                0.0f,
                0.0f,
                angle - 90.0f
            );


        float timer = 0.0f;


        while (timer < spiderMoveDuration)
        {
            timer +=
                Time.deltaTime;


            float rate =
                timer /
                spiderMoveDuration;


            spiderRect.anchoredPosition =
                Vector2.Lerp(
                    startPosition,
                    endPosition,
                    rate
                );


            yield return null;
        }


        spiderObject.SetActive(
            false
        );

        isHappening = false;
    }


    /// <summary>
    /// テスト用
    /// ノイズを強制発生
    /// </summary>
    public void PlayNoise()
    {
        if (isHappening)
        {
            return;
        }

        StartCoroutine(
            NoiseHappening()
        );
    }


    /// <summary>
    /// テスト用
    /// クモを強制発生
    /// </summary>
    public void PlaySpider()
    {
        if (isHappening)
        {
            return;
        }

        StartCoroutine(
            SpiderHappening()
        );
    }
}