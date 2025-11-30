using System.Collections;
using UnityEngine;
using System.IO;
using Debug = UnityEngine.Debug;
using TMPro;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics; // Required for Process (Editor)

public class StockfishManager : MonoBehaviour
{
    // --- EDITOR PROCESS (Windows/Mac) ---
    private Process editorProcess;

    // --- ANDROID PROCESS (JNI) ---
    private AndroidJavaObject javaProcess;
    private AndroidJavaObject outputStream;
    private AndroidJavaObject inputStream;
    private AndroidJavaObject errorStream;
    private Thread outputThread;
    private Thread errorThread;
    private volatile bool isRunning = false;

    // --- SHARED STATE ---
    private string latestMessage = null;
    private volatile string lastEngineLine = "";
    private long lastMessageTick = 0;

    // Concurrency control
    private long currentCommandId = 0;

    async void Start()
    {
        bool success = StartStockfish();

        if (success)
        {
            // Init Sequence
            await SendCommandAwaitResult("uci");
            await SendCommandAwaitResult("isready");
        }
    }

    void Update()
    {
        // Update UI from the shared 'latestMessage' variable
        string message = System.Threading.Interlocked.Exchange(ref latestMessage, null);

        if (message != null)
        {
            Debug.Log("SF: " + message);
        }
    }

    // --- test method, how to talk to stockfish ---

    public async void TestInputAsync(string input)
    {
        if (input == null) return;
        string command = input;

        string finalMessage = await SendCommandAwaitResult(command, 0.5f);

        //if (finalMessage != null)
        //{
        //    if (popupObject != null) popupObject.text = "Final: " + finalMessage;
        //}
    }

    public async Task<string> SendCommandAwaitResult(string command, float silenceTimeout = 0.2f)
    {
        currentCommandId++;
        long myId = currentCommandId;
        long timeBeforeSend = DateTime.UtcNow.Ticks;

        SendUciCommand(command);

        while (true)
        {
            await Task.Delay(25);

            // 1. Cancellation Check
            if (myId != currentCommandId) return null;

            // 2. Silence Check
            long now = DateTime.UtcNow.Ticks;
            long lastTick = Interlocked.Read(ref lastMessageTick);
            TimeSpan timeSinceLastMsg = new TimeSpan(now - lastTick);

            if (lastTick > timeBeforeSend && timeSinceLastMsg.TotalSeconds > silenceTimeout)
            {
                return lastEngineLine;
            }
        }
    }

    public bool StartStockfish()
    {
#if UNITY_EDITOR
        return StartStockfishEditor();
#elif UNITY_ANDROID
        return StartStockfishAndroid();
#else
        return false;
#endif
    }

    // --- EDITOR IMPLEMENTATION (C# Process) ---
    private bool StartStockfishEditor()
    {
        string binaryName = "stockfish.exe"; // Windows
        // On Mac, you might need just "stockfish"
        string path = Path.Combine(Application.streamingAssetsPath, binaryName);

        if (!File.Exists(path))
        {
            latestMessage = $"Editor Error: Missing {path}";
            return false;
        }

        try
        {
            editorProcess = new Process();
            editorProcess.StartInfo.FileName = path;
            editorProcess.StartInfo.UseShellExecute = false;
            editorProcess.StartInfo.RedirectStandardInput = true;
            editorProcess.StartInfo.RedirectStandardOutput = true;
            editorProcess.StartInfo.RedirectStandardError = true;
            editorProcess.StartInfo.CreateNoWindow = true;

            // Hook up events (Simpler than threads for Editor)
            editorProcess.OutputDataReceived += (s, e) => HandleEditorOutput(e.Data, false);
            editorProcess.ErrorDataReceived += (s, e) => HandleEditorOutput(e.Data, true);

            editorProcess.Start();
            editorProcess.BeginOutputReadLine();
            editorProcess.BeginErrorReadLine();

            return true;
        }
        catch (Exception e)
        {
            latestMessage = "Editor Launch Failed: " + e.Message;
            return false;
        }
    }

    private void HandleEditorOutput(string line, bool isError)
    {
        if (string.IsNullOrEmpty(line)) return;

        // 1. Update Timer
        Interlocked.Exchange(ref lastMessageTick, DateTime.UtcNow.Ticks);

        // 2. Update Logic State
        lastEngineLine = line;

        // 3. Update UI
        if (isError) latestMessage = $"<color=red>{line}</color>";
        else latestMessage = line;
    }

