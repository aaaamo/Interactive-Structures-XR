using UnityEngine;
using TMPro;

/// <summary>
/// Comprehensive tutorial panel showing detailed instructions for all modes
/// Can be toggled on/off by user, shows step-by-step instructions
/// </summary>
public class ModeTutorialPanel : MonoBehaviour
{
    [Header("References")]
    public TextMeshPro tutorialText;
    public GameObject tutorialPanel;
    public Transform panelTransform;

    [Header("Settings")]
    public bool showOnModeChange = true;
    public Vector3 panelOffset = new Vector3(0.5f, 0.2f, 0.8f); // Offset from camera
    public KeyCode toggleKey = KeyCode.H; // For desktop testing

    private OVRGraphController.Mode lastMode = OVRGraphController.Mode.AddNode;
    private bool isPanelVisible = true;

    // Comprehensive tutorial content for each mode
    private static readonly string[] modeTutorials = new string[]
    {
        // AddNode (0)
        @"<b><size=0.08><color=#88FF88>MODE: ADD NODE</color></size></b>

<b>PURPOSE:</b>
Create structural joints (nodes) where members will connect.
Nodes are the foundation of your truss structure.

<b>HOW TO USE:</b>
1. Point your controller where you want to place a node
2. The <color=#88FF88>green ghost preview</color> shows where it will snap
3. Press <color=#FFFF88>TRIGGER</color> to place the node
4. A label will show the node's coordinates (X, Y, Z)

<b>TIPS:</b>
• Nodes automatically snap to the grid for precision
• The grid helps you build straight, aligned structures
• You can adjust grid spacing in Grid mode
• Place nodes at corners and connection points

<b>NEXT STEP:</b>
After placing nodes, switch to <color=#88DDFF>AddEdge</color> mode to connect them!",

        // AddEdge (1)
        @"<b><size=0.08><color=#88DDFF>MODE: ADD EDGE</color></size></b>

<b>PURPOSE:</b>
Connect two nodes with structural members (beams, struts).
Edges carry forces between nodes in your structure.

<b>HOW TO USE:</b>
1. Point at the FIRST node you want to connect
2. When it <color=#88FF88>glows green</color>, press <color=#FFFF88>TRIGGER</color>
3. The node will stay <color=#88FF88>highlighted</color>
4. Point at the SECOND node (will <color=#88DDFF>glow blue</color>)
5. Press <color=#FFFF88>TRIGGER</color> again to create the edge
6. A <color=#88DDFF>blue preview line</color> shows the connection

<b>VALIDATION:</b>
• If the line turns <color=#FF8888>RED</color>, that edge already exists
• You'll feel an error vibration and see an error message
• Cannot create duplicate edges between the same nodes

<b>TIPS:</b>
• Build triangular patterns for stable structures
• Connect all nodes that should share forces
• You can see the edge as a cylinder between nodes

<b>NEXT STEP:</b>
Add forces with <color=#FFAA88>AddLoad</color> mode!",

        // AddLoad (2)
        @"<b><size=0.08><color=#FFAA88>MODE: ADD LOAD</color></size></b>

<b>PURPOSE:</b>
Apply forces (loads) to nodes. These represent weights,
external forces, or any load your structure must support.

<b>HOW TO USE:</b>
1. Point at the node where you want to apply force
2. When it <color=#FFAA88>glows orange</color>, press <color=#FFFF88>TRIGGER</color>
3. The node will stay <color=#FFAA88>highlighted</color>
4. Move your controller to set the force direction
5. An <color=#FFAA88>orange arrow</color> shows the force direction
6. The arrow's LENGTH = force MAGNITUDE
7. Press <color=#FFFF88>TRIGGER</color> again to confirm

<b>UNDERSTANDING MAGNITUDE:</b>
• Distance from node to cursor = force strength
• Pull away from node = larger force
• The arrow will scale to show the magnitude
• Units are in Newtons (N)

<b>TIPS:</b>
• Point downward for gravity/weight loads
• You can add multiple loads to one node
• Each node shows how many loads it has
• Typical loads: 100N - 10,000N

<b>NEXT STEP:</b>
Fix nodes in place with <color=#FFFF88>ToggleSupport</color> mode!",

        // ToggleSupport (3)
        @"<b><size=0.08><color=#FFFF88>MODE: TOGGLE SUPPORT</color></size></b>

<b>PURPOSE:</b>
Fix nodes in place so they cannot move. These are the
anchor points or foundations of your structure.

<b>HOW TO USE:</b>
1. Point at any node
2. When it <color=#FFFF88>glows yellow</color>, press <color=#FFFF88>TRIGGER</color>
3. The node switches between FREE and FIXED

<b>VISUAL STATES:</b>
• <color=#FFFFFF>FREE nodes:</color> Can move under loads
• <color=#FFFF88>SUPPORT nodes:</color> Fixed in place (different visual)
• Toggle back and forth as needed

<b>WHY SUPPORTS MATTER:</b>
• Structures need supports to be stable
• Without supports, structure will collapse under load
• Supports provide 'reaction forces' to balance loads
• Think of them as bolts to the floor/wall

<b>TIPS:</b>
• Typical: Fix 2-3 nodes as supports
• Fix nodes at the base of your structure
• Too many supports = overconstrained (won't move)
• Too few supports = unstable (will collapse)

<b>STRUCTURAL RULE:</b>
For a stable 3D truss: <color=#88FF88>members + supports ≥ 3 × nodes</color>

<b>NEXT STEP:</b>
Analyze your structure with <color=#FFFFFF>Analyze</color> mode!",

        // Move (4)
        @"<b><size=0.08><color=#AA88FF>MODE: MOVE</color></size></b>

<b>PURPOSE:</b>
Reposition individual elements (nodes, edges, loads)
without deleting and recreating them.

<b>HOW TO USE:</b>
1. Point at the element you want to move
2. It will <color=#AA88FF>glow purple</color> when hovering
3. Press and HOLD <color=#FFFF88>TRIGGER</color>
4. Move your controller to drag the element
5. Release <color=#FFFF88>TRIGGER</color> to drop it

<b>WHAT YOU CAN MOVE:</b>
• <color=#FFFFFF>Nodes:</color> Move the joint (connected edges move with it)
• <color=#FFFFFF>Edges:</color> Move both nodes at once
• <color=#FFFFFF>Loads:</color> Reposition the force arrow

<b>BEHAVIOR:</b>
• Moving a node updates all connected edges automatically
• Nodes snap to grid during movement
• Moving an edge moves both its endpoints
• Loads stay attached to their node

<b>TIPS:</b>
• Use this to fine-tune your structure
• Easier than deleting and recreating
• Great for adjusting load positions
• If you want to move a whole structure, use Grab mode

<b>NEXT STEP:</b>
Remove mistakes with <color=#FF8888>Delete</color> mode!",

        // Delete (5)
        @"<b><size=0.08><color=#FF8888>MODE: DELETE</color></size></b>

<b>PURPOSE:</b>
Remove unwanted elements from your structure.
This is a DESTRUCTIVE action - deleted items cannot be recovered.

<b>HOW TO USE:</b>
1. Point at the element you want to delete
2. It will <color=#FF8888>glow red</color> as a warning
3. Press <color=#FFFF88>TRIGGER</color> to permanently delete
4. You'll feel a strong vibration confirming deletion

<b>WHAT YOU CAN DELETE:</b>
• <color=#FFFFFF>Nodes:</color> Removes node + all connected edges + all loads
• <color=#FFFFFF>Edges:</color> Removes just that member
• <color=#FFFFFF>Loads:</color> Removes just that force

<b>⚠️ CASCADE DELETION:</b>
Deleting a node will automatically delete:
  • All edges connected to that node
  • All loads applied to that node

This prevents orphaned edges and loads.

<b>TIPS:</b>
• The red highlight warns you before deletion
• Strong haptic feedback confirms the action
• No undo feature - be careful!
• Delete edges first if you want to keep the nodes

<b>NEXT STEP:</b>
Move entire structures with <color=#88DDFF>Grab</color> mode!",

        // Grab (6)
        @"<b><size=0.08><color=#88DDFF>MODE: GRAB</color></size></b>

<b>PURPOSE:</b>
Move an entire connected structure as one piece.
All connected nodes and edges move together.

<b>HOW TO USE:</b>
1. Point at ANY part of the structure (node or edge)
2. <color=#88DDFF>ALL connected parts glow cyan</color> - this shows what will move
3. Press and HOLD <color=#FFFF88>TRIGGER</color>
4. Move your controller to drag the entire structure
5. Release <color=#FFFF88>TRIGGER</color> to drop it

<b>HOW IT WORKS:</b>
• Uses BFS algorithm to find all connected elements
• Starts from the element you pointed at
• Follows edges to find every connected node
• Highlights the entire graph in <color=#88DDFF>cyan</color>
• Moves all nodes while maintaining their relative positions

<b>INDEPENDENT STRUCTURES:</b>
• If you have multiple separate structures, each moves independently
• Only the structure you pointed at will move
• Unconnected structures stay in place

<b>TIPS:</b>
• Great for repositioning completed sections
• The cyan highlight shows exactly what will move
• Maintains all edge connections
• Loads move with their nodes

<b>NEXT STEP:</b>
Test your structure with <color=#FFFFFF>Analyze</color> mode!",

        // Analyze (7)
        @"<b><size=0.08><color=#FFFFFF>MODE: ANALYZE</color></size></b>

<b>PURPOSE:</b>
Run finite element analysis (FEA) to calculate forces,
displacements, and reactions in your structure.

<b>HOW TO USE:</b>
1. Build a complete structure (nodes + edges + loads + supports)
2. Switch to Analyze mode
3. Press <color=#FFFF88>TRIGGER</color> to start analysis
4. Press <color=#FFFF88>GRIP</color> to cancel
5. Wait for ""Analyzing Structure..."" spinner
6. View results in the text panel

<b>WHAT YOU'LL SEE:</b>

<color=#FF8888>RED members</color> = TENSION (being pulled apart)
<color=#8888FF>BLUE members</color> = COMPRESSION (being pushed together)
Color intensity = force magnitude

<b>ANALYSIS RESULTS SHOW:</b>
• Node forces (applied loads in X, Y, Z)
• Member forces (tension T or compression C)
• Support reactions (how much force supports provide)
• Displacements (how much structure deforms)

<b>DISPLACEMENT VISUALIZATION:</b>
• Semi-transparent red shows deformed shape
• Exaggerated by 100,000,000× to be visible
• Adjust scale with <color=#88FF88>LEFT THUMBSTICK UP/DOWN</color>

<b>REQUIREMENTS FOR STABLE STRUCTURE:</b>
✓ At least 1 support node (fixed in place)
✓ Formula: members + supports ≥ 3 × nodes
✓ All nodes must have loads or be connected to loaded nodes

<b>ERROR MESSAGES:</b>
• ""Unstable"" = Not enough supports/members
• ""No structure"" = No nodes placed
• ""Singular matrix"" = Geometric instability

<b>MATERIAL PROPERTIES:</b>
• Young's Modulus: 200 GPa (steel)
• Cross-sectional Area: 0.01 m² (10 cm²)

<b>TIPS:</b>
• Build triangular patterns for stability
• Redundant members increase strength
• Supports should be at the base/anchors
• Large displacements mean weak structure",

        // Grid (8)
        @"<b><size=0.08><color=#AAAAAA>MODE: GRID</color></size></b>

<b>PURPOSE:</b>
Customize the 3D grid that nodes snap to.
Control grid size, spacing, and alignment.

<b>HOW TO USE:</b>
1. Press <color=#FFFF88>TRIGGER</color> to set grid anchor points
2. First click: Origin point
3. Second click: Direction/orientation
4. Or use thumbstick to adjust parameters

<b>CONTROLS:</b>
<color=#88FF88>LEFT THUMBSTICK LEFT/RIGHT:</color> Cycle parameters
  • X axis size
  • Y axis size
  • Z axis size
  • Spacing between grid points

<color=#88FF88>LEFT THUMBSTICK UP/DOWN:</color> Adjust selected parameter
  • Increase/decrease value

<color=#88FF88>LEFT THUMBSTICK PRESS:</color> Toggle grid visibility
  • Show/hide grid points

<color=#88FF88>A BUTTON:</color> Re-detect table surface
  • Automatically aligns grid to detected table

<b>GRID PARAMETERS:</b>
• <color=#FFFFFF>X, Y, Z:</color> Number of grid points in each direction
• <color=#FFFFFF>Spacing:</color> Distance between points (in meters)
• Default: 0.1m (10cm) spacing
• Minimum: 0.01m (1cm)

<b>VISUAL FEEDBACK:</b>
• Grid points appear as small spheres
• Proximity shader makes points brighter near cursor
• Fades away from cursor for clarity
• Semi-transparent marker previews surface

<b>AUTO-DETECTION:</b>
• App auto-detects tables on start
• Aligns grid to table surface
• Falls back to waist-height if no table found
• X-axis follows table's right vector

<b>TIPS:</b>
• Smaller spacing = more precision
• Larger grid = more building space
• Toggle visibility off when analyzing
• Re-detect if you move to a new table

<b>MATH:</b>
Nodes snap to: origin + (i×spacing×X) + (j×spacing×Y) + (k×spacing×Z)"
    };

    void Start()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(isPanelVisible);
        }

        UpdateTutorialText(lastMode);
    }

