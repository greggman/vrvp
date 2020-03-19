using OxOD;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using RenderHeads.Media.AVProVideo;

namespace GMan {

interface VideoWrapper {
  double time { get; set; }
  double length { get; }
  bool isPlaying { get; }
  string extensions { get; }
  double playRate { get; set; }

  void SetUrl(string url);
  void Play();
  void Pause();
  void Stop();

  void SetViewSettings(ViewSettings vs);
}

class VideoPlayerWrapper : VideoWrapper {
  VideoPlayer videoPlayer;
  public VideoPlayerWrapper(VideoPlayer vp) {
    videoPlayer = vp;
  }
  public double time {
    get { return videoPlayer.time; }
    set { videoPlayer.time = value; }
  }
  public double length {
    get { return videoPlayer.length; }
  }
  public double playRate {
    get { return videoPlayer.playbackSpeed; }
    set { videoPlayer.playbackSpeed = (float)value; }
  }
  public string extensions {
    get { return ".mp4|.m4v"; }
  }
  public bool isPlaying {
    get { return videoPlayer.isPlaying; }
  }
  public void SetUrl(string url) {
    videoPlayer.url = url;
  }
  public void Play() {
    videoPlayer.Play();
  }
  public void Pause() {
    videoPlayer.Pause();
  }
  public void Stop() {
    videoPlayer.Stop();
  }
  public void SetViewSettings(ViewSettings vs)
  {
  }
};

class MediaPlayerWrapper : VideoWrapper {
  MediaPlayer mediaPlayer;
  ApplyToMaterial applyToMaterial;
  public MediaPlayerWrapper(MediaPlayer vp, ApplyToMaterial atm) {
    mediaPlayer = vp;
    applyToMaterial = atm;
  }
  public double time {
    get { return mediaPlayer.Control.GetCurrentTimeMs() / 1000.0f; }
    set { mediaPlayer.Control.SeekFast((float)(value * 1000.0)); }
  }
  public double length {
    get { return mediaPlayer.Info.GetDurationMs() / 1000.0f; }
  }
  public double playRate {
    get { return mediaPlayer.Control.GetPlaybackRate(); }
    set { mediaPlayer.Control.SetPlaybackRate((float)value); }
  }
  public bool isPlaying {
    get { return mediaPlayer.Control.IsPlaying(); }
  }
  public string extensions {
    get { return ".mp4|.m4v|.webm|.mkv|.avi|.asf|.wmv|.mov"; }
  }
  public void SetUrl(string url) {
    mediaPlayer.OpenVideoFromFile(MediaPlayer.FileLocation.AbsolutePathOrURL, url, false);
  }
  public void Play() {
    mediaPlayer.Play();
  }
  public void Pause() {
    mediaPlayer.Pause();
  }
  public void Stop() {
    mediaPlayer.Stop();
  }
  float ProjectionTypeToMapping(ProjectionType pt) {
    switch(pt) {
      case ProjectionType.None: return 3f;
      case ProjectionType.Fisheye: return 2f;
      case ProjectionType.Equirectangular: return 1f;
      //case ProjectionType.Cubemap: return 0f;
      default: return 0f;
    }
  }
  float LayoutToLayout(Layout lt) {
    switch(lt) {
      case Layout.FullFrame: return 0f;
      case Layout.LeftRight: return 1f;
      case Layout.TopBottom: return 2f;
      case Layout.RightLeft: return 3f;
      case Layout.BottomTop: return 4f;
      default: return 0f;
    }
  }
  float FieldOfViewToImageType(FieldOfView fov) {
    switch(fov){
      case FieldOfView.Flat: return 2f;
      case FieldOfView.View180: return 1f;
      case FieldOfView.View360: return 0f;
      default: return 0f;
    }
  }
  static void Enable(Material mat, string keyword, bool enable) {
    if (enable) {
      mat.EnableKeyword(keyword);
    } else {
      mat.DisableKeyword(keyword);
    }
  }
  public void SetViewSettings(ViewSettings vs) {
    Material mat = applyToMaterial._material;
    mat.SetFloat("_Mapping", ProjectionTypeToMapping(vs.projectionType));
    Enable(mat, "_MAPPING_6_FRAMES_LAYOUT", vs.projectionType == ProjectionType.None);
    Enable(mat, "_MAPPING_LATITUDE_LONGITUDE_LAYOUT", vs.projectionType == ProjectionType.Equirectangular);
    Enable(mat, "_MAPPING_FISHEYE_LAYOUT", vs.projectionType == ProjectionType.Fisheye);
    mat.SetFloat("_ImageType", FieldOfViewToImageType(vs.fieldOfView));
    mat.SetFloat("_Layout", LayoutToLayout(vs.layout));
  }
}

public enum ProjectionType {
  None,
  Equirectangular,
  Fisheye,
}

public enum FieldOfView {
  Flat,
  View180,
  View360,
}

public enum Layout {
  FullFrame,
  LeftRight,
  RightLeft,
  TopBottom,
  BottomTop,
}

[System.Serializable]
public class ViewSettings
{
    public ProjectionType projectionType = ProjectionType.Equirectangular;
    public FieldOfView fieldOfView = FieldOfView.View180;
    public Layout layout = Layout.LeftRight;
    public bool yFlip = false;
    public float scale = 1;

