# VR Video Player

A no frills VR video player.

[Download Here](https://github.com/greggman/vrvp/releases)

I don't trust all the commercial video players not to have metrics, analytics,
and other kinds of spyware in them. AFAIK not a single one explicitly states they don't spy on you.

And so, this basic VR video player.

# Instructions

Press B or Y to make one controller appear or the other. Only one controller appears at a time. Clicking again on the same controller will make it disappear.

Click the video icon (it's a rounded rectangle with triangle inside similar to the youtube logo). This will bring up the file open dialog.

Select a video.

Click the "mode" button and select the options appropriate for your video.

Other buttons:

* ⏪/⏩ jump forward and backward. Pressing the stick on either controller left or right will also jump forward and back
* 🔁️ will set a loop. one click sets the start, second click sets the end, 3rd click clears the loop
* `>`/`<` jump to the next/previous video in the current folder
* the line at the bottom cues the video

Basically. that's it!

# Development

Unfortunately I can't make it entirely open source (yet?) because it uses 2 commercial plugins. 

1. [Unity UI: OxOD Open/Save File Dialog System](https://assetstore.unity.com/packages/tools/gui/unity-ui-oxod-open-save-file-dialog-system-69720)
2. [AVPro Video (Windows)](https://assetstore.unity.com/packages/tools/video/avpro-video-windows-57969)

If I have time I can look into ways to replace the file dialog and make AVPro Video an external dependency.

I have no idea if this would work but you can try

1. Create a new project in Unity
2. Enable XR and the Oculus XR support
3. Import the [Oculus Integration](https://assetstore.unity.com/packages/tools/integration/oculus-integration-82022)
4. Import the [AVPro Plugin](https://assetstore.unity.com/packages/tools/video/avpro-video-windows-57969)
5. Import the [OxOD Open/Save File Dialog System](https://assetstore.unity.com/packages/tools/gui/unity-ui-oxod-open-save-file-dialog-system-69720)
6. Save and exit Unity
7. Copy all the files in this repo on top of the project you just created
8. Pray
9. Open the project in Unity

I have not tested if that will work. The repo here is basically my `Assets` folder minus 
`Assets/AVProVideo`, `Assets/Oculus`, `Assets/OxOD`, `Assets/Plugins`

# TODO

* Add support for flat videos

  currently there is no support for flat 2D or flat 3D videos. I'm just lazy because I can already use VLC
  or [other](https://mopho-v.org) in VR for flat videos and I don't have any 3D flat videos.

* Add support for moving the POV

  As it is you can press the system button on your right Oculus controller and "Reset View" but that only
  changes the direction. I'd like to also be able to change the orientation as in like tilt the video up
  or rotate it. If I add it I'd probably make it so you squeeze the the trigger or the grip or either controller and
  drag to orient. Would probably need a reset button. Maybe one could appear and 5 seconds after dragging
  as a way not to need a special button on the toolbar

* Replace OxOD

  That dialog is really designed for desktop and mouse.

* Make the cue bar better/bigger

  As it is it's small, easy to miss, easy to hit one of the buttons above. Not sure what the fix is
  but it works

# License

The code here is [MIT](LICENSE.md)