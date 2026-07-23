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
    public string condition = "baseline";

    [Header("Save Location")]
    [Tooltip("Example: C:/Users/YourName/Desktop/VRPaintingStudy or leave empty to use fallback.")]
    public string preferredRoot = "";

    [Tooltip("Used inside Application.persistentDataPath if preferredRoot fails or when running on Android/Quest.")]
    public string fallbackFolderName = "StudyData";

    [Header("Image Export Fix")]
    public SaveImageTransform imageTransform = SaveImageTransform.Rotate90CounterClockwise;

    [Header("Runtime Info")]
    [SerializeField] private string activeSessionFolder;
    [SerializeField] private string eventsPath;

    [Header("Editor Project Copy")]
    public bool alsoCopyToProjectFolderInEditor = true;

    [Tooltip("Folder created beside Assets, Packages, and ProjectSettings.")]
    public string projectCopyFolderName = "StudyData";

    [SerializeField] private string projectCopySessionFolder;
    [SerializeField] private string projectCopyEventsPath;


    public string ActiveSessionFolder => activeSessionFolder;
    public string EventsPath => eventsPath;

    private bool sessionStarted;

    

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

        projectCopySessionFolder = "";
        projectCopyEventsPath = "";

        #if UNITY_EDITOR
        if (alsoCopyToProjectFolderInEditor)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            string projectCopyRoot = Path.Combine(projectRoot, projectCopyFolderName);

            projectCopySessionFolder = Path.Combine(projectCopyRoot, sessionFolderName);
            Directory.CreateDirectory(projectCopySessionFolder);

            projectCopyEventsPath = Path.Combine(projectCopySessionFolder, "events.jsonl");

            Debug.Log($"[SessionLogger] Project copy folder: {projectCopySessionFolder}");
        }
        #endif

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

    public void StartNewSession(string newParticipantId, string newCondition)
    {
        participantId = SanitizeFileName(newParticipantId);
        condition = SanitizeFileName(newCondition);

        sessionStarted = false;

        activeSessionFolder = "";
        eventsPath = "";

        projectCopySessionFolder = "";
        projectCopyEventsPath = "";

        StartSession();
    }

    public void LogTaskStarted(string taskName, string taskType, bool gamifiedVisible)
    {
        if (!sessionStarted)
            StartSession();

        LogEvent(new TaskStartedEvent
        {
            type = "task_started",
            time = Time.time,
            taskName = taskName,
            taskType = taskType,
            gamifiedVisible = gamifiedVisible
        });
    }

    public void LogStrokePoint(
        Vector2 uv,
        Vector3 worldPosition,
        int brushSizePx,
        Color brushColor,
        bool eraser,
        StrokeScoreResult scoreResult
    )
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
            eraser = eraser,

            taskType = scoreResult.taskType,
            scoreCategory = scoreResult.category,
            pointsAwarded = scoreResult.pointsAwarded,
            referenceValue = scoreResult.referenceValue,
            brushValue = scoreResult.brushValue,
            difference = scoreResult.difference
        });
    }

    public void LogCanvasCleared()
    {
        if (!sessionStarted)
            StartSession();

        LogEvent(new SimpleEvent
        {
            type = "canvas_cleared",
            time = Time.time
        });
    }

    public void LogSubmitted(string pngPath)
    {
        if (!sessionStarted)
            StartSession();

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

        string safeFileName = SanitizeFileName(fileName);

        if (!safeFileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            safeFileName += ".png";

        string pngPath = Path.Combine(activeSessionFolder, safeFileName);

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

#if UNITY_EDITOR
            if (alsoCopyToProjectFolderInEditor && !string.IsNullOrEmpty(projectCopySessionFolder))
            {
                string projectCopyPngPath = Path.Combine(projectCopySessionFolder, safeFileName);
                File.WriteAllBytes(projectCopyPngPath, pngBytes);

                Debug.Log($"[SessionLogger] Project copy PNG saved: {projectCopyPngPath}");
            }
#endif

            if (finalTexture != sourceTexture)
                Destroy(finalTexture);

            Destroy(sourceTexture);

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
            string line = json + Environment.NewLine;

            if (!string.IsNullOrEmpty(eventsPath))
            {
                File.AppendAllText(eventsPath, line);
            }
            else
            {
                Debug.LogError("[SessionLogger] eventsPath is empty. Session was not started correctly.");
            }

#if UNITY_EDITOR
            if (alsoCopyToProjectFolderInEditor && !string.IsNullOrEmpty(projectCopyEventsPath))
            {
                File.AppendAllText(projectCopyEventsPath, line);
            }
#endif
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

    public void LogTaskSubmitted(
    string taskName,
    string taskType,
    string condition,
    bool gamifiedVisible,
    float taskElapsedSeconds,
    int totalStrokePoints,
    int excellentPoints,
    int goodPoints,
    int okayPoints,
    int inaccuratePoints,
    int score,
    float progressPercent,
    string savedImagePath
)
    {
        if (!sessionStarted)
            StartSession();

        LogEvent(new TaskSubmittedEvent
        {
            type = "task_submitted",
            time = Time.time,
            taskName = taskName,
            taskType = taskType,
            condition = condition,
            gamifiedVisible = gamifiedVisible,
            taskElapsedSeconds = taskElapsedSeconds,
            totalStrokePoints = totalStrokePoints,
            excellentPoints = excellentPoints,
            goodPoints = goodPoints,
            okayPoints = okayPoints,
            inaccuratePoints = inaccuratePoints,
            score = score,
            progressPercent = progressPercent,
            savedImagePath = savedImagePath
        });
    }

    public void LogFeedbackShown(string taskName, string taskType, string feedbackText)
    {
        if (!sessionStarted)
            StartSession();

        LogEvent(new FeedbackShownEvent
        {
            type = "feedback_shown",
            time = Time.time,
            taskName = taskName,
            taskType = taskType,
            feedbackText = feedbackText
        });
    }

    public void LogSessionComplete()
    {
        if (!sessionStarted)
            StartSession();

        LogEvent(new SimpleEvent
        {
            type = "session_complete",
            time = Time.time
        });
    }



    [Serializable]
    private class TaskSubmittedEvent
    {
        public string type;
        public float time;

        public string taskName;
        public string taskType;
        public string condition;
        public bool gamifiedVisible;

        public float taskElapsedSeconds;

        public int totalStrokePoints;
        public int excellentPoints;
        public int goodPoints;
        public int okayPoints;
        public int inaccuratePoints;

        public int score;
        public float progressPercent;

        public string savedImagePath;
    }

    [Serializable]
    private class FeedbackShownEvent
    {
        public string type;
        public float time;

        public string taskName;
        public string taskType;
        public string feedbackText;
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
    private class TaskStartedEvent
    {
        public string type;
        public float time;
        public string taskName;
        public string taskType;
        public bool gamifiedVisible;
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

        public string taskType;
        public string scoreCategory;
        public int pointsAwarded;
        public float referenceValue;
        public float brushValue;
        public float difference;
    }

    [Serializable]
    private class SubmittedEvent
    {
        public string type;
        public float time;
        public string pngPath;
    }
}