    // --- ANDROID IMPLEMENTATION (Java/JNI) ---
    private bool StartStockfishAndroid()
    {
        string binaryPath = GetNativeLibPath();
        string binaryDir = Path.GetDirectoryName(binaryPath);

        if (!File.Exists(binaryPath))
        {
            latestMessage = $"Error: File missing at {binaryPath}";
            return false;
        }

        try
        {
            AndroidJavaClass listClass = new AndroidJavaClass("java.util.ArrayList");
            AndroidJavaObject commandList = new AndroidJavaObject("java.util.ArrayList");
            commandList.Call<bool>("add", "/system/bin/sh");
            commandList.Call<bool>("add", "-c");
            commandList.Call<bool>("add", $"export LD_LIBRARY_PATH={binaryDir}; \"{binaryPath}\"");

            AndroidJavaObject processBuilder = new AndroidJavaObject("java.lang.ProcessBuilder", commandList);
            processBuilder.Call<AndroidJavaObject>("redirectErrorStream", false);

            javaProcess = processBuilder.Call<AndroidJavaObject>("start");

            if (javaProcess == null) return false;

            outputStream = javaProcess.Call<AndroidJavaObject>("getOutputStream");
            inputStream = javaProcess.Call<AndroidJavaObject>("getInputStream");
            errorStream = javaProcess.Call<AndroidJavaObject>("getErrorStream");

            if (outputStream == null || inputStream == null || errorStream == null) return false;

            isRunning = true;

            outputThread = new Thread(() => ReadStream(inputStream, "OUT"));
            outputThread.IsBackground = true;
            outputThread.Start();

            errorThread = new Thread(() => ReadStream(errorStream, "ERR"));
            errorThread.IsBackground = true;
            errorThread.Start();

            return true;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            latestMessage = $"Crash: {e.Message}";
            return false;
        }
    }

    private void ReadStream(AndroidJavaObject stream, string tag)
    {
        AndroidJNI.AttachCurrentThread();
        try
        {
            AndroidJavaObject inputStreamReader = new AndroidJavaObject("java.io.InputStreamReader", stream);
            AndroidJavaObject reader = new AndroidJavaObject("java.io.BufferedReader", inputStreamReader);

            while (isRunning)
            {
                string line = reader.Call<string>("readLine");
                if (line == null) break;

                // 1. Update Timer
                Interlocked.Exchange(ref lastMessageTick, DateTime.UtcNow.Ticks);

                // 2. Update Logic State
                lastEngineLine = line;

                // 3. Update UI
                if (tag == "ERR") latestMessage = $"<color=red>{line}</color>";
                else latestMessage = line;
            }
        }
        catch (Exception e)
        {
            if (isRunning) latestMessage = $"{tag} Read Error: {e.Message}";
        }
        finally
        {
            AndroidJNI.DetachCurrentThread();
        }
    }

    // --- SHARED HELPER METHODS ---

    private void SendUciCommand(string command)
    {
#if UNITY_EDITOR
        if (editorProcess != null && !editorProcess.HasExited)
        {
            editorProcess.StandardInput.WriteLine(command);
            editorProcess.StandardInput.Flush();
        }
#elif UNITY_ANDROID
        if (javaProcess != null && outputStream != null)
        {
            try
            {
                AndroidJavaObject writer = new AndroidJavaObject("java.io.OutputStreamWriter", outputStream);
                AndroidJavaObject bufferedWriter = new AndroidJavaObject("java.io.BufferedWriter", writer);
                
                bufferedWriter.Call("write", command);
                bufferedWriter.Call("newLine");
                bufferedWriter.Call("flush");
            }
            catch (Exception e)
            {
                latestMessage = $"Write Failed: {e.Message}";
            }
        }
#endif
    }

    private string GetNativeLibPath()
    {
        AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        AndroidJavaObject appInfo = currentActivity.Call<AndroidJavaObject>("getApplicationInfo");
        string nativeLibDir = appInfo.Get<string>("nativeLibraryDir");
        return Path.Combine(nativeLibDir, "libstockfish.so");
    }

    void OnDestroy()
    {
        // 1. Cleanup Android
        isRunning = false;
        if (javaProcess != null)
        {
            try { javaProcess.Call("destroy"); } catch { }
        }

        // 2. Cleanup Editor
        if (editorProcess != null && !editorProcess.HasExited)
        {
            editorProcess.Kill();
            editorProcess.Dispose();
        }
    }
}