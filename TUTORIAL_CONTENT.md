# Tutorial Content - All Modes

This document contains all the written tutorials that users will see in the app.

---

## 📘 MODE 1: ADD NODE

**PURPOSE:**
Create structural joints (nodes) where members will connect.
Nodes are the foundation of your truss structure.

**HOW TO USE:**
1. Point your controller where you want to place a node
2. The green ghost preview shows where it will snap
3. Press TRIGGER to place the node
4. A label will show the node's coordinates (X, Y, Z)

**TIPS:**
• Nodes automatically snap to the grid for precision
• The grid helps you build straight, aligned structures
• You can adjust grid spacing in Grid mode
• Place nodes at corners and connection points

**NEXT STEP:**
After placing nodes, switch to AddEdge mode to connect them!

---

## 📘 MODE 2: ADD EDGE

**PURPOSE:**
Connect two nodes with structural members (beams, struts).
Edges carry forces between nodes in your structure.

**HOW TO USE:**
1. Point at the FIRST node you want to connect
2. When it glows green, press TRIGGER
3. The node will stay highlighted
4. Point at the SECOND node (will glow blue)
5. Press TRIGGER again to create the edge
6. A blue preview line shows the connection

**VALIDATION:**
• If the line turns RED, that edge already exists
• You'll feel an error vibration and see an error message
• Cannot create duplicate edges between the same nodes

**TIPS:**
• Build triangular patterns for stable structures
• Connect all nodes that should share forces
• You can see the edge as a cylinder between nodes

**NEXT STEP:**
Add forces with AddLoad mode!

---

## 📘 MODE 3: ADD LOAD

**PURPOSE:**
Apply forces (loads) to nodes. These represent weights,
external forces, or any load your structure must support.

**HOW TO USE:**
1. Point at the node where you want to apply force
2. When it glows orange, press TRIGGER
3. The node will stay highlighted
4. Move your controller to set the force direction
5. An orange arrow shows the force direction
6. The arrow's LENGTH = force MAGNITUDE
7. Press TRIGGER again to confirm

**UNDERSTANDING MAGNITUDE:**
• Distance from node to cursor = force strength
• Pull away from node = larger force
• The arrow will scale to show the magnitude
• Units are in Newtons (N)

**TIPS:**
• Point downward for gravity/weight loads
• You can add multiple loads to one node
• Each node shows how many loads it has
• Typical loads: 100N - 10,000N

**NEXT STEP:**
Fix nodes in place with ToggleSupport mode!

---

## 📘 MODE 4: TOGGLE SUPPORT

**PURPOSE:**
Fix nodes in place so they cannot move. These are the
anchor points or foundations of your structure.

**HOW TO USE:**
1. Point at any node
2. When it glows yellow, press TRIGGER
3. The node switches between FREE and FIXED

**VISUAL STATES:**
• FREE nodes: Can move under loads
• SUPPORT nodes: Fixed in place (different visual)
• Toggle back and forth as needed

**WHY SUPPORTS MATTER:**
• Structures need supports to be stable
• Without supports, structure will collapse under load
• Supports provide 'reaction forces' to balance loads
• Think of them as bolts to the floor/wall