    void Update()
    {
        // Desktop testing toggle
        if (Input.GetKeyDown(toggleKey))
        {
            TogglePanel();
        }

        // VR: Hold B button on right controller to toggle
        if (OVRInput.GetDown(OVRInput.Button.Two, OVRInput.Controller.RTouch))
        {
            TogglePanel();
        }

        // Position panel relative to camera if it's visible
        if (isPanelVisible && panelTransform != null && Camera.main != null)
        {
            PositionPanel();
        }
    }

    /// <summary>
    /// Update tutorial text when mode changes
    /// </summary>
    public void UpdateForMode(OVRGraphController.Mode newMode)
    {
        if (newMode != lastMode)
        {
            lastMode = newMode;

            if (showOnModeChange && !isPanelVisible)
            {
                ShowPanel();
            }

            UpdateTutorialText(newMode);
        }
    }

    /// <summary>
    /// Update the tutorial text content
    /// </summary>
    void UpdateTutorialText(OVRGraphController.Mode mode)
    {
        if (tutorialText == null) return;

        int modeIndex = (int)mode;
        if (modeIndex >= 0 && modeIndex < modeTutorials.Length)
        {
            tutorialText.text = modeTutorials[modeIndex];
        }
    }

    /// <summary>
    /// Toggle panel visibility
    /// </summary>
    public void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(isPanelVisible);
        }

        HapticFeedback.Trigger(HapticFeedback.HapticType.Light);
    }

    /// <summary>
    /// Show the panel
    /// </summary>
    public void ShowPanel()
    {
        isPanelVisible = true;
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
    }

    /// <summary>
    /// Hide the panel
    /// </summary>
    public void HidePanel()
    {
        isPanelVisible = false;
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Position panel to the side of the user's view
    /// </summary>
    void PositionPanel()
    {
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Position to the right and slightly forward
        Vector3 targetPos = cameraPos
            + cameraRight * panelOffset.x
            + Vector3.up * panelOffset.y
            + cameraForward * panelOffset.z;

        panelTransform.position = Vector3.Lerp(panelTransform.position, targetPos, Time.deltaTime * 5f);

        // Face the user
        Vector3 lookDir = cameraPos - panelTransform.position;
        lookDir.y = 0; // Keep panel upright
        if (lookDir.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(-lookDir);
            panelTransform.rotation = Quaternion.Slerp(panelTransform.rotation, targetRotation, Time.deltaTime * 5f);
        }
    }

    /// <summary>
    /// Show quick reference of all modes (condensed)
    /// </summary>
    public void ShowQuickReference()
    {
        if (tutorialText == null) return;

        tutorialText.text = @"<b><size=0.1>QUICK REFERENCE - ALL MODES</size></b>

<b><color=#88FF88>1. ADD NODE</color></b> - Point & Trigger to place joints
<b><color=#88DDFF>2. ADD EDGE</color></b> - Click 2 nodes to connect members
<b><color=#FFAA88>3. ADD LOAD</color></b> - Click node, then set force direction
<b><color=#FFFF88>4. TOGGLE SUPPORT</color></b> - Click node to fix in place
<b><color=#AA88FF>5. MOVE</color></b> - Hold Trigger to drag elements
<b><color=#FF8888>6. DELETE</color></b> - Click to remove (permanent!)
<b><color=#88DDFF>7. GRAB</color></b> - Hold Trigger to move whole structure
<b><color=#FFFFFF>8. ANALYZE</color></b> - Calculate forces (Red=Tension, Blue=Compression)
<b><color=#AAAAAA>9. GRID</color></b> - Adjust grid spacing & size

<b>CONTROLS:</b>
Right Thumbstick L/R: Change modes
Right Trigger: Primary action
Right Grip (Analyze): Cancel
B Button: Toggle this help panel

<b>Press B or click a mode for detailed help!</b>";
    }
}
