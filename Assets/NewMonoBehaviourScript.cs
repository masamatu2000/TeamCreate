using UnityEngine;
using UnityEngine.Windows.Speech;

public class DictationTest : MonoBehaviour
{
    private DictationRecognizer recognizer;

    private void Start()
    {
        recognizer = new DictationRecognizer();

        recognizer.DictationHypothesis += OnHypothesis;
        recognizer.DictationResult += OnResult;
        recognizer.DictationComplete += OnComplete;
        recognizer.DictationError += OnError;

        recognizer.InitialSilenceTimeoutSeconds = 60f;
        recognizer.AutoSilenceTimeoutSeconds = 3600f;

        recognizer.Start();

        Debug.Log("Dictationテスト開始");
    }

    private void OnHypothesis(string text)
    {
        Debug.Log("途中：" + text);
    }

    private void OnResult(
        string text,
        ConfidenceLevel confidence)
    {
        Debug.Log(
            "結果：" +
            text +
            " / " +
            confidence
        );
    }

    private void OnComplete(
        DictationCompletionCause cause)
    {
        Debug.LogWarning(
            "終了：" +
            cause
        );
    }

    private void OnError(
        string error,
        int hresult)
    {
        Debug.LogError(
            "エラー：" +
            error +
            " / " +
            hresult
        );
    }

    private void OnDestroy()
    {
        if (recognizer == null)
        {
            return;
        }

        recognizer.DictationHypothesis -= OnHypothesis;
        recognizer.DictationResult -= OnResult;
        recognizer.DictationComplete -= OnComplete;
        recognizer.DictationError -= OnError;

        if (recognizer.Status ==
            SpeechSystemStatus.Running)
        {
            recognizer.Stop();
        }

        recognizer.Dispose();
    }
}