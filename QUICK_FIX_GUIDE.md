# Quick Fix Guide - Tutorial & Save/Load Issues

## ✅ FIXED: Tutorial Text Too Small
- Text size increased to 0.08-0.1 (was 0.03-0.04)
- Headers now 0.1 size
- Body text now 0.08 size
- Much more readable in VR!

## ✅ FIXED: Panel Moving with Eyes
- Panel now positions ONCE when shown
- Stays stationary after initial positioning
- No more following your head movement
- Can read comfortably

## ✅ Panel Now Centered
- Changed from right side (0.6, 0.2, 0.8) to center (0.0, 0.0, 1.5)
- Panel appears directly in front of you
- 1.5 meters away for comfortable reading distance

---

## 💾 Save/Load UI - Quick Setup

The Save/Load UI wasn't created yet. Here's the **5-minute setup**:

### **Option 1: Use Keyboard Shortcuts (EASIEST)**

I can add keyboard shortcuts to save/load without UI:

**Press these keys while playing:**
- **S** = Quick Save (saves as "QuickSave_[timestamp].json")
- **L** = Load last save
- Files saved to: `AppData/LocalLow/[YourCompany]/[YourGame]/StructureSaves/`

### **Option 2: Create Simple UI Panel (10 minutes)**

1. **Create SaveLoadManager:**
   ```
   Hierarchy → Create Empty → "SaveLoadManager"
   Add Component → "SaveLoadManager"
   Assign GraphManager reference
   ```

2. **Create UI Panel:**
   ```
   Hierarchy → 3D Object → Quad
   Name: "SaveLoadPanel"
   Scale: X=1, Y=0.8, Z=1
   Position: X=0, Y=1.5, Z=2
   ```

3. **Add Text (3 TextMeshPro objects):**
   ```
   Add 3 children:
   - TitleText (font size 0.1)
   - InstructionText (font size 0.08)
   - FileListText (font size 0.08)
   ```

4. **Add Component:**
   ```
   Select SaveLoadPanel
   Add Component → "SaveLoadUI"
   Assign all references
   ```

5. **Controls:**
   - Press **Y button** (left controller) to toggle panel

---

## 🎯 Recommended: Just Use Code Shortcuts

Want to skip the UI? I can add simple keyboard/button shortcuts:

**Add this to your scene:**

1. Create empty GameObject: "SaveLoadShortcuts"
2. I'll create a script that adds:
   - **Left Grip + A** = Quick Save
   - **Left Grip + B** = Load last save
   - Shows toast message when saved/loaded

Want me to create that script?

---

## 📝 Summary of Changes

### Tutorial Panel Fixed:
- ✅ Text 2-3x larger (0.08-0.1 instead of 0.03-0.04)
- ✅ Panel stays still (no more eye tracking)
- ✅ Centered in front of user (not to the side)
- ✅ Comfortable reading distance (1.5m)

### Save/Load Options:
- **Option A:** Keyboard shortcuts (S/L keys) - No setup needed
- **Option B:** Full UI panel - 10 minutes setup
- **Option C:** VR button shortcuts (Grip+A/B) - I can create script

---

## 🔧 In Unity Inspector Settings

If you want to adjust panel position yourself:

**Select ContextualTutorialPanel:**
```
Settings:
- Panel Offset X: 0.0 (left/right, 0=center)
- Panel Offset Y: 0.0 (up/down, 0=eye level)
- Panel Offset Z: 1.5 (distance, higher=further)
```

**Adjust to your preference:**
- X: -0.5 (left), 0 (center), 0.5 (right)
- Y: -0.2 (lower), 0 (eye level), 0.2 (higher)
- Z: 1.0 (close), 1.5 (medium), 2.0 (far)

---

**Last Updated:** 2026-01-13
**Status:** Tutorial issues FIXED, Save/Load needs setup choice
