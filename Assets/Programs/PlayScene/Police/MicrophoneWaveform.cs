using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

// Canvas内の空のUIオブジェクトに追加してください。
// Windows Editor / Windows実行ファイル用です。
[RequireComponent(typeof(RectTransform))]
public class MicrophoneWaveform : MonoBehaviour
{
    [SerializeField, Range(4, 64)] private int barCount = 24;
    [SerializeField] private Color barColor = Color.green;
    [SerializeField, Min(0.1f)] private float sensitivity = 12f;
    [SerializeField, Min(0f)] private float noiseThreshold = 0.005f;
    [SerializeField] private bool onlyWhileSpaceHeld = true;

    // 他のUIから参照できる、0～1の音量です。
    public float Volume { get; private set; }

    private RectTransform[] bars;
    private RectTransform area;
    private AudioClip microphoneClip;
    private string deviceName;
    private float[] samples;
    private bool hasEnoughSamples;
    private const int SampleFrames = 1024;

    private void Awake()
    {
        area = GetComponent<RectTransform>();
        bars = new RectTransform[barCount];

        for (int i = 0; i < bars.Length; i++)
        {
            var bar = new GameObject("VoiceBar_" + i,
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = bar.GetComponent<RectTransform>();
            rect.SetParent(transform, false);
            rect.anchorMin = new Vector2((i + 0.15f) / barCount, 0.5f);
            rect.anchorMax = new Vector2((i + 0.85f) / barCount, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(0f, 2f);
            rect.anchoredPosition = Vector2.zero;
            var image = bar.GetComponent<Image>();
            image.color = barColor;
            image.raycastTarget = false;
            bars[i] = rect;
        }
    }

    private void OnEnable()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (Microphone.devices.Length == 0)
        {
            Debug.LogWarning("波形表示：マイクが見つかりません。", this);
            return;
        }

        deviceName = Microphone.devices[0];
        try
        {
            // 1秒分の録音領域を繰り返し使用します。音声は再生しません。
            microphoneClip = Microphone.Start(deviceName, true, 1, 44100);
            if (microphoneClip != null)
                samples = new float[SampleFrames * microphoneClip.channels];
            else
                Debug.LogWarning("波形表示：マイクを開始できませんでした。", this);
        }
        catch (Exception error)
        {
            Debug.LogWarning("波形表示：" + error.Message, this);
        }
#endif
    }

    private void Update()
    {
        bool show = !onlyWhileSpaceHeld ||
            (Keyboard.current != null && Keyboard.current.spaceKey.isPressed);
        bool hasData = false;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (microphoneClip != null && Microphone.IsRecording(deviceName))
        {
            int position = Microphone.GetPosition(deviceName);
            if (position >= SampleFrames) hasEnoughSamples = true;

            if (show && hasEnoughSamples)
            {
                // 現在の録音位置より少し手前の、録音済みデータを読みます。
                int offset = (position - SampleFrames + microphoneClip.samples)
                    % microphoneClip.samples;
                hasData = microphoneClip.GetData(samples, offset);
            }
        }
#endif

        float totalSquares = 0f;
        for (int i = 0; i < bars.Length; i++)
        {
            float level = 0f;
            if (hasData)
            {
                int begin = i * samples.Length / bars.Length;
                int end = (i + 1) * samples.Length / bars.Length;
                float squares = 0f;
                for (int j = begin; j < end; j++)
                    squares += samples[j] * samples[j];
                totalSquares += squares;
                // 各区間の音の大きさ（RMS）をバーの高さに変換します。
                float rms = Mathf.Sqrt(squares / (end - begin));
                level = Mathf.Clamp01(Mathf.Max(0f, rms - noiseThreshold) * sensitivity);
            }

            float height = Mathf.Lerp(2f, Mathf.Max(2f, area.rect.height), level);
            bars[i].SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        }

        Volume = hasData
            ? Mathf.Clamp01(Mathf.Max(0f,
                Mathf.Sqrt(totalSquares / samples.Length) - noiseThreshold) * sensitivity)
            : 0f;
    }

    private void OnDisable()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        if (microphoneClip != null)
        {
            Microphone.End(deviceName);
            Destroy(microphoneClip);
        }
#endif
        microphoneClip = null;
        hasEnoughSamples = false;
        Volume = 0f;
        if (bars != null)
            foreach (var bar in bars)
                if (bar != null)
                    bar.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 2f);
    }
}