    public void SetProjectionType(ProjectionType pt) { projectionType = pt; }
    public void SetFieldOfView(FieldOfView fov) { fieldOfView = fov; }
    public void SetLayout(Layout lt) { layout = lt; }
    public void SetFlipY(bool flip) { yFlip = flip; }
    public void SetScale(float s) { scale = s; }

    public void Copy(ViewSettings src) {
      projectionType = src.projectionType;
      fieldOfView = src.fieldOfView;
      layout = src.layout;
      yFlip = src.yFlip;
      scale = src.scale;
    }
}

public class UXCoordinator : MonoBehaviour
{
    [Header("OxOD Reference")]
    public FileDialog dialog;

    [Header("Related Resources")]
    public GameObject mainToolbar;
    public GameObject playButton;
    public GameObject pauseButton;
    public GameObject viewSettingsGameObject;
    public OvrAvatar ovrAvatar;
    public GameObject laserPointer;
    public MediaPlayer mediaPlayer;
    public ApplyToMaterial applyToMaterial;
    public VideoPlayer videoPlayer;
    public Slider cueSlider;
    public Text nameElement;
    public Text timeElement;
    public Text debugText;

    [Header("View Settings")]
    public ViewSettings viewSettings;

    [Header("UI Templates")]
    public GameObject toggleGroupTemplate;
    public GameObject toggleTemplate;
    public GameObject textTemplate;
    public GameObject sliderTemplate;

    VideoWrapper videoWrapper;
    bool isSelecting = false;
    string currentVideoPath = "";
    string currentSettingsPath = "";
    int loopMode = 0;
    double loopStart;
    double loopEnd;
    bool isWaitingForAMoment = false;
    string[] filenamesInSameFolder;
    bool showFps = false;

    string[] buttonNames = {
        "Fire1",  // bottom (right hand)
        "Fire2",  // top (right hand)
        "Fire3",  // bottom (left hand)
        "Jump",   // top (left hand)
    };
    string[] axisNames = {
        "Oculus_CrossPlatform_PrimaryThumbstickHorizontal",
        "Oculus_CrossPlatform_PrimaryThumbstickVertical",
        "Oculus_CrossPlatform_PrimaryIndexTrigger",
        "Oculus_CrossPlatform_PrimaryHandTrigger",
        "Oculus_CrossPlatform_SecondaryThumbstickHorizontal",
        "Oculus_CrossPlatform_SecondaryThumbstickVertical",
        "Oculus_CrossPlatform_SecondaryIndexTrigger",
        "Oculus_CrossPlatform_SecondaryHandTrigger",
    };
    string[] leftButtons = {
      "Fire3",
      "Jump",
    };
    string[] leftAxes = {
      "Oculus_CrossPlatform_PrimaryIndexTrigger",
      "Oculus_CrossPlatform_PrimaryHandTrigger",
    };
    string[] rightButtons = {
      "Fire1",
      "Fire2",
    };
    string[] rightAxes = {
      "Oculus_CrossPlatform_SecondaryIndexTrigger",
      "Oculus_CrossPlatform_SecondaryHandTrigger",
    };

