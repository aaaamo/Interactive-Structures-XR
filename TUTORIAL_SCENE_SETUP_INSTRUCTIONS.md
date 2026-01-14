# Tutorial System Setup - Your Scene

## Quick Fix: Add Tutorial Systems to SampleScene

You need to add both tutorial components to your Unity scene. Follow these steps:

---

## Part 1: WelcomeTutorial Setup (10 minutes)

### Step 1: Create WelcomeTutorial GameObject

1. In Unity Hierarchy, right-click → **Create Empty**
2. Name it: `WelcomeTutorial`
3. Position: X=0, Y=0, Z=0

### Step 2: Create the Visual Panel

1. Right-click `WelcomeTutorial` → **3D Object → Quad**
2. Name it: `WelcomeTutorialPanel`
3. Transform:
   - Scale: X=1.2, Y=0.8, Z=1
   - Position: X=0, Y=0, Z=0

### Step 3: Create Background Material (Optional but Recommended)

1. In Project window: Right-click → **Create → Material**
2. Name it: `WelcomeTutorialMaterial`
3. Set color: Black (R=0, G=0, B=0) with Alpha=0.9
4. Drag material onto `WelcomeTutorialPanel` quad

### Step 4: Add Tutorial Text

1. Right-click `WelcomeTutorialPanel` → **3D Object → Text - TextMeshPro**
   - If prompted to import TMP Essentials, click "Import"
2. Name it: `WelcomeTutorialText`
3. Configure TextMeshPro component:
   - **Font Size:** 0.06
   - **Alignment:** Center (horizontal and vertical)
   - **Wrapping:** Enabled
   - **Overflow:** Truncate
   - **Rich Text:** ✓ Enabled (IMPORTANT!)
   - **Width:** 1.1
   - **Height:** 0.7
4. Transform position: X=0, Y=0, Z=-0.01 (slightly in front of panel)

### Step 5: Add WelcomeTutorial Component

1. Select `WelcomeTutorial` (the parent GameObject)
2. In Inspector: **Add Component**
3. Search for `WelcomeTutorial` and add it
4. Assign references:
   - **Tutorial Text:** Drag `WelcomeTutorialText` here
   - **Tutorial Panel:** Drag `WelcomeTutorialPanel` here
   - **Panel Transform:** Drag `WelcomeTutorial` itself here
5. Settings:
   - **Step Duration:** 6
   - **Show On Start:** ✓ Checked
   - **Panel Offset:** X=0, Y=0, Z=1.5

### Step 6: Initial State

1. Select `WelcomeTutorial` in Hierarchy - **Make sure it's ENABLED** (checkbox at top of Inspector)
2. Select `WelcomeTutorialPanel` child - Initially enabled is fine (script controls visibility)

---

## Part 2: ContextualTutorialPanel Setup (10 minutes)

### Step 1: Create ContextualTutorialPanel GameObject

1. In Unity Hierarchy, right-click → **Create Empty**
2. Name it: `ContextualTutorialPanel`
3. Position: X=0, Y=0, Z=0

### Step 2: Create the Panel Background

1. Right-click `ContextualTutorialPanel` → **3D Object → Quad**
2. Name it: `ContextualPanelBackground`
3. Transform:
   - Scale: X=0.5, Y=0.4, Z=1
   - Position: X=0, Y=0, Z=0

### Step 3: Create Background Material (Optional)

1. In Project window: Right-click → **Create → Material**
2. Name it: `ContextualPanelMaterial`
3. Set color: Dark Gray (R=0.1, G=0.1, B=0.1) with Alpha=0.85
4. Drag material onto `ContextualPanelBackground` quad

### Step 4: Add Contextual Text

1. Right-click `ContextualPanelBackground` → **3D Object → Text - TextMeshPro**
2. Name it: `ContextualTutorialText`
3. Configure TextMeshPro component:
   - **Font Size:** 0.04
   - **Alignment:** Center, Top
   - **Wrapping:** Enabled
   - **Overflow:** Truncate
   - **Rich Text:** ✓ Enabled (IMPORTANT!)
   - **Width:** 0.45
   - **Height:** 0.35
4. Transform position: X=0, Y=0, Z=-0.01

### Step 5: Add ContextualTutorialPanel Component

1. Select `ContextualTutorialPanel` (the parent GameObject)
2. In Inspector: **Add Component**
3. Search for `ContextualTutorialPanel` and add it
4. Assign references:
   - **Tutorial Text:** Drag `ContextualTutorialText` here
   - **Panel Object:** Drag `ContextualPanelBackground` here
   - **Panel Transform:** Drag `ContextualTutorialPanel` itself here
