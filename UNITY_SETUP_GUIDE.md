# Unity Setup Guide - Step by Step

## 🎯 Quick Fix for Hover Cues

The hover cues should now work automatically! I've created a new `SimpleHighlight` component that is more reliable.

**To test immediately:**
1. Open your scene in Unity
2. Add an empty GameObject called "DebugTest"
3. Add the `DebugUITest` component to it
4. Play the scene
5. Create some nodes (AddNode mode)
6. Press **1** key to highlight all nodes in green
7. Press **2** key to clear highlights

If this works, the hover system is working! Now just need to set up components.

---

## 📋 Complete Setup Checklist

### STEP 1: Add VisualFeedbackManager (Required for Hover)

```
1. In Hierarchy, create empty GameObject: "VisualFeedbackManager"
2. In Inspector, click "Add Component"
3. Search for "VisualFeedbackManager"
4. Add it (no references needed, it's a singleton)
```

**That's it!** The hover cues should now work automatically.

---

### STEP 2: Add MarkerController (For Marker Color)

```
1. In Hierarchy, find your Marker object
   (It's referenced by OVRGraphController.markerTransform)
2. Select the Marker
3. In Inspector, click "Add Component"
4. Search for "MarkerController"
5. Add it
```

No configuration needed - it auto-sets colors per mode.

---

### STEP 3: Link VisualFeedbackManager to OVRGraphController (Optional)

The `VisualFeedbackManager` is a singleton, so it works automatically.
But if you want to verify:

```
1. Find your OVRGraphController GameObject in Hierarchy
2. Select it
3. In Inspector, you'll see the OVRGraphController component
4. It should already have references like:
   - Graph Manager
   - Marker Transform
   - Mode Text
   - etc.
```

**The visual feedback works automatically via Singleton pattern!**

---

### STEP 4: Setup Save/Load System (Optional)

#### A. Create SaveLoadManager

```
1. Hierarchy → Right-click → Create Empty
2. Name it "SaveLoadManager"
3. Add Component → "SaveLoadManager"
4. In Inspector, assign:
   - Graph Manager: Drag your GraphManager object
```

#### B. Create SaveLoadUI Panel

```
1. Hierarchy → Right-click → 3D Object → Quad
2. Name it "SaveLoadPanel"
3. Scale it: X=0.8, Y=0.6, Z=1
4. Position: X=0, Y=1.5, Z=2 (in front of camera)

5. Add 3 TextMeshPro objects as children:
   a) Create → 3D Object → Text - TextMeshPro
      - Name: "TitleText"
      - Font Size: 0.08
      - Alignment: Center

   b) Create another TextMeshPro
      - Name: "InstructionText"
      - Font Size: 0.04
      - Alignment: Left

   c) Create another TextMeshPro
      - Name: "FileListText"
      - Font Size: 0.04
      - Alignment: Left

6. Select SaveLoadPanel
7. Add Component → "SaveLoadUI"
8. Assign references in Inspector:
   - Save Load Manager: Drag SaveLoadManager object
   - Save Load Panel: Drag SaveLoadPanel itself
   - Panel Transform: Drag SaveLoadPanel Transform
   - Title Text: Drag TitleText
   - Instruction Text: Drag InstructionText
   - File List Text: Drag FileListText

9. Initially disable the panel:
   - Select SaveLoadPanel
   - Uncheck the box at top of Inspector
```

**Controls:**
- Press **Y button** (left controller) to toggle save/load panel

---

### STEP 5: Setup Tutorial System (Optional)

#### A. Welcome Tutorial Panel

```
1. Hierarchy → Create Empty → Name: "WelcomeTutorialPanel"
2. Add 3D → Quad as child
3. Scale quad: X=1.2, Y=0.8, Z=1
4. Add TextMeshPro as child of quad:
   - Name: "TutorialText"
   - Font Size: 0.05
   - Alignment: Center, Top
   - Text Wrapping: Enabled

5. Select WelcomeTutorialPanel
6. Add Component → "WelcomeTutorial"
7. Assign references:
   - Tutorial Text: Drag TutorialText
   - Tutorial Panel: Drag the Quad
   - Panel Transform: Drag WelcomeTutorialPanel transform

8. Initially enable it (will auto-hide after tutorial)
```

**Usage:**
- Tutorial shows automatically on first app start
- Press TRIGGER to advance steps
- 6 steps total

#### B. Mode Tutorial Panel (Persistent Help)

```
1. Hierarchy → Create Empty → Name: "ModeTutorialPanel"
2. Add 3D → Quad as child
3. Scale: X=1.0, Y=1.2, Z=1
4. Position: To the right of view (X=1.5, Y=1.5, Z=2)
5. Add TextMeshPro as child:
   - Name: "ModeTutorialText"
   - Font Size: 0.04
   - Text Wrapping: Enabled
   - Rich Text: Enabled

6. Select ModeTutorialPanel
7. Add Component → "ModeTutorialPanel"
8. Assign:
   - Tutorial Text: Drag ModeTutorialText
   - Tutorial Panel: Drag the Quad
   - Panel Transform: Drag ModeTutorialPanel transform

9. Link to OVRGraphController:
   - Select your OVRGraphController object
   - In Inspector, find "Tutorial Panel" field
   - Drag ModeTutorialPanel into it
```

**Controls:**
- Press **B button** (right controller) to toggle help panel
- Auto-updates when you change modes

#### C. Tooltip System (First-Time Tips)

