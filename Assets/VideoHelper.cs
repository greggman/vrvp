using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VideoHelper : MonoBehaviour
{
    public Material skyboxVideoMaterial;

    UnityEngine.Video.VideoPlayer videoPlayer;

    // Start is called before the first frame update
    void Start()
    {
        videoPlayer = GetComponent<UnityEngine.Video.VideoPlayer> ();
        videoPlayer.prepareCompleted += OnPrepareCompleted;
    }

    void OnPrepareCompleted(UnityEngine.Video.VideoPlayer videoPlayer)
    {
        uint w = videoPlayer.width;
        uint h = videoPlayer.height;
        Debug.Log("vid size: " + w + " " + h);
        RenderTexture rt = videoPlayer.targetTexture;
        if (rt != null) {
          if (rt.width != w || rt.height != h) {
            Debug.Log("size difference!");
            rt.Release();
            videoPlayer.targetTexture = null;
          }
        }
        if (videoPlayer.targetTexture == null) {
          Debug.Log("make new rt");
          rt = new RenderTexture((int)w, (int)h, 0);
          videoPlayer.targetTexture = rt;
          //skyboxVideoMaterial.SetTexture("_Tex", rt);
          skyboxVideoMaterial.mainTexture = rt;
        }
    }
}
