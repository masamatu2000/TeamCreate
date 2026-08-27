using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class RuleSceneManager : MonoBehaviour
{
    [SerializeField]
    private VideoPlayer videoPlayer;

    private void Start()
    {
        // 動画が最後まで再生されたとき
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void Update()
    {
        // スペースキーを押したらPlaySceneへ移動
        if (Keyboard.current != null &&
            Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            GoToPlayScene();
        }
    }

    /// <summary>
    /// 動画が最後まで再生されたとき
    /// </summary>
    private void OnVideoFinished(VideoPlayer player)
    {
        GoToPlayScene();
    }

    /// <summary>
    /// PlaySceneへ移動
    /// </summary>
    private void GoToPlayScene()
    {
        SceneManager.LoadScene("PlayScene");
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoFinished;
        }
    }
}