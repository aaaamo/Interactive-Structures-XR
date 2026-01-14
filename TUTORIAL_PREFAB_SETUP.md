# Smart Tutorial Panel - Prefab Setup Guide

## 🎯 What This Does

The **ContextualTutorialPanel** is a smart tutorial system that:
- ✅ Shows only when user enters a new mode (first time)
- ✅ Auto-hides after 3 successful actions (user learned it)
- ✅ Auto-hides after 10 seconds (timeout)
- ✅ Can be toggled with B button anytime
- ✅ Positions to right side of view automatically
- ✅ Concise, one-screen instructions

**This replaces the complex multi-step tutorials with smart contextual help!**

---

## 📦 Quick Setup (5 Minutes)

### Step 1: Create the Panel GameObject

```
1. In Unity Hierarchy: Right-click → Create Empty
2. Name it: "ContextualTutorialPanel"
3. Position: X=0, Y=0, Z=0 (doesn't matter, auto-positions)
```

### Step 2: Create the Visual Panel

```
1. Right-click ContextualTutorialPanel → 3D Object → Quad
2. Name the quad: "PanelBackground"
3. Scale it: X=0.5, Y=0.4, Z=1
4. (Optional) Change material color to semi-transparent black
```

### Step 3: Add the Text

```
1. Right-click PanelBackground → 3D Object → Text - TextMeshPro
2. Name it: "TutorialText"
3. Configure TextMeshPro:
   - Font Size: 0.04
   - Alignment: Center, Top
   - Text Wrapping: Enabled
   - Rich Text: Enabled (important!)
   - Overflow: Truncate
   - Width: 0.45
   - Height: 0.35
4. Position relative to quad: Z=-0.01 (slightly in front)
```

### Step 4: Add the Component

```
1. Select "ContextualTutorialPanel" (parent)
2. Add Component → Search "ContextualTutorialPanel"
3. Assign references in Inspector:
   - Tutorial Text: Drag "TutorialText"
   - Panel Object: Drag "PanelBackground"
   - Panel Transform: Drag "ContextualTutorialPanel" itself
4. Settings (leave default):
   - Show Tutorials: ✓ (checked)
   - Auto Hide Delay: 10
   - Panel Offset: X=0.6, Y=0.2, Z=0.8
```

### Step 5: Link to OVRGraphController

```
1. Find your OVRGraphController object in Hierarchy
2. Select it
3. In Inspector, find the "Contextual Tutorial" field
4. Drag "ContextualTutorialPanel" into that field
```

### Step 6: Initial State

```
1. Select "ContextualTutorialPanel" in Hierarchy
2. Check the box at top of Inspector to enable it
3. Select "PanelBackground" child
4. **Uncheck** the box to hide it initially
   (Script will show/hide automatically)
```

---

## ✅ You're Done!

**Test it:**
1. Press Play
2. Switch modes with thumbstick
3. Tutorial appears first time in each mode
4. Do 3 actions in that mode
5. Tutorial auto-hides
6. Press B button to show again

---

## 🎨 Optional: Make it Look Better

### Add Background Color

```
1. Select "PanelBackground" quad
2. Create new Material: Assets → Create → Material
3. Name it "TutorialPanelMat"
4. Set color: Black (0,0,0) with Alpha 0.8
5. Drag material onto quad
```

### Add Border

```
1. Duplicate the quad (Ctrl+D)
2. Name it "PanelBorder"
3. Scale slightly larger: X=0.52, Y=0.42
4. Position: Z=0.01 (behind main quad)
5. Change color to white or yellow
```

### Add Icon

```
1. Add small quad as child: "IconQuad"
2. Scale: 0.08 x 0.08
3. Position: Top-left corner
4. Add sprite/texture for mode icon
```

---

## 🎮 How It Works

### First Time in Mode
```
User switches to AddNode mode
  ↓
Panel appears on right side
  ↓
Shows: "TRIGGER to place nodes"
  ↓
User places 3 nodes
  ↓
Panel auto-hides (user learned it!)
```

### Returning to Known Mode
```
User switches back to AddNode
  ↓
Panel DOESN'T show (already learned)
  ↓
User can press B button if they forgot
  ↓
Panel shows again
```

### Auto-Hide Timeout
```
Panel shows
  ↓
User reads but doesn't act
  ↓
After 10 seconds: Panel auto-hides
  ↓
User can press B to show again
```