**TIPS:**
• Typical: Fix 2-3 nodes as supports
• Fix nodes at the base of your structure
• Too many supports = overconstrained (won't move)
• Too few supports = unstable (will collapse)

**STRUCTURAL RULE:**
For a stable 3D truss: **members + supports ≥ 3 × nodes**

**NEXT STEP:**
Analyze your structure with Analyze mode!

---

## 📘 MODE 5: MOVE

**PURPOSE:**
Reposition individual elements (nodes, edges, loads)
without deleting and recreating them.

**HOW TO USE:**
1. Point at the element you want to move
2. It will glow purple when hovering
3. Press and HOLD TRIGGER
4. Move your controller to drag the element
5. Release TRIGGER to drop it

**WHAT YOU CAN MOVE:**
• Nodes: Move the joint (connected edges move with it)
• Edges: Move both nodes at once
• Loads: Reposition the force arrow

**BEHAVIOR:**
• Moving a node updates all connected edges automatically
• Nodes snap to grid during movement
• Moving an edge moves both its endpoints
• Loads stay attached to their node

**TIPS:**
• Use this to fine-tune your structure
• Easier than deleting and recreating
• Great for adjusting load positions
• If you want to move a whole structure, use Grab mode

**NEXT STEP:**
Remove mistakes with Delete mode!

---

## 📘 MODE 6: DELETE

**PURPOSE:**
Remove unwanted elements from your structure.
This is a DESTRUCTIVE action - deleted items cannot be recovered.

**HOW TO USE:**
1. Point at the element you want to delete
2. It will glow red as a warning
3. Press TRIGGER to permanently delete
4. You'll feel a strong vibration confirming deletion

**WHAT YOU CAN DELETE:**
• Nodes: Removes node + all connected edges + all loads
• Edges: Removes just that member
• Loads: Removes just that force

**⚠️ CASCADE DELETION:**
Deleting a node will automatically delete:
  • All edges connected to that node
  • All loads applied to that node

This prevents orphaned edges and loads.

**TIPS:**
• The red highlight warns you before deletion
• Strong haptic feedback confirms the action
• No undo feature - be careful!
• Delete edges first if you want to keep the nodes

**NEXT STEP:**
Move entire structures with Grab mode!

---

## 📘 MODE 7: GRAB

**PURPOSE:**
Move an entire connected structure as one piece.
All connected nodes and edges move together.

**HOW TO USE:**
1. Point at ANY part of the structure (node or edge)
2. ALL connected parts glow cyan - this shows what will move
3. Press and HOLD TRIGGER
4. Move your controller to drag the entire structure
5. Release TRIGGER to drop it

**HOW IT WORKS:**
• Uses BFS algorithm to find all connected elements
• Starts from the element you pointed at
• Follows edges to find every connected node
• Highlights the entire graph in cyan
• Moves all nodes while maintaining their relative positions

**INDEPENDENT STRUCTURES:**
• If you have multiple separate structures, each moves independently
• Only the structure you pointed at will move
• Unconnected structures stay in place

**TIPS:**
• Great for repositioning completed sections
• The cyan highlight shows exactly what will move
• Maintains all edge connections
• Loads move with their nodes

**NEXT STEP:**
Test your structure with Analyze mode!

---

## 📘 MODE 8: ANALYZE

**PURPOSE:**
Run finite element analysis (FEA) to calculate forces,
displacements, and reactions in your structure.

**HOW TO USE:**
1. Build a complete structure (nodes + edges + loads + supports)
2. Switch to Analyze mode
3. Press TRIGGER to start analysis
4. Press GRIP to cancel
5. Wait for "Analyzing Structure..." spinner
6. View results in the text panel

**WHAT YOU'LL SEE:**

**RED members** = TENSION (being pulled apart)
**BLUE members** = COMPRESSION (being pushed together)
Color intensity = force magnitude

**ANALYSIS RESULTS SHOW:**
• Node forces (applied loads in X, Y, Z)
• Member forces (tension T or compression C)
• Support reactions (how much force supports provide)
• Displacements (how much structure deforms)

**DISPLACEMENT VISUALIZATION:**
• Semi-transparent red shows deformed shape
• Exaggerated by 100,000,000× to be visible
• Adjust scale with LEFT THUMBSTICK UP/DOWN

**REQUIREMENTS FOR STABLE STRUCTURE:**
✓ At least 1 support node (fixed in place)
✓ Formula: members + supports ≥ 3 × nodes
✓ All nodes must have loads or be connected to loaded nodes

**ERROR MESSAGES:**
• "Unstable" = Not enough supports/members
• "No structure" = No nodes placed
• "Singular matrix" = Geometric instability

**MATERIAL PROPERTIES:**
• Young's Modulus: 200 GPa (steel)
• Cross-sectional Area: 0.01 m² (10 cm²)

**TIPS:**
• Build triangular patterns for stability
• Redundant members increase strength
• Supports should be at the base/anchors
• Large displacements mean weak structure

---

## 📘 MODE 9: GRID

**PURPOSE:**
Customize the 3D grid that nodes snap to.
Control grid size, spacing, and alignment.

**HOW TO USE:**
1. Press TRIGGER to set grid anchor points
2. First click: Origin point
3. Second click: Direction/orientation
4. Or use thumbstick to adjust parameters

**CONTROLS:**
**LEFT THUMBSTICK LEFT/RIGHT:** Cycle parameters
  • X axis size
  • Y axis size
  • Z axis size
  • Spacing between grid points

**LEFT THUMBSTICK UP/DOWN:** Adjust selected parameter
  • Increase/decrease value

**LEFT THUMBSTICK PRESS:** Toggle grid visibility
  • Show/hide grid points

**A BUTTON:** Re-detect table surface
  • Automatically aligns grid to detected table

**GRID PARAMETERS:**
• X, Y, Z: Number of grid points in each direction
• Spacing: Distance between points (in meters)
• Default: 0.1m (10cm) spacing
• Minimum: 0.01m (1cm)

**VISUAL FEEDBACK:**
• Grid points appear as small spheres
• Proximity shader makes points brighter near cursor
• Fades away from cursor for clarity
• Semi-transparent marker previews surface

**AUTO-DETECTION:**
• App auto-detects tables on start
• Aligns grid to table surface
• Falls back to waist-height if no table found
• X-axis follows table's right vector

**TIPS:**
• Smaller spacing = more precision
• Larger grid = more building space
• Toggle visibility off when analyzing
• Re-detect if you move to a new table

**MATH:**
Nodes snap to: origin + (i×spacing×X) + (j×spacing×Y) + (k×spacing×Z)

---

## 🎮 QUICK REFERENCE (Condensed View)

**1. ADD NODE** - Point & Trigger to place joints
**2. ADD EDGE** - Click 2 nodes to connect members
**3. ADD LOAD** - Click node, then set force direction
**4. TOGGLE SUPPORT** - Click node to fix in place
**5. MOVE** - Hold Trigger to drag elements
**6. DELETE** - Click to remove (permanent!)
**7. GRAB** - Hold Trigger to move whole structure
**8. ANALYZE** - Calculate forces (Red=Tension, Blue=Compression)
**9. GRID** - Adjust grid spacing & size

**CONTROLS:**
- Right Thumbstick L/R: Change modes
- Right Trigger: Primary action
- Right Grip (Analyze): Cancel
- B Button: Toggle help panel

---

## 💡 USAGE

The tutorial panel is implemented in [ModeTutorialPanel.cs](Assets/Scripts/ModeTutorialPanel.cs)

**Features:**
- Automatically updates when user changes modes
- Can be toggled on/off with B button on right controller
- Positions itself to the right side of user's view
- Follows camera smoothly
- Can show quick reference or detailed mode-specific instructions

**Setup in Unity:**
1. Create a Canvas or 3D panel in world space
2. Add TextMeshPro component for the tutorial text
3. Add ModeTutorialPanel component to the panel
4. Assign the text reference
5. Assign to OVRGraphController.tutorialPanel
6. Panel will auto-update when modes change!

**Toggle Options:**
- Press B button on right controller (in VR)
- Press H key (for desktop testing)
- Call `tutorialPanel.TogglePanel()` from code
- `tutorialPanel.ShowQuickReference()` for condensed view

---

**Document Version:** 1.0
**Last Updated:** 2026-01-13
**Content Type:** In-app tutorial text