    OVRCameraRig m_CameraRig;
    UnityEngine.EventSystems.OVRInputModule m_InputModule;

    System.Action<ProjectionType> updateProjectionTypeUI;
    System.Action<FieldOfView> updateFieldOfViewUI;
    System.Action<Layout> updateLayoutUI;
    System.Action<bool> updateFlipYUI;
    System.Action<float> updateScaleUI;

    // Start is called before the first frame update
    void Start()
    {
        m_CameraRig = FindObjectOfType<OVRCameraRig>();
        m_InputModule = FindObjectOfType<UnityEngine.EventSystems.OVRInputModule>();
        // videoWrapper = new VideoPlayerWrapper(videoPlayer);
        videoWrapper = new MediaPlayerWrapper(mediaPlayer, applyToMaterial);
        SetActiveController(OVRInput.Controller.LTouch);

        updateProjectionTypeUI = AddToggleGroup<ProjectionType>(viewSettingsGameObject, "Projection", viewSettings.SetProjectionType);
        updateFieldOfViewUI = AddToggleGroup<FieldOfView>(viewSettingsGameObject, "Field of View", viewSettings.SetFieldOfView);
        updateLayoutUI = AddToggleGroup<Layout>(viewSettingsGameObject, "Layout", viewSettings.SetLayout);
        updateFlipYUI = AddCheckbox(viewSettingsGameObject, "Flip Y", viewSettings.SetFlipY);
        updateScaleUI = AddSlider(viewSettingsGameObject, "Scale", viewSettings.SetScale, 0.25f, 2.0f);
        viewSettingsGameObject.SetActive(false);
        UpdateViewSettings(false);
    }

    void UpdateViewSettings(bool writeSettings = true)
    {
      videoWrapper.SetViewSettings(viewSettings);

      updateProjectionTypeUI(viewSettings.projectionType);
      updateFieldOfViewUI(viewSettings.fieldOfView);
      updateLayoutUI(viewSettings.layout);
      updateFlipYUI(viewSettings.yFlip);
      updateScaleUI(viewSettings.scale);

      if (writeSettings && currentSettingsPath.Length > 0) {
        File.WriteAllText(currentSettingsPath, DeJson.Serialize.From(viewSettings), System.Text.Encoding.UTF8);
      }
    }

    //void SetViewSettingsProjectType(ProjectionType pt) { viewSettings.projectionType = pt; }
    //void SetViewSettings
    System.Action<T> AddToggleGroup<T>(GameObject parent, string label, System.Action<T> fn)
    {
      {
        GameObject text = Instantiate<GameObject>(textTemplate, parent.transform);
        text.name = typeof(T).Name;
        text.GetComponent<Text>().text = label;
      }

      var toggles = new List<(T, Toggle)>();

      {
        GameObject group = Instantiate<GameObject>(toggleGroupTemplate, parent.transform);
        ToggleGroup toggleGroup = group.GetComponent<ToggleGroup>();

        bool first = true;
        foreach (T e in (T[])System.Enum.GetValues(typeof(T)))  {
          GameObject option = Instantiate<GameObject>(toggleTemplate, group.transform);
          option.name = e.ToString();

          Text text = option.GetComponentInChildren<Text>();
          text.text = e.ToString();

          Toggle toggle = option.GetComponent<Toggle>();
          toggles.Add((e, toggle));
          toggle.group = toggleGroup;
          toggle.isOn = first;
          first = false;
          toggle.onValueChanged.AddListener((bool t) => {
            Debug.Log("toogle clicked:" + toggle.isOn);
            fn(e);
            UpdateViewSettings();
          });

          /*
          Button button = option.GetComponentInChildren<Button>();
          button.onClick.AddListener(() => {
            Debug.Log("button clicked:" + toggle.isOn + ":" + e.ToString());
            toggle.SetIsOnWithoutNotify(true);
            fn(e);
            UpdateViewSettings();
            Debug.Log("after button clicked:" + toggle.isOn);
          });
          */
        }
      }

      return (T v) => {
        foreach(var tuple in toggles) {
          tuple.Item2.SetIsOnWithoutNotify(tuple.Item1.Equals(v));
        }
      };
    }

