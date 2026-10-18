mergeInto(LibraryManager.library, {
    WebVoice_Init: function (objectNamePtr) {
        // 前のシーンの処理が残っていたら解除
        if (window.unityWebVoice) {
            window.unityWebVoice.dispose();
        }

        var objectName = UTF8ToString(objectNamePtr);
        var SpeechRecognition =
            window.SpeechRecognition ||
            window.webkitSpeechRecognition;

        function report(message) {
            SendMessage(objectName, "OnWebVoiceError", message);
        }

        if (!SpeechRecognition) {
            report("このブラウザは音声認識に対応していません。");
            return;
        }

        var recognition = new SpeechRecognition();
        recognition.lang = "ja-JP";
        recognition.continuous = true;
        recognition.interimResults = false;

        var held = false;
        var running = false;
        var disposed = false;

        recognition.onresult = function (event) {
            if (disposed) return;

            for (var i = event.resultIndex;
                 i < event.results.length; i++) {
                if (event.results[i].isFinal) {
                    SendMessage(
                        objectName,
                        "OnWebVoiceResult",
                        event.results[i][0].transcript
                    );
                }
            }
        };

        recognition.onerror = function (event) {
            if (!disposed) {
                report("音声認識エラー: " + event.error);
            }
        };

        recognition.onend = function () {
            running = false;
        };

        function onKeyDown(event) {
            if (event.code !== "Space" || event.repeat) return;

            // 入力欄に文字を打っている間は開始しない
            var tag = event.target && event.target.tagName;
            if (tag === "INPUT" || tag === "TEXTAREA" ||
                (event.target && event.target.isContentEditable)) {
                return;
            }

            event.preventDefault();
            held = true;

            if (running) return;

            try {
                recognition.start();
                running = true;
            } catch (error) {
                report(error.message);
            }
        }

        function onKeyUp(event) {
            if (event.code !== "Space") return;

            held = false;

            // abortではなくstopで、最後の認識結果を待つ
            if (running) {
                recognition.stop();
            }
        }

        function onBlur() {
            held = false;
            if (running) recognition.stop();
        }

        document.addEventListener("keydown", onKeyDown);
        document.addEventListener("keyup", onKeyUp);
        window.addEventListener("blur", onBlur);

        window.unityWebVoice = {
            dispose: function () {
                disposed = true;
                document.removeEventListener("keydown", onKeyDown);
                document.removeEventListener("keyup", onKeyUp);
                window.removeEventListener("blur", onBlur);

                recognition.onresult = null;
                recognition.onerror = null;
                recognition.onend = null;
                recognition.abort();
            }
        };
    },

    WebVoice_Dispose: function () {
        if (window.unityWebVoice) {
            window.unityWebVoice.dispose();
            window.unityWebVoice = null;
        }
    }
});