5. Settings:
   - **Show Tutorials:** ✓ Checked
   - **Auto Hide Delay:** 10
   - **Actions Before Auto Hide:** 3
   - **Panel Offset:** X=0.6, Y=0.2, Z=0.8

### Step 6: Initial State

1. Select `ContextualTutorialPanel` - **Make sure it's ENABLED**
2. Select `ContextualPanelBackground` - **UNCHECK** to hide initially (script shows/hides it)

---

## Part 3: Link to OVRGraphController

### Find OVRGraphController

Your OVRGraphController exists as a standalone GameObject in the scene (not under OVRCameraRig).

1. In Hierarchy, look for a GameObject with the `OVRGraphController` component
2. It should be at the root level of your scene hierarchy

### Link ContextualTutorialPanel

1. Select the GameObject with `OVRGraphController`
2. In the Inspector, find the `OVRGraphController` component
3. Scroll down to the **"Visual Feedback"** section
4. Look for a field called **"Contextual Tutorial"**
5. Drag `ContextualTutorialPanel` from Hierarchy into that field

---

## Part 4: Test It

### Test WelcomeTutorial

1. Press **Play** in Unity
2. You should see the welcome tutorial panel appear in front of you
3. Press the **Right Trigger** to advance through steps
4. Should show 6 tutorial steps

### Test ContextualTutorialPanel

1. After welcome tutorial finishes
2. Switch modes with **Right Thumbstick Left/Right**
3. First time in each mode, you should see contextual help on the right side
4. Perform 3 actions in that mode - tutorial should auto-hide
5. Press **B Button** to show/hide tutorial manually

---

## Troubleshooting

### WelcomeTutorial doesn't show

Check:
- [ ] `WelcomeTutorial` GameObject is **enabled** in Hierarchy
- [ ] `Show On Start` is checked in component
- [ ] All 3 references are assigned (tutorialText, tutorialPanel, panelTransform)
- [ ] TextMeshPro has **Rich Text** enabled
- [ ] Camera.main exists in your scene

### ContextualTutorialPanel doesn't show

Check:
- [ ] `ContextualTutorialPanel` GameObject is **enabled**
- [ ] Panel Object reference assigned
- [ ] Linked to OVRGraphController
- [ ] `Show Tutorials` is checked
- [ ] Panel background starts **disabled** (unchecked)

### Tutorial shows but no text

Check:
- [ ] Tutorial Text reference assigned
- [ ] TextMeshPro component has **Rich Text** enabled
- [ ] Font size is not too small (0.04-0.06)
- [ ] Text width/height are reasonable

### Tutorial in wrong position

- Adjust `Panel Offset` values in Inspector
- For WelcomeTutorial: Try Z=1.5 to 2.5
- For ContextualTutorial: Try X=0.5-0.7, Y=0.1-0.3, Z=0.6-1.0

---

## Scene Hierarchy After Setup

```
SampleScene
├── Main Camera
├── OVRCameraRig
├── OVRGraphController                 ← Has OVRGraphController component
├── GraphManager
├── StructuralAnalyzer
├── VisualFeedbackManager
├── WelcomeTutorial                    ← NEW!
│   └── WelcomeTutorialPanel (Quad)
│       └── WelcomeTutorialText (TextMeshPro)
└── ContextualTutorialPanel            ← NEW!
    └── ContextualPanelBackground (Quad)
        └── ContextualTutorialText (TextMeshPro)
```

---

## Quick Checklist

### WelcomeTutorial ✓
- [ ] GameObject created and enabled
- [ ] Quad panel created (1.2 x 0.8)
- [ ] TextMeshPro added with Rich Text enabled
- [ ] WelcomeTutorial component added
- [ ] All 3 references assigned
- [ ] Show On Start checked

### ContextualTutorialPanel ✓
- [ ] GameObject created and enabled
- [ ] Quad panel created (0.5 x 0.4)
- [ ] Panel starts disabled
- [ ] TextMeshPro added with Rich Text enabled
- [ ] ContextualTutorialPanel component added
- [ ] All 3 references assigned
- [ ] Linked to OVRGraphController

---

**Estimated Setup Time:** 20 minutes total
**Difficulty:** Easy - No coding required!

Once set up, save your scene and test in VR!
