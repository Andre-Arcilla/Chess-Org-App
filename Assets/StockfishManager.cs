using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using System.IO;
using Debug = UnityEngine.Debug;
using TMPro;
using System;
using System.Threading;

public class StockfishManager : MonoBehaviour
{
    private Thread outputThread;
    private Thread errorThread;
    private volatile bool isRunning = false;

    // JNI Objects
    private AndroidJavaObject javaProcess;
    private AndroidJavaObject outputStream; // stdin (Write to process)
    private AndroidJavaObject inputStream;  // stdout (Read from process)
    private AndroidJavaObject errorStream;  // stderr (Read errors)

    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();

    [Header("Popup")]
    [SerializeField] private TextMeshProUGUI popupObject;

    void Start()
    {
        if (popupObject != null) popupObject.text = "Starting Engine...";
        StartStockfish();
    }

    void Update()
    {
        while (logQueue.TryDequeue(out string message))
        {
            if (popupObject != null)
            {
                if (message.StartsWith("info"))
                    popupObject.text = "Thinking...";
                else
                {
                    // Keeping your requested debug format
                    Debug.Log("aaaaaaaaaaaaa " + message);
                    popupObject.text = "aaaaaaaaaaaaa " + message;
                }
            }
        }
    }

    public void StartStockfish()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        StartStockfishAndroid();
#else
        logQueue.Enqueue("Use Editor Version on PC.");
#endif
    }

    private void StartStockfishAndroid()
    {
        string binaryPath = GetNativeLibPath();
        string binaryDir = Path.GetDirectoryName(binaryPath);

        Debug.Log($"[Java] Launching: {binaryPath}");

        if (!File.Exists(binaryPath))
        {
            logQueue.Enqueue($"Error: File missing at {binaryPath}");
            return;
        }

        try
        {
            // 1. Setup Command List
            AndroidJavaClass listClass = new AndroidJavaClass("java.util.ArrayList");
            AndroidJavaObject commandList = new AndroidJavaObject("java.util.ArrayList");
            commandList.Call<bool>("add", "/system/bin/sh");
            commandList.Call<bool>("add", "-c");
            commandList.Call<bool>("add", $"export LD_LIBRARY_PATH={binaryDir}; \"{binaryPath}\"");

            // 2. ProcessBuilder
            AndroidJavaObject processBuilder = new AndroidJavaObject("java.lang.ProcessBuilder", commandList);
            processBuilder.Call<AndroidJavaObject>("redirectErrorStream", false); // Keep streams separate

            // 3. Start Process
            javaProcess = processBuilder.Call<AndroidJavaObject>("start");

            if (javaProcess == null)
            {
                logQueue.Enqueue("Fatal: Java Process is null.");
                return;
            }

            // 4. Get Streams
            outputStream = javaProcess.Call<AndroidJavaObject>("getOutputStream");
            inputStream = javaProcess.Call<AndroidJavaObject>("getInputStream");
            errorStream = javaProcess.Call<AndroidJavaObject>("getErrorStream");

            if (outputStream == null || inputStream == null || errorStream == null)
            {
                logQueue.Enqueue("Fatal: One or more streams are null.");
                return;
            }

            isRunning = true;

            // 5. Start Reader Threads
            outputThread = new Thread(() => ReadStream(inputStream, "OUT"));
            outputThread.IsBackground = true;
            outputThread.Start();

            errorThread = new Thread(() => ReadStream(errorStream, "ERR"));
            errorThread.IsBackground = true;
            errorThread.Start();

            // 6. Test
            logQueue.Enqueue("Process Started. Sending 'uci'...");
            SendUciCommand("uci");
            StartCoroutine(RunCalculationTest());
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            logQueue.Enqueue($"Crash: {e.Message}");
        }
    }

    // Generic Stream Reader
    private void ReadStream(AndroidJavaObject stream, string tag)
    {
        // --- CRITICAL FIX START ---
        // This attaches the C# Thread to the Android Java VM.
        // Without this, using AndroidJavaObject causes "Object reference not set..." crash.
        AndroidJNI.AttachCurrentThread();
        try
        {
            AndroidJavaObject inputStreamReader = new AndroidJavaObject("java.io.InputStreamReader", stream);
            AndroidJavaObject reader = new AndroidJavaObject("java.io.BufferedReader", inputStreamReader);

            while (isRunning)
            {
                string line = reader.Call<string>("readLine");
                if (line == null) break;

                if (tag == "OUT")
                {
                    if (line.StartsWith("bestmove") || line == "readyok" || line == "uciok" || line.StartsWith("id"))
                    {
                        logQueue.Enqueue(line);
                    }
                }
                else // ERROR STREAM
                {
                    logQueue.Enqueue($"<color=red>{line}</color>");
                }
            }
        }
        catch (System.Exception e)
        {
            if (isRunning) logQueue.Enqueue($"{tag} Read Error: {e.Message}");
        }
        finally
        {
            // Must detach to prevent memory leaks or crashes on app close
            AndroidJNI.DetachCurrentThread();
        }
        // --- CRITICAL FIX END ---
    }

    private void SendUciCommand(string command)
    {
        if (javaProcess != null && outputStream != null)
        {
            try
            {
                // Write command + \n
                AndroidJavaObject writer = new AndroidJavaObject("java.io.OutputStreamWriter", outputStream);
                AndroidJavaObject bufferedWriter = new AndroidJavaObject("java.io.BufferedWriter", writer);

                bufferedWriter.Call("write", command);
                bufferedWriter.Call("newLine");
                bufferedWriter.Call("flush");
            }
            catch (Exception e)
            {
                logQueue.Enqueue($"Write Failed: {e.Message}");
            }
        }
    }

    IEnumerator RunCalculationTest()
    {
        yield return new WaitForSeconds(1.0f);
        SendUciCommand("isready");
        yield return new WaitForSeconds(1.0f);
        SendUciCommand("go depth 2");
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
        isRunning = false;
        if (javaProcess != null)
        {
            try { javaProcess.Call("destroy"); } catch { }
        }
    }
}