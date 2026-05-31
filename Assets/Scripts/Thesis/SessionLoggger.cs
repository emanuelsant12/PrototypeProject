using System;
using System.IO;
using UnityEngine;

public enum SaveImageTransform
{
    None,
    Rotate90Clockwise,
    Rotate90CounterClockwise,
    Rotate180,
    FlipVertical,
    FlipHorizontal
}

public class SessionLogger : MonoBehaviour
{
    [Header("Session Info")]
    public string participantId = "P01";
    public string condition = "Baseline";

    [Header("Save Location")]
    [Tooltip("Example: C:/Users/YourName/Desktop/VRPaintingStudy or leave empty to use fallback.")]
    public string preferredRoot = "";

    [Tooltip("Used inside Application.persistentDataPath if preferredRoot fails or when running on Android/Quest.")]
    public string fallbackFolderName = "VRPaintingStudy";

    [Header("Runtime Info")]
    [SerializeField] private string activeSessionFolder;
    [SerializeField] private string eventsPath;

    [Header("Image Export Fix")]
    public SaveImageTransform imageTransform = SaveImageTransform.Rotate90CounterClockwise;

    public string ActiveSessionFolder => activeSessionFolder;
    public string EventsPath => eventsPath;

    private bool sessionStarted;

    private void Awake()
    {
        StartSession();
    }

    public void StartSession()
    {
        if (sessionStarted)
            return;

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        string cleanParticipant = SanitizeFileName(participantId);
        string cleanCondition = SanitizeFileName(condition);

        string sessionFolderName = $"{cleanParticipant}_{cleanCondition}_{timestamp}";

        activeSessionFolder = ResolveSessionFolder(sessionFolderName);
        Directory.CreateDirectory(activeSessionFolder);

        eventsPath = Path.Combine(activeSessionFolder, "events.jsonl");

        sessionStarted = true;

        LogEvent(new SessionStartedEvent
        {
            type = "session_started",
            time = Time.time,
            utc = DateTime.UtcNow.ToString("o"),
            participantId = participantId,
            condition = condition,
            sessionFolder = activeSessionFolder,
            platform = Application.platform.ToString()
        });

        Debug.Log($"[SessionLogger] Session folder: {activeSessionFolder}");
    }

    public void LogStrokePoint(Vector2 uv, Vector3 worldPosition, int brushSizePx, Color brushColor, bool eraser)
    {
        if (!sessionStarted)
            StartSession();

        LogEvent(new StrokePointEvent
        {
            type = "stroke_point",
            time = Time.time,
            uvX = uv.x,
            uvY = uv.y,
            worldX = worldPosition.x,
            worldY = worldPosition.y,
            worldZ = worldPosition.z,
            brushSizePx = brushSizePx,
            value = brushColor.r,
            eraser = eraser
        });
    }

    public void LogCanvasCleared()
    {
        LogEvent(new SimpleEvent
        {
            type = "canvas_cleared",
            time = Time.time
        });
    }

    public void LogSubmitted(string pngPath)
    {
        LogEvent(new SubmittedEvent
        {
            type = "submitted",
            time = Time.time,
            pngPath = pngPath
        });
    }