    System.Action<bool> AddCheckbox(GameObject parent, string label, System.Action<bool> fn)
    {
      {
        GameObject text = Instantiate<GameObject>(textTemplate, parent.transform);
        text.name = label;
        text.GetComponent<Text>().text = label;
      }
      {
        GameObject option = Instantiate<GameObject>(toggleTemplate, parent.transform);
        Text text = option.GetComponentInChildren<Text>();
        text.text = label;
        Toggle toggle = option.GetComponent<Toggle>();
        toggle.onValueChanged.AddListener((bool t) => {
          fn(t);
          UpdateViewSettings();
        });

        return (bool b) => {
          toggle.SetIsOnWithoutNotify(b);
        };
      }
    }

    System.Action<float> AddSlider(GameObject parent, string label, System.Action<float> fn, float min, float max)
    {
      {
        GameObject text = Instantiate<GameObject>(textTemplate, parent.transform);
        text.name = label;
        text.GetComponent<Text>().text = label;
      }
      {
        GameObject option = Instantiate<GameObject>(sliderTemplate, parent.transform);
        Slider slider = option.GetComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.onValueChanged.AddListener((float t) => {
          fn(t);
          UpdateViewSettings();
        });

        return (float f) => {
          slider.SetValueWithoutNotify(f);
        };
      }
    }

    bool ControllerActive(string[] buttons, string[] axes)
    {
        foreach (var buttonName in buttons)
        {
            if (Input.GetButtonDown(buttonName)) {
              return true;
            }
        }
        foreach (var axisName in axes)
        {
            if (Input.GetAxis(axisName) > 0.25) {
              return true;
            }
        }
        return false;
    }

    void DebugControllers()
    {
        StringBuilder sb = new StringBuilder();
        foreach (var buttonName in buttonNames)
        {
            sb.AppendLine(buttonName + ":" + Input.GetButton(buttonName));
        }
        foreach (var axisName in axisNames)
        {
            sb.AppendLine(axisName + "+" + Input.GetAxis(axisName));
        }
        debugText.text = sb.ToString();
    }

    string PadZero2(int value) {
      return value.ToString().PadLeft(2, '0');
    }

    void QueWithThumbsticks()
    {
      if (!isWaitingForAMoment) {
        float dx = Input.GetAxis("Oculus_CrossPlatform_PrimaryThumbstickHorizontal") + Input.GetAxis("Oculus_CrossPlatform_SecondaryThumbstickHorizontal");
        if (Mathf.Abs(dx) > 0.2f) {
          dx /= 0.8f;
          videoWrapper.time = (videoWrapper.time + dx * 20.0) % videoWrapper.length;
          isWaitingForAMoment = true;
          StartCoroutine(ClearIsWaitingForAMoment(1.0f));
        }
      }
    }

    void ProcessLooping()
    {
      if (loopMode == 2 && videoWrapper.time > loopEnd) {
        videoWrapper.time = loopStart;
      }
    }

    void UpdateTimeDisplay()
    {
      float currentLen = (float)videoWrapper.length;
      float currentTime = (float)videoWrapper.time;
      float cuePosition = currentLen > 0.0f
          ? currentTime / currentLen
          : 0.0f;
      //Debug.Log("time:" + currentTime + " len:" + currentLen + " cue:" + cuePosition);
      cueSlider.SetValueWithoutNotify(cuePosition);
      int hours = (int)(currentTime / 60.0f / 60.0f);
      int mins = (int)(currentTime / 60.0f) % 60;
      int secs = (int)(currentTime) % 60;
      timeElement.text = showFps
        ? ((int)(1.0f / Time.deltaTime)).ToString()
        : PadZero2(hours) + ":" + PadZero2(mins) + ":" + PadZero2(secs);
    }

