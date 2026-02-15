using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections.Generic;
using System.IO;
using TMPro;

[System.Serializable]
public class VideoItem
{
    public VideoPlayer videoPlayer; // Assign the VideoPlayer component
    public RawImage rawImage;       // Assign the RawImage that will display the video
    public string fileName;         // Name of the video in StreamingAssets, e.g., "myvideo.mp4"
}

public class DebugScript : MonoBehaviour
{
    public List<VideoItem> videos = new List<VideoItem>();
    public TextMeshProUGUI text;

    public void Start()
    {
        foreach (var item in videos)
        {
            if (item.videoPlayer == null || item.rawImage == null || string.IsNullOrEmpty(item.fileName))
            {
                Debug.LogWarning("VideoItem missing references or filename.");
                continue;
            }

            string path = Path.Combine(Application.streamingAssetsPath, item.fileName);
            item.videoPlayer.url = path;

            text.text = path + " aaa";

            // Prepare the video first to get its dimensions
            item.videoPlayer.Prepare();
            item.videoPlayer.prepareCompleted += (vp) =>
            {
                // Create RenderTexture matching video dimensions
                RenderTexture rt = new RenderTexture(vp.texture.width, vp.texture.height, 0);
                vp.targetTexture = rt;
                item.rawImage.texture = rt;

                vp.Play();
            };
        }
    }
}