```
1. Find your right controller object in Hierarchy
2. Create Empty as child → Name: "TooltipPanel"
3. Add small Quad as child
4. Scale: X=0.4, Y=0.2, Z=1
5. Position relative to controller: Y=0.1, Z=0.2
6. Add TextMeshPro as child:
   - Name: "TooltipText"
   - Font Size: 0.03

7. Select TooltipPanel parent
8. Add Component → "TutorialTooltipSystem"
9. Assign:
   - Tooltip Parent: The TooltipPanel transform
   - Tooltip Text: Drag TooltipText
   - Tooltip Background: Drag the Quad

10. Link to OVRGraphController:
    - Select OVRGraphController
    - Find "Tutorial System" field
    - Drag TooltipPanel into it
```

**Behavior:**
- Shows once per mode on first use
- Auto-dismisses after 5 seconds

---

### STEP 6: Setup Example Structures (Optional)

```
1. Hierarchy → Create Empty → Name: "ExampleStructures"
2. Add Component → "ExampleStructures"
3. Assign:
   - Save Load Manager: Drag SaveLoadManager

4. Load example bridge:
   - In code: exampleStructures.LoadSimpleBridge()
   - Or create UI button that calls it
```

---

## 🔧 Testing the Setup

### Test 1: Hover Cues
```
1. Play the scene
2. Switch to AddEdge mode (thumbstick left/right)
3. Point at a node
4. Node should glow/highlight
```

**If not working:**
- Check VisualFeedbackManager exists in scene
- Add DebugUITest component and press 1 key to test manually
- Check Console for errors

### Test 2: Marker Color
```
1. Play the scene
2. Switch modes with thumbstick
3. Marker should change color:
   - AddNode: Green
   - Delete: Red
   - AddEdge: Blue
   etc.
```

**If not working:**
- Check MarkerController is on Marker object
- Check Marker has a Renderer component

### Test 3: Save/Load
```
1. Build a structure
2. Press Y button (left controller)
3. Press Left Trigger to save
4. Delete structure manually
5. Press Y button
6. Press X to switch to Load mode
7. Navigate with thumbstick
8. Press Trigger to load
```

### Test 4: Tutorials
```
1. Start fresh scene
2. Welcome tutorial should appear
3. Press Trigger to advance
4. After completion, press B button
5. Mode-specific help should appear
6. Change modes - help updates
```

---

## 🐛 Common Issues

### Issue: "Hover cues don't work"

**Solution:**
1. Add VisualFeedbackManager to scene (empty GameObject)
2. Make sure nodes have Renderer components
3. Use DebugUITest to test manually (press 1 key)

### Issue: "Tutorials don't show"

**Solution:**
1. Make sure tutorial panels are created as 3D objects (Quads)
2. Check TextMeshPro is assigned
3. Verify components are added
4. Check panel is enabled in Hierarchy

### Issue: "Marker doesn't change color"

**Solution:**
1. Add MarkerController component to Marker object
2. Check Marker has a Renderer with Material
3. Material must have "_Color" property

### Issue: "Save/Load panel doesn't appear"

**Solution:**
1. Press Y button on LEFT controller (not right)
2. Check SaveLoadUI component exists
3. Check panel references are assigned
4. Initially disable the panel in Inspector

---

## 📊 Required Components Summary

| Component | Required? | Purpose |
|-----------|-----------|---------|
| VisualFeedbackManager | **YES** (for hover) | Singleton, manages all highlights |
| MarkerController | Recommended | Colors marker per mode |
| SimpleHighlight | Auto-added | Added to objects automatically |
| SaveLoadManager | Optional | Save/load structures |
| SaveLoadUI | Optional | VR interface for save/load |
| WelcomeTutorial | Optional | One-time startup tutorial |
| ModeTutorialPanel | Optional | Persistent help panel |
| TutorialTooltipSystem | Optional | First-time tooltips |
| ExampleStructures | Optional | Pre-made structures |
| DebugUITest | For testing | Debug helper |

---

## 🎮 Full VR Controls Reference

**Right Controller:**
- Thumbstick Left/Right: Change modes
- Trigger: Primary action (place, select, etc.)
- Grip (Analyze): Cancel analysis
- A Button: Re-detect table surface
- B Button: Toggle mode help panel

**Left Controller:**
- Y Button: Toggle save/load panel
- X Button: Switch save/load mode
- Thumbstick: Navigate save/load list, adjust grid
- Trigger: Confirm save/load
- Grip: Cancel save/load panel

---

## 📝 Quick Start (Minimal Setup)

**For hover cues only:**
```
1. Create empty GameObject: "VisualFeedbackManager"
2. Add VisualFeedbackManager component
3. Done!
```

**For marker colors:**
```
1. Select Marker object
2. Add MarkerController component
3. Done!
```

**Everything else is optional!**

---

## 🚀 Advanced: Prefab Setup

If you want to create prefabs:

```
1. Set up all components as above
2. Drag GameObjects to Project window to create prefabs
3. In future scenes, just drag prefabs into Hierarchy
```

---

## 📞 Still Not Working?

**Debug Steps:**
1. Add DebugUITest component to scene
2. Play scene
3. Press **4** key to check all components
4. Check Console for debug output
5. Press **5** to create test structure
6. Press **1** to test highlighting manually

**Check Console for:**
- "[DEBUG] Component check" messages
- Error messages (red)
- Warning messages (yellow)

---

**Document Version:** 1.0
**Last Updated:** 2026-01-13
**Quick Fix:** Just add VisualFeedbackManager to scene for hover cues!