    public string SaveRenderTextureAsPng(RenderTexture renderTexture, string fileName = "final.png")
    {
        if (!sessionStarted)
            StartSession();

        if (renderTexture == null)
        {
            Debug.LogError("[SessionLogger] Cannot save PNG because RenderTexture is null.");
            return null;
        }

        string pngPath = Path.Combine(activeSessionFolder, fileName);

        RenderTexture previous = RenderTexture.active;

        try
        {
            RenderTexture.active = renderTexture;

            Texture2D sourceTexture = new Texture2D(
                renderTexture.width,
                renderTexture.height,
                TextureFormat.RGBA32,
                false
            );

            sourceTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            sourceTexture.Apply();

            Texture2D finalTexture = ApplyImageTransform(sourceTexture, imageTransform);

            byte[] pngBytes = finalTexture.EncodeToPNG();
            File.WriteAllBytes(pngPath, pngBytes);

            Destroy(sourceTexture);

            if (finalTexture != sourceTexture)
                Destroy(finalTexture);

            LogSubmitted(pngPath);

            Debug.Log($"[SessionLogger] Saved PNG: {pngPath}");
            return pngPath;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionLogger] Failed to save PNG: {e.Message}");
            return null;
        }
        finally
        {
            RenderTexture.active = previous;
        }
    }

    private Texture2D ApplyImageTransform(Texture2D source, SaveImageTransform transform)
    {
        if (transform == SaveImageTransform.None)
            return source;

        Color32[] sourcePixels = source.GetPixels32();
        int sourceWidth = source.width;
        int sourceHeight = source.height;

        int targetWidth = sourceWidth;
        int targetHeight = sourceHeight;

        if (transform == SaveImageTransform.Rotate90Clockwise ||
            transform == SaveImageTransform.Rotate90CounterClockwise)
        {
            targetWidth = sourceHeight;
            targetHeight = sourceWidth;
        }

        Texture2D target = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false);
        Color32[] targetPixels = new Color32[targetWidth * targetHeight];

        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < sourceWidth; x++)
            {
                int sourceIndex = y * sourceWidth + x;

                int targetX = x;
                int targetY = y;

                switch (transform)
                {
                    case SaveImageTransform.Rotate90Clockwise:
                        targetX = sourceHeight - 1 - y;
                        targetY = x;
                        break;

                    case SaveImageTransform.Rotate90CounterClockwise:
                        targetX = y;
                        targetY = sourceWidth - 1 - x;
                        break;

                    case SaveImageTransform.Rotate180:
                        targetX = sourceWidth - 1 - x;
                        targetY = sourceHeight - 1 - y;
                        break;

                    case SaveImageTransform.FlipVertical:
                        targetX = x;
                        targetY = sourceHeight - 1 - y;
                        break;

                    case SaveImageTransform.FlipHorizontal:
                        targetX = sourceWidth - 1 - x;
                        targetY = y;
                        break;
                }

                int targetIndex = targetY * targetWidth + targetX;
                targetPixels[targetIndex] = sourcePixels[sourceIndex];
            }
        }

        target.SetPixels32(targetPixels);
        target.Apply();

        return target;
    }

    private void LogEvent<T>(T eventData)
    {
        try
        {
            string json = JsonUtility.ToJson(eventData);
            File.AppendAllText(eventsPath, json + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SessionLogger] Failed to write JSONL event: {e.Message}");
        }
    }

    private string ResolveSessionFolder(string sessionFolderName)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return GetFallbackSessionFolder(sessionFolderName);
#else
        if (!string.IsNullOrWhiteSpace(preferredRoot))
        {
            try
            {
                string preferredSessionFolder = Path.Combine(preferredRoot, sessionFolderName);

                Directory.CreateDirectory(preferredSessionFolder);

                string testFile = Path.Combine(preferredSessionFolder, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);

                return preferredSessionFolder;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SessionLogger] Preferred path failed. Falling back. Reason: {e.Message}");
            }
        }

        return GetFallbackSessionFolder(sessionFolderName);
#endif
    }

    private string GetFallbackSessionFolder(string sessionFolderName)
    {
        string fallbackRoot = Path.Combine(Application.persistentDataPath, fallbackFolderName);
        return Path.Combine(fallbackRoot, sessionFolderName);
    }

    private static string SanitizeFileName(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return "Unknown";

        foreach (char c in Path.GetInvalidFileNameChars())
            input = input.Replace(c, '_');

        input = input.Replace(" ", "_");

        return input;
    }

    [Serializable]
    private class SimpleEvent
    {
        public string type;
        public float time;
    }

    [Serializable]
    private class SessionStartedEvent
    {
        public string type;
        public float time;
        public string utc;
        public string participantId;
        public string condition;
        public string sessionFolder;
        public string platform;
    }

    [Serializable]
    private class StrokePointEvent
    {
        public string type;
        public float time;

        public float uvX;
        public float uvY;

        public float worldX;
        public float worldY;
        public float worldZ;

        public int brushSizePx;
        public float value;
        public bool eraser;
    }

    [Serializable]
    private class SubmittedEvent
    {
        public string type;
        public float time;
        public string pngPath;
    }
}