    void Update()
    {
      //DebugControllers();
      if (OVRPlugin.shouldQuit) {
        CmdExit();
      }

      if (!isSelecting && videoWrapper.isPlaying) {
        QueWithThumbsticks();
        ProcessLooping();
      }

      UpdateTimeDisplay();

      if (!isSelecting && (Input.GetButtonDown("Fire2") || Input.GetButtonDown("Jump"))) {
        if (viewSettingsGameObject.activeSelf) {
          viewSettingsGameObject.SetActive(false);
          mainToolbar.SetActive(true);
        } else {
          ShowUI(!mainToolbar.activeSelf);
        }
      }

      if (ControllerActive(leftButtons, leftAxes)) {
          SetActiveController(OVRInput.Controller.LTouch);
      }
      if (ControllerActive(rightButtons, rightAxes)) {
          SetActiveController(OVRInput.Controller.RTouch);
      }
    }

    IEnumerator RestoreName(float waitTime) {
      yield return new WaitForSeconds(waitTime);
      nameElement.text = currentVideoPath;
    }

    IEnumerator ClearIsWaitingForAMoment(float waitTime) {
      yield return new WaitForSeconds(waitTime);
      isWaitingForAMoment = false;
    }

    public void CmdExit()
    {
      videoPlayer.Stop();
      #if UNITY_EDITOR
          // Application.Quit() does not work in the editor
          UnityEditor.EditorApplication.isPlaying = false;
      #else
          Application.Quit();
      #endif
    }

    public void CmdStop()
    {
      videoWrapper.Stop();
    }

    public void CmdPlay()
    {
      videoWrapper.Play();
      playButton.SetActive(false);
      pauseButton.SetActive(true);
    }

    public void CmdPause()
    {
      videoWrapper.Pause();
      playButton.SetActive(true);
      pauseButton.SetActive(false);
    }

    public void CmdPrev()
    {
      GoToPrevNext(-1);
    }

    public void CmdNext()
    {
      GoToPrevNext(+1);
    }

    void GoToPrevNext(int direction) {
      if (currentVideoPath.Length > 0) {
        string[] files = GetFileListForNextPrevious(currentVideoPath, videoWrapper.extensions);
        int ndx = System.Array.IndexOf<string>(files, Path.GetFileName(currentVideoPath));
        if (ndx >= 0) {
          ndx = (ndx + files.Length + direction) % files.Length;
          StartVideo(Path.Combine(Path.GetDirectoryName(currentVideoPath), files[ndx]));
        }
      }
    }

    public void CmdLoop()
    {
      switch (loopMode) {
        case 0:
          loopStart = videoWrapper.time;
          break;
        case 1:
          loopEnd = videoWrapper.time;
          break;
        case 2:
          loopMode = 0;
          break;
      }
      ++loopMode;
    }

    public void CmdSelectVideo()
    {
        mainToolbar.SetActive(false);
        isSelecting = true;
        StartCoroutine(SelectVideo(currentVideoPath));
    }

    public void CmdSelectMode()
    {
      mainToolbar.SetActive(false);
      viewSettingsGameObject.SetActive(true);
    }

    public void CmdSetPlayRate()
    {
      videoWrapper.playRate = videoWrapper.playRate == 1.0
         ? 0.5
         : 1.0;
    }

    public void CmdToggleInfo()
    {
      showFps = !showFps;
    }

    public void CueVideo()
    {
      videoWrapper.time = cueSlider.value * videoWrapper.length;
    }

    void ShowUI(bool show)
    {
        mainToolbar.SetActive(show);
        laserPointer.SetActive(show);
        ovrAvatar.ShowControllers(show);
        ovrAvatar.ShowFirstPerson = show;
    }

    public IEnumerator SelectVideo(string path)
    {
        string caption = "OPEN FILE";
        int maxSize = -1;
        bool saveLastPath = true;

        yield return StartCoroutine(dialog.Open(path, videoWrapper.extensions, caption, null, maxSize, saveLastPath));

        isSelecting = false;

        if (dialog.result != null)
        {
            loopMode = 0;
            StartVideo(dialog.result);
            ShowUI(false);
        } else {
          ShowUI(true);
        }
    }

