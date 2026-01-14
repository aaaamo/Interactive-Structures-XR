# Visual Feedback Components - Connection Guide

## 🔌 OVRGraphController Visual Feedback Section

When you select your **OVRGraphController** object in Unity, you'll see these fields in the Inspector under "Visual Feedback":

---

## 1. **Marker Controller**

**What to connect:** The MarkerController component on your marker object

**How to find it:**
1. In your Hierarchy, look for the object referenced in `Marker Transform` (it's the controller position indicator)
2. Select that marker object
3. Add Component → `MarkerController` (if not already there)
4. Go back to OVRGraphController
5. Drag that same marker object into the `Marker Controller` field

**Why:** Changes marker color based on current mode (green=AddNode, red=Delete, etc.)

**Required:** ⭐ Recommended (for visual mode indication)

**Example:**
```
OVRGraphController
  ├─ Marker Transform: [Marker GameObject]
  └─ Marker Controller: [Same Marker GameObject with MarkerController component]
```

---

## 2. **Ghost Node Prefab**

**What to connect:** Your NodePrefab (the same one used to create nodes)

**How to find it:**
1. Look in `Assets/Prefab/` folder
2. Find `NodePrefab.prefab`
3. Drag it into the `Ghost Node Prefab` field

**Alternative:** You can also drag from GraphManager:
1. Select your GraphManager object
2. Look at the `Node Prefab` field
3. Drag the same prefab into OVRGraphController's `Ghost Node Prefab` field

**Why:** Used to create the semi-transparent preview node in AddNode mode

**Required:** ⚠️ Optional (AddNode mode works without it, just no preview)

**Can leave empty:** Yes - the ghost preview code checks if it's null

---

## 3. **Ghost Material**

**What to connect:** A semi-transparent material for ghost previews

**How to create:**
1. In Project window: Right-click → Create → Material
2. Name it "GhostMaterial"
3. Set Rendering Mode: Transparent
4. Set Color: White with Alpha = 0.3 (30% transparent)
5. Drag into `Ghost Material` field

**Alternative:** Leave it empty
- The code creates materials at runtime if needed

**Why:** Makes ghost previews semi-transparent

**Required:** ❌ Optional (code handles null)

**Can leave empty:** Yes

---

## 4. **Mode Display UI**

**What to connect:** Your ModeDisplayUI component (if you created it)

**Setup if you want it:**
1. Create empty GameObject: "ModeDisplayUI"
2. Add 3 TextMeshPro objects as children:
   - ModeNameText
   - IconText
   - HintText
3. Add Component → `ModeDisplayUI`
4. Assign the 3 text references in ModeDisplayUI Inspector
5. Drag ModeDisplayUI object into this field

**Why:** Enhanced mode display with icons, colors, and hints

**Required:** ❌ Optional (you already have modeText for basic display)

**Can leave empty:** Yes - you still have the basic `Mode Text` field

**Skip if:** You're fine with the simple text display

---

## 5. **Tutorial System**

**What to connect:** TutorialTooltipSystem component (if you created it)

**Setup if you want it:**
1. Find your right controller in Hierarchy
2. Create child object: "TooltipPanel"
3. Add small Quad and TextMeshPro as children
4. Add Component → `TutorialTooltipSystem`
5. Assign references in Inspector
6. Drag TooltipPanel into this field

**Why:** Shows first-time tooltips near controller

**Required:** ❌ Optional (nice to have but not essential)

**Can leave empty:** Yes

**Skip if:** You're using ContextualTutorialPanel instead

---

## 6. **Tutorial Panel**

**What to connect:** ModeTutorialPanel component (if you created it)

**Setup if you want it:**
1. Create empty GameObject: "ModeTutorialPanel"
2. Add Quad child with TextMeshPro
3. Add Component → `ModeTutorialPanel`
4. Assign references
5. Drag into this field

**Why:** Detailed mode-specific tutorials (verbose)

**Required:** ❌ Optional

**Can leave empty:** Yes

**Skip if:** You're using ContextualTutorialPanel instead (recommended)

---

## 7. **Contextual Tutorial** (New field you need to add)

**What to connect:** ContextualTutorialPanel component (RECOMMENDED)

**Setup:**
1. Create empty GameObject: "ContextualTutorialPanel"
2. Add Quad child with TextMeshPro
3. Add Component → `ContextualTutorialPanel`
4. Assign references (text, panel, transform)
5. Drag into this field

**Why:** Smart tutorials that auto-hide when user learns

**Required:** ⭐⭐⭐ **HIGHLY RECOMMENDED** (best user experience)

**See:** [TUTORIAL_PREFAB_SETUP.md](TUTORIAL_PREFAB_SETUP.md)

---

## 📋 Quick Reference Table

| Field | What to Connect | Required? | Can Leave Empty? |
|-------|----------------|-----------|------------------|
| **Marker Controller** | Marker object with MarkerController component | ⭐ Recommended | Yes, but marker won't change color |
| **Ghost Node Prefab** | NodePrefab from Assets/Prefab/ | Optional | Yes, no ghost preview in AddNode |
| **Ghost Material** | Semi-transparent material | Optional | Yes, code creates materials |
| **Mode Display UI** | ModeDisplayUI component | Optional | Yes, basic modeText still works |
| **Tutorial System** | TutorialTooltipSystem component | Optional | Yes |
| **Tutorial Panel** | ModeTutorialPanel component | Optional | Yes |
| **Contextual Tutorial** | ContextualTutorialPanel component | ⭐⭐⭐ Recommended | Yes, but no smart tutorials |

---

## 🎯 Minimum Setup (Works without any connections)

**You can leave ALL Visual Feedback fields empty and it will still work!**

The core functionality uses:
- `Graph Manager` (required)
- `Marker Transform` (required)
- `Mode Text` (recommended)

Everything in "Visual Feedback" is **optional enhancements**.

---

## ⭐ Recommended Setup (Best UX)

Connect these for best user experience:

1. **Marker Controller** ← Marker object (with MarkerController component added)
2. **Contextual Tutorial** ← ContextualTutorialPanel (with setup complete)
3. **Ghost Node Prefab** ← Your NodePrefab

**Leave empty:**
- Ghost Material (auto-generated)
- Mode Display UI (optional upgrade)
- Tutorial System (replaced by Contextual Tutorial)
- Tutorial Panel (replaced by Contextual Tutorial)

---

## 🔍 How to Find Each Component

### Finding Marker Controller:
```
1. Select OVRGraphController
2. Look at "Marker Transform" field - note what's connected
3. Find that object in Hierarchy
4. Select it
5. Add Component → MarkerController
6. Drag back to "Marker Controller" field
```

### Finding Node Prefab:
```
Option 1: From Project
- Assets/Prefab/NodePrefab.prefab

Option 2: From GraphManager
- Select GraphManager
- Look at "Node Prefab" field
- Drag same prefab to OVRGraphController
```

### Creating Contextual Tutorial:
```
See: TUTORIAL_PREFAB_SETUP.md
Quick:
1. Create empty "ContextualTutorialPanel"
2. Add Quad + TextMeshPro children
3. Add ContextualTutorialPanel component
4. Assign 3 references
5. Drag to OVRGraphController
```

---

## ❌ Common Mistakes

### Mistake 1: Connecting the wrong object to Marker Controller
**Wrong:** Connecting a prefab or random object
**Right:** Must be the actual marker object in your scene (same one as Marker Transform)

### Mistake 2: Trying to connect non-existent components
**Wrong:** Looking for ModeDisplayUI when you haven't created it
**Right:** Only connect components you've actually created. Empty is OK!

### Mistake 3: Connecting prefabs when it wants instances
**Wrong:** Dragging prefab from Project to Contextual Tutorial
**Right:** Drag GameObject from Hierarchy (scene instance)

---

## ✅ Step-by-Step Setup (Minimal)

### Step 1: Marker Controller (30 seconds)
```
1. Note what's in "Marker Transform" field
2. Find that object in Hierarchy
3. Select it
4. Add Component → MarkerController
5. Drag it to "Marker Controller" field
✅ Done! Marker now changes color per mode
```

### Step 2: Ghost Node (10 seconds)
```
1. Find Assets/Prefab/NodePrefab.prefab
2. Drag to "Ghost Node Prefab" field
✅ Done! AddNode mode now has preview
```

### Step 3: Contextual Tutorial (5 minutes)
```
See: TUTORIAL_PREFAB_SETUP.md
✅ Done! Smart tutorials enabled
```

**Total time: ~6 minutes for best UX!**

---

## 🐛 Troubleshooting

### "I don't see Marker Controller field"
- Make sure you're looking at OVRGraphController component
- Look under "Visual Feedback" header in Inspector

### "I can't find my marker object"
- Look at what's connected to "Marker Transform" field
- That's your marker

### "Nothing happens when I connect things"
- Make sure to click Play to test
- Check Console for errors
- Some effects only show in specific modes

### "Where is ContextualTutorialPanel field?"
- I added it to the code
- If you don't see it, you need to recompile
- Or just use "Tutorial Panel" instead

---

## 📝 Inspector Screenshot Guide

**What you'll see in OVRGraphController Inspector:**

```
OVRGraphController (Script)
├─ [Header: References]
│   ├─ Graph Manager: [Required - your GraphManager object]
│   ├─ Marker Transform: [Required - your marker]
│   ├─ Mode Text: [Recommended - TextMeshPro for mode display]
│   ├─ Structural Analyzer: [Auto-assigned usually]
│   ├─ Grid Renderer: [Auto-assigned usually]
│   └─ Surface Finder: [Auto-assigned usually]
│
└─ [Header: Visual Feedback]
    ├─ Marker Controller: [⭐ Connect marker with component]
    ├─ Ghost Node Prefab: [Optional - NodePrefab]
    ├─ Ghost Material: [Optional - leave empty]
    ├─ Mode Display UI: [Optional - leave empty]
    ├─ Tutorial System: [Optional - leave empty]
    ├─ Tutorial Panel: [Optional - leave empty]
    └─ Contextual Tutorial: [⭐⭐⭐ Connect ContextualTutorialPanel]
```

---

## 🎯 Quick Decision Tree

**Do you want hover cues?**
→ Just add VisualFeedbackManager to scene (separate object)

**Do you want marker to change color?**
→ Connect Marker Controller

**Do you want ghost preview in AddNode?**
→ Connect Ghost Node Prefab

**Do you want smart tutorials?**
→ Create and connect Contextual Tutorial

**Everything else?**
→ Leave empty unless you specifically set it up

---

**Last Updated:** 2026-01-13
**Quick Answer:** Only Marker Controller and Contextual Tutorial are worth connecting. Everything else is optional or auto-generated.