---

## 🔧 Customization

### Change Tutorial Text

Edit `ContextualTutorialPanel.cs`:
```csharp
string GetTutorialForMode(OVRGraphController.Mode mode)
{
    switch (mode)
    {
        case OVRGraphController.Mode.AddNode:
            return @"YOUR CUSTOM TEXT HERE";
```

### Change Actions Before Hide

Edit line in `ContextualTutorialPanel.cs`:
```csharp
private const int ACTIONS_BEFORE_HIDE = 3; // Change to 5, 10, etc.
```

### Change Auto-Hide Delay

In Unity Inspector:
```
Auto Hide Delay: 15 (or any seconds)
```

### Change Position

In Unity Inspector:
```
Panel Offset:
  X: 0.6 (right/left)
  Y: 0.2 (up/down)
  Z: 0.8 (forward/back)
```

---

## 💡 Advanced: Create Prefab

Once set up:

```
1. Drag "ContextualTutorialPanel" from Hierarchy to Project window
2. Saves as prefab
3. In future scenes: Drag prefab into Hierarchy
4. Link to OVRGraphController
5. Done!
```

---

## 🐛 Troubleshooting

### "Tutorial doesn't show"

Check:
- [ ] ContextualTutorialPanel enabled in Hierarchy
- [ ] Panel Object assigned in Inspector
- [ ] Linked to OVRGraphController
- [ ] Show Tutorials is checked

### "Tutorial shows but no text"

Check:
- [ ] Tutorial Text assigned
- [ ] TextMeshPro Rich Text enabled
- [ ] Font size not too small (0.04+)

### "Tutorial in wrong position"

Check:
- [ ] Camera.main exists
- [ ] Panel Offset values
- [ ] Try: X=0.6, Y=0.2, Z=0.8

### "Tutorial won't hide"

Check:
- [ ] OnActionPerformed() is being called
- [ ] Check Console for errors
- [ ] Try pressing B button to hide manually

---

## 📊 What Each Mode Shows

| Mode | Tutorial Text |
|------|--------------|
| AddNode | "TRIGGER to place nodes / Nodes snap to grid" |
| AddEdge | "1. TRIGGER on first / 2. TRIGGER on second" |
| AddLoad | "1. TRIGGER to select / 2. Point direction / 3. TRIGGER to confirm" |
| ToggleSupport | "TRIGGER on node to fix/unfix / Need supports for stability" |
| Move | "Hold TRIGGER to drag / Release to drop" |
| Delete | "TRIGGER to delete / ⚠️ No undo!" |
| Grab | "Cyan = whole structure / Hold TRIGGER to move all" |
| Analyze | "TRIGGER = Run / GRIP = Cancel / Red = Tension, Blue = Compression" |
| Grid | "TRIGGER = Set anchors / Left stick = Adjust" |

---

## 🚀 Why This is Better

### Old System (WelcomeTutorial)
- ❌ 6-step sequence on startup
- ❌ Blocks user from starting
- ❌ User forgets by the time they use it
- ❌ Can't skip individual modes

### New System (ContextualTutorialPanel)
- ✅ Shows only when needed
- ✅ Learns user's skill level
- ✅ Auto-hides when user demonstrates understanding
- ✅ Always accessible with B button
- ✅ Non-intrusive (to the side)
- ✅ Mode-specific help

---

## 📝 Complete Hierarchy Example

```
Scene
├── Main Camera
├── OVRCameraRig
├── OVRGraphController
├── GraphManager
├── VisualFeedbackManager
└── ContextualTutorialPanel          ← New!
    └── PanelBackground (Quad)
        ├── PanelBorder (Quad, optional)
        └── TutorialText (TextMeshPro)
```

---

## 🎯 Quick Summary

**Minimum Setup:**
1. Create empty "ContextualTutorialPanel"
2. Add Quad child with TextMeshPro
3. Add ContextualTutorialPanel component
4. Assign 3 references
5. Link to OVRGraphController
6. Done in 5 minutes!

**Behavior:**
- Shows first time in each mode
- Hides after 3 actions OR 10 seconds
- B button to toggle anytime
- Floats to right side of view

---

**Document Version:** 1.0
**Last Updated:** 2026-01-13
**Setup Time:** ~5 minutes
**User Experience:** ⭐⭐⭐⭐⭐