    void StartVideo(string path) {
      currentVideoPath = path;
      currentSettingsPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + ".viewSettings");
      if (System.IO.File.Exists(currentSettingsPath)) {
        string json = File.ReadAllText(currentSettingsPath, System.Text.Encoding.UTF8);
        ViewSettings newViewSettings = DeJson.Deserialize.To<ViewSettings>(json);
        viewSettings.Copy(newViewSettings);
        UpdateViewSettings(false);
      } else {
        string name = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        if (name.Contains("over")) { viewSettings.layout = Layout.TopBottom; }
        if (name.Contains("bt")) { viewSettings.layout = Layout.BottomTop; }
        if (name.Contains("rl")) { viewSettings.layout = Layout.RightLeft; }
        if (name.Contains("lr")) { viewSettings.layout = Layout.LeftRight; }
        if (name.Contains("tb")) { viewSettings.layout = Layout.TopBottom; }
        if (name.Contains("sbs")) { viewSettings.layout = Layout.LeftRight; }
        if (name.Contains("360")) { viewSettings.fieldOfView = FieldOfView.View360; }
        if (name.Contains("half")) { viewSettings.fieldOfView = FieldOfView.View180; }
        if (name.Contains("180")) { viewSettings.fieldOfView = FieldOfView.View180; }
        if (name.Contains("360")) { viewSettings.fieldOfView = FieldOfView.View360; }
        if (name.Contains("f180")) { viewSettings.projectionType = ProjectionType.Fisheye; }
        if (name.Contains("vr180")) { viewSettings.projectionType = ProjectionType.Fisheye; }
        UpdateViewSettings();
      }
      videoWrapper.SetUrl("file://" + path.Replace('\\', '/'));
      nameElement.text = currentVideoPath;
      CmdPlay();
    }

    string[] GetFileListForNextPrevious(string filepath, string extensions)
    {
      extensions = extensions.ToLower();
      string[] allowedExtensions = extensions.Split('|');

      string dirPath = Path.GetDirectoryName(currentVideoPath);
      DirectoryInfo dir = new DirectoryInfo(dirPath);
      
      FileInfo[] info = dir.GetFiles().Where(f => !f.Name.StartsWith(".") && allowedExtensions.Contains(f.Extension.ToLower())).ToArray();
      return info.Select(v => v.Name).ToArray();
    }

    public void OnVideoEvent(MediaPlayer mp, MediaPlayerEvent.EventType et, ErrorCode errorCode)
    {
        if (errorCode != ErrorCode.None)  {
          Debug.LogError("ERROR:" + errorCode.ToString() + ":" + et.ToString());
        }
        switch (et)
        {
            case MediaPlayerEvent.EventType.ReadyToPlay:
                mp.Control.Play();
                break;
            case MediaPlayerEvent.EventType.FirstFrameReady:
                //Debug.Log("First frame ready");
                //Debug.Log("duration:" + mp.Info.GetDurationMs());
                break;
            case MediaPlayerEvent.EventType.FinishedPlaying:
                //mp.Control.Rewind();
                break;
        }
        //Debug.Log("Event: " + et.ToString());
    }

    void SetActiveController(OVRInput.Controller c)
    {
        if (c == OVRInput.Controller.LTouch) {
          m_InputModule.rayTransform = m_CameraRig.leftHandAnchor;
          m_InputModule.horizontalAxis = "Oculus_CrossPlatform_PrimaryThumbstickHorizontal";
          m_InputModule.verticalAxis = "Oculus_CrossPlatform_PrimaryThumbstickVertical";
          m_InputModule.submitButton = "Fire3";
          m_InputModule.cancelButton = "Jump";
          m_InputModule.useRightStickScroll = false;
        } else {
          m_InputModule.rayTransform = m_CameraRig.rightHandAnchor;
          m_InputModule.horizontalAxis = "Oculus_CrossPlatform_SecondaryThumbstickHorizontal";
          m_InputModule.verticalAxis = "Oculus_CrossPlatform_SecondaryThumbstickVertical";
          m_InputModule.submitButton = "Fire1";
          m_InputModule.cancelButton = "Fire2";
          m_InputModule.useRightStickScroll = true;
        }
    }
}

}  // namespace GMan