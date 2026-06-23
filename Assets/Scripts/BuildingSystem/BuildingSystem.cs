using UnityEngine;
using System.Collections.Generic;
using Simulation.Data;
using Simulation.Physics;
using Simulation.Mission;

namespace Simulation.Building
{
    /// <summary>
    /// 3D Building System
    /// - Place on ground or on top of existing structures
    /// - Stacks upward infinitely
    /// - Size comes from Prefab bounds (Pivot-aware)
    /// - Ghost follows mouse smoothly
    /// Modes: Idle, Placing, Moving, Deleting
    /// </summary>
    public class BuildingSystem : MonoBehaviour
    {
        public enum BuildMode { Idle, Placing, Moving, Deleting, Painting }

        public static BuildingSystem Instance { get; private set; }

        [Header("Grid Settings")]
        [SerializeField] private bool useGridSnap = true;
        [SerializeField] private float gridSize = 1f;
        [Tooltip("Number of grid columns (X axis)")]
        [SerializeField] private int gridColumns = 10;
        [Tooltip("Number of grid rows (Z axis)")]
        [SerializeField] private int gridRows = 10;
        [Tooltip("If true, Y axis also snaps to grid increments so all structures share the same base levels.")]
        [SerializeField] private bool snapYToGrid = true;

        [Header("Layer Masks")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask structureLayer;

        [Header("Joint Settings")]
        [Tooltip("ระยะขยาย Hitbox เล็กน้อยตอนหา Joint เพื่อนบ้าน (ยิ่งมากยิ่งจับชิ้นข้างเคียงง่ายขึ้น)")]
        [SerializeField] private float jointHitboxExpand = 0.15f;

        [Header("Height Settings")]
        [Tooltip("Vertical distance between floors. If pillarReference is assigned, this will be auto-set in Start.")]
        [SerializeField] private float heightStep = 3.0f;

        [Tooltip("Optional: Assign your Pillar/Column structure here. Its height will automatically define the Height Step for the whole building.")]
        [SerializeField] private StructureData pillarReference;

        [Header("Budget")]
        private float _currentBudget;

        [Header("General SFX / VFX")]
        [SerializeField] private AudioClip generalPlaceSound;
        [SerializeField] private AudioClip generalSellSound;
        [SerializeField] private AudioClip generalPaintSound;
        [SerializeField] private AudioClip generalUndoSound;
        [SerializeField] private AudioClip generalRedoSound;
        [SerializeField] private AudioClip generalErrorSound;
        [SerializeField] private GameObject generalSellVFX;

        [Header("References")]
        [SerializeField] private UnityEngine.Camera mainCamera;
        [SerializeField] private GhostBuilder ghostBuilder;
        // อ่าน OccludedColliders จาก CameraController เพื่อข้ามตอน Raycast
        private Simulation.Camera.CameraController _cameraController;

        // State
        private BuildMode _currentMode = BuildMode.Idle;
        private StructureData _selectedData;
        private MaterialData _selectedMaterial;
        private List<StructureUnit> _placedStructures = new List<StructureUnit>();
        private StructureUnit _movingUnit;
        private StructureUnit _hoveredUnit;
        private HashSet<StructureUnit> _dragSelectedUnits = new HashSet<StructureUnit>();
        private bool _isDraggingMove = false;

        // Group Moving
        private List<StructureUnit> _movingGroup = new List<StructureUnit>();
        private List<Vector3> _groupOffsets = new List<Vector3>();
        private List<float> _groupRotations = new List<float>();
        private struct GroupOriginalState { public StructureUnit unit; public Vector3 pos; public float rot; }
        private List<GroupOriginalState> _groupOriginalStates = new List<GroupOriginalState>();

        // Placement Temp Data
        private Vector3 _currentHitPos;
        private Vector3 _currentHitNormal;
        private Collider _currentHitCollider;
        private float _pivotToBottomOffset = 0f;
        private bool _hasValidTarget;

        // Pickup Temp Data
        private float _pickupStartTime;
        private Vector3 _pickupStartMousePos;
        private bool _isHoldingPickup = false;

        // Right-click hold detection to distinguish between quick cancel and camera drag
        private float _rightClickDownTime = 0f;
        private const float RIGHT_CLICK_CANCEL_HOLD_THRESHOLD = 0.25f;

        // Frame cooldown: prevents the same click that selects a structure from also placing it
        private bool _justEnteredPlacing = false;

        // Floor-level system
        private int _currentFloor = 1;
        private int _maxOccupiedFloor = 1;

        // Undo / Redo System
        private class BuildAction
        {
            public System.Action Undo;
            public System.Action Redo;
        }
        private Stack<BuildAction> _undoStack = new Stack<BuildAction>();
        private Stack<BuildAction> _redoStack = new Stack<BuildAction>();

        // State for Move Command Undo
        private Vector3 _moveOriginalPos;
        private float _moveOriginalRot;
        private Collider _moveOriginalTargetCol;

        public float CurrentBudget => _currentBudget;
        public BuildMode CurrentMode => _currentMode;
        public bool IsPlacing => _currentMode == BuildMode.Placing;
        public bool IsMoving => _currentMode == BuildMode.Moving;
        public bool IsDeleting => _currentMode == BuildMode.Deleting;
        public bool IsStructurePlaced(StructureUnit unit) => _placedStructures.Contains(unit);
        public bool IsPainting => _currentMode == BuildMode.Painting;
        public MaterialData SelectedMaterial => _selectedMaterial;
        public int CurrentFloor => _currentFloor;
        public int MaxOccupiedFloor => _maxOccupiedFloor;
        public int GridColumns => gridColumns;
        public int GridRows => gridRows;
        public float GetGridSize => gridSize;
        public float HeightStep => heightStep > 0f ? heightStep : gridSize;

        // Batch Command System
        private bool _isBatching = false;
        private List<System.Action> _batchUndos = new List<System.Action>();
        private List<System.Action> _batchRedos = new List<System.Action>();

        /// <summary>
        /// ตั้งงบประมาณจากภายนอก (เช่น MissionManager)
        /// </summary>
        public void SetBudget(float amount)
        {
            _currentBudget = amount;
        }

        public void SetGridDimensions(int cols, int rows)
        {
            if (cols > 0) gridColumns = cols;
            if (rows > 0) gridRows = rows;
            
            // Sync visual grid
            if (Simulation.Physics.SimulationManager.Instance != null)
            {
                Simulation.Physics.SimulationManager.Instance.UpdateGridVisual(gridColumns, gridRows, gridSize);
            }
        }

        /// <summary>
        /// คำนวณราคาจริงหลังหักส่วนลด (Economist) และบวกภาษี (Tax)
        /// </summary>
        public float GetEffectivePrice(float basePrice)
        {
            float price = basePrice;

            // 1. ส่วนลดจาก Economist NPC (-10%)
            if (Simulation.NPC.NPCSkillManager.Instance != null)
            {
                float discount = Simulation.NPC.NPCSkillManager.Instance.GetDiscountRate();
                price *= (1f - discount);
            }

            // 2. ภาษีจาก MissionData (+X%) — ถ้า Politician active จะเป็น 0
            if (Simulation.NPC.NPCSkillManager.Instance != null)
            {
                float tax = Simulation.NPC.NPCSkillManager.Instance.GetEffectiveTaxRate();
                price *= (1f + tax);
            }
            else
            {
                // ถ้ายังไม่มี NPCSkillManager ให้ใช้ tax จาก MissionData ตรง
                var mm = Simulation.Mission.MissionManager.Instance;
                if (mm != null && mm.CurrentMission != null && mm.CurrentMission.enableTax)
                {
                    price *= (1f + mm.CurrentMission.taxRate);
                }
            }

            return price;
        }

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

#if UNITY_EDITOR
            AutoAssignAudioClips();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            AutoAssignAudioClips();
        }

        private void OnValidate()
        {
            AutoAssignAudioClips();
        }

        private void AutoAssignAudioClips()
        {
            if (generalUndoSound == null)
            {
                generalUndoSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/UI/Undo - Redo.mp3");
            }
            if (generalRedoSound == null)
            {
                generalRedoSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/UI/Undo - Redo.mp3");
            }
            if (generalErrorSound == null)
            {
                generalErrorSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/UI/Error.mp3");
            }
            if (generalSellSound == null)
            {
                generalSellSound = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/SFX/UI/Sell.mp3");
            }
        }
#endif

        private void Start()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (ghostBuilder == null) ghostBuilder = GetComponent<GhostBuilder>();
            
            // Auto-set heightStep from pillarReference if available
            if (pillarReference != null && pillarReference.prefab != null)
            {
                (Vector3 center, Vector3 size) = GetPrefabBounds(pillarReference.prefab);
                heightStep = size.y;
            }

            // หา CameraController จากกล้องหลัก
            if (mainCamera != null)
                _cameraController = mainCamera.GetComponent<Simulation.Camera.CameraController>();

            // Initialize Grid Visual
            if (Simulation.Physics.SimulationManager.Instance != null)
            {
                Simulation.Physics.SimulationManager.Instance.UpdateGridVisual(gridColumns, gridRows, gridSize);
            }
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    _cameraController = mainCamera.GetComponent<Simulation.Camera.CameraController>();
                }
            }
            if (mainCamera == null) return;

            if (Input.GetMouseButtonDown(1))
            {
                _rightClickDownTime = Time.time;
            }

            HandleFloorSwitch();

            // ปิดระบบสร้างทั้งหมดถ้ากำลังเริ่มการจำลองฟิสิกส์ (ป้องกันการวางของขณะร่วง)
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                if (_currentMode != BuildMode.Idle) ExitMode();
                return;
            }

            UpdateRaycast();
            HandleHoverHighlight();

            switch (_currentMode)
            {
                case BuildMode.Placing:
                    HandlePlacementMode();
                    break;
                case BuildMode.Moving:
                    if (_movingUnit != null || _movingGroup.Count > 0)
                        HandleMovingMode();
                    else
                        HandlePickupMode();
                    break;
                case BuildMode.Deleting:
                    HandleDeleteMode();
                    break;
                case BuildMode.Painting:
                    HandlePaintingMode();
                    break;
                default:
                    break;
            }

            HandleUndoRedoInput();
        }

        // --------------------------------------------------------------------------------
        // UNDO / REDO
        // --------------------------------------------------------------------------------

        private void HandleUndoRedoInput()
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand))
            {
                if (Input.GetKeyDown(KeyCode.Z))
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        Redo();
                    else
                        Undo();
                }
                else if (Input.GetKeyDown(KeyCode.Y))
                {
                    Redo();
                }
            }
        }

        private void ExecuteCommand(System.Action execute, System.Action undo)
        {
            execute();
            
            if (_isBatching)
            {
                _batchUndos.Add(undo);
                _batchRedos.Add(execute);
            }
            else
            {
                _undoStack.Push(new BuildAction { Undo = undo, Redo = execute });
                _redoStack.Clear(); // Any new action clears the redo history
            }
        }

        private void BeginBatch()
        {
            _isBatching = true;
            _batchUndos.Clear();
            _batchRedos.Clear();
        }

        private void EndBatch()
        {
            if (!_isBatching) return;
            _isBatching = false;

            if (_batchUndos.Count > 0)
            {
                // Create copies to capture in the closure
                var undos = new List<System.Action>(_batchUndos);
                var redos = new List<System.Action>(_batchRedos);

                _undoStack.Push(new BuildAction
                {
                    Undo = () => { for (int i = undos.Count - 1; i >= 0; i--) undos[i]?.Invoke(); },
                    Redo = () => { for (int i = 0; i < redos.Count; i++) redos[i]?.Invoke(); }
                });
                _redoStack.Clear();
            }
        }

        public void Undo()
        {
            if (_undoStack.Count > 0)
            {
                var action = _undoStack.Pop();
                action.Undo();
                _redoStack.Push(action);
                
                if (generalUndoSound != null)
                {
                    Vector3 playPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
                    AudioSource.PlayClipAtPoint(generalUndoSound, playPos);
                }
                RecalculateMaxFloor();
            }
            else if (generalErrorSound != null)
            {
                Vector3 playPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(generalErrorSound, playPos);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                action.Redo();
                _undoStack.Push(action);

                if (generalRedoSound != null)
                {
                    Vector3 playPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
                    AudioSource.PlayClipAtPoint(generalRedoSound, playPos);
                }
                RecalculateMaxFloor();
            }
            else if (generalErrorSound != null)
            {
                Vector3 playPos = mainCamera != null ? mainCamera.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(generalErrorSound, playPos);
            }
        }

        // --------------------------------------------------------------------------------
        // FLOOR SWITCHING (Q/E)
        // --------------------------------------------------------------------------------

        private void HandleFloorSwitch()
        {
            if (Input.GetKeyDown(KeyCode.Q))
            {
                // Go DOWN one floor (minimum = 1)
                if (_currentFloor > 1)
                {
                    _currentFloor--;
                    NotifyCameraFloorChanged();
                }
            }

            if (Input.GetKeyDown(KeyCode.E))
            {
                // Go UP one floor (max = highest occupied floor + 1)
                if (_currentFloor < _maxOccupiedFloor + 1)
                {
                    _currentFloor++;
                    NotifyCameraFloorChanged();
                }
            }
        }

        /// <summary>
        /// Recalculate the highest floor that has at least one structure placed on it.
        /// Called after placing, moving, or deleting structures.
        /// </summary>
        public void RecalculateMaxFloor()
        {
            _maxOccupiedFloor = 1;
            foreach (var unit in _placedStructures)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                
                var stress = unit.GetComponent<StructuralStress>();
                if (stress != null && stress.IsBroken) continue;

                if (unit.Data == null) continue;
                
                float bottomY = float.MaxValue;
                foreach (Collider col in unit.GetComponentsInChildren<Collider>())
                {
                    if (col.bounds.min.y < bottomY) bottomY = col.bounds.min.y;
                }
                if (bottomY == float.MaxValue) bottomY = unit.transform.position.y - GetPivotToBottomOffset(unit.gameObject);
                
                float y = bottomY;

                int floor = Mathf.FloorToInt(y / heightStep + 0.1f) + 1;
                if (floor > _maxOccupiedFloor) _maxOccupiedFloor = floor;
            }
        }

        /// <summary>
        /// Convert a world Y position to a floor index (0-based).
        /// Floor 0 = ground level, Floor 1 = one heightStep up, etc.
        /// </summary>
        public int GetFloorFromY(float worldY)
        {
            float step = HeightStep;
            // ใช้ FloorToInt + epsilon เพื่อเลี่ยงปัญหา Bankers Rounding ที่ทำให้ชั้นกระโดด
            // และรองรับทั้ง Bottom-pivot (y=0) และ Center-pivot (y=0.5*step)
            return Mathf.Max(1, Mathf.FloorToInt(worldY / step + 0.1f) + 1);
        }

        /// <summary>
        /// Get the world Y position for a given floor index.
        /// </summary>
        public float GetFloorY(int floor)
        {
            float step = heightStep > 0f ? heightStep : gridSize;
            return Mathf.Max(0, floor - 1) * step;
        }

        private void NotifyCameraFloorChanged()
        {
            float floorY = GetFloorY(_currentFloor);
            if (_cameraController != null)
            {
                _cameraController.SetFloorView(_currentFloor, floorY);
            }
            if (SimulationManager.Instance != null)
            {
                SimulationManager.Instance.SetGridHeight(floorY);
            }
        }

        public void TriggerCameraShake(float intensity)
        {
            if (_cameraController != null)
            {
                _cameraController.TriggerShake(intensity);
            }
        }

        // --------------------------------------------------------------------------------
        // RAYCAST - hits BOTH ground and structures
        // --------------------------------------------------------------------------------

        private void UpdateRaycast()
        {
            _hasValidTarget = false;
            _currentHitCollider = null;

            if (mainCamera == null) return;

            LayerMask combinedMask = groundLayer | structureLayer;
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            // คำนวณ Floor Plane สำหรับชั้นปัจจุบัน (เพื่อให้ข้ามการวางของชั้นที่ต่ำกว่า)
            float currentFloorY = GetFloorY(_currentFloor);
            Plane floorPlane = new Plane(Vector3.up, new Vector3(0, currentFloorY, 0));
            float enter;
            bool hitPlane = floorPlane.Raycast(ray, out enter);
            Vector3 planeHitPos = hitPlane ? ray.GetPoint(enter) : Vector3.zero;

            // 1. Try precise Raycast first to get clean, pixel-perfect hit points and normals
            RaycastHit preciseHit;
            bool hitPrecise = UnityEngine.Physics.Raycast(ray, out preciseHit, 500f, combinedMask, QueryTriggerInteraction.Collide);
            var occluded = _cameraController != null ? _cameraController.OccludedColliders : null;

            bool ignorePreciseHit = hitPrecise && preciseHit.point.y < currentFloorY - 0.1f;

            bool preciseHitValidStructure = hitPrecise 
                && (occluded == null || !occluded.Contains(preciseHit.collider))
                && preciseHit.collider.GetComponentInParent<StructureUnit>() != null;

            if (preciseHitValidStructure && !ignorePreciseHit)
            {
                _currentHitPos      = preciseHit.point;
                _currentHitNormal   = SnapToCardinal(preciseHit.normal);
                _currentHitCollider = preciseHit.collider;
                _hasValidTarget     = true;
                return;
            }

            // 2. Fallback to SphereCast only if precise raycast missed or hit the ground
            float castRadius = gridSize * 0.15f;
            RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(ray, castRadius, 500f, combinedMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit? bestStructureHit = null;
            RaycastHit? bestGroundHit = null;

            foreach (var hit in hits)
            {
                if (occluded != null && occluded.Contains(hit.collider)) continue;
                if (hit.point.y < currentFloorY - 0.1f) continue; // ข้ามการชนกับวัตถุที่อยู่ต่ำกว่าชั้นปัจจุบัน

                bool isStructure = hit.collider.GetComponentInParent<StructureUnit>() != null;

                if (isStructure && bestStructureHit == null)
                {
                    bestStructureHit = hit;
                }
                else if (!isStructure && bestGroundHit == null)
                {
                    bestGroundHit = hit;
                }

                if (bestStructureHit != null && bestGroundHit != null) break;
            }

            RaycastHit? chosen = bestStructureHit ?? ((hitPrecise && !ignorePreciseHit) ? (RaycastHit?)preciseHit : bestGroundHit);

            if (chosen.HasValue)
            {
                _currentHitPos      = chosen.Value.point;
                _currentHitNormal   = SnapToCardinal(chosen.Value.normal);
                _currentHitCollider = chosen.Value.collider;
                _hasValidTarget     = true;
            }
            else if (hitPlane)
            {
                // Fallback ยิงเข้าหา Plane ของชั้นปัจจุบัน (กรณีคลิกโดนอากาศ หรือของชั้นต่ำกว่า)
                _currentHitPos      = planeHitPos;
                _currentHitNormal   = Vector3.up;
                _currentHitCollider = null;
                _hasValidTarget     = true;
            }
        }

        private Vector3 SnapToCardinal(Vector3 normal)
        {
            float absX = Mathf.Abs(normal.x);
            float absY = Mathf.Abs(normal.y);
            float absZ = Mathf.Abs(normal.z);

            if (absY > absX && absY > absZ)
            {
                return new Vector3(0f, Mathf.Sign(normal.y), 0f);
            }
            else if (absX > absZ)
            {
                return new Vector3(Mathf.Sign(normal.x), 0f, 0f);
            }
            else
            {
                return new Vector3(0f, 0f, Mathf.Sign(normal.z));
            }
        }

        // --------------------------------------------------------------------------------
        // HOVER HIGHLIGHT (Select items to Move/Delete)
        // --------------------------------------------------------------------------------

        private void HandleHoverHighlight()
        {
            // Only highlight in Move, Delete, or Paint modes when NOT currently holding an object
            bool canHighlight = _currentMode == BuildMode.Moving || _currentMode == BuildMode.Deleting || _currentMode == BuildMode.Painting;
            bool isHolding = _currentMode == BuildMode.Moving && _movingUnit != null;

            if (!canHighlight || isHolding)
            {
                ClearHover();
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (UnityEngine.Physics.Raycast(ray, out RaycastHit hit, 500f, structureLayer, QueryTriggerInteraction.Collide))
            {
                // Find StructureUnit on this object or any parent
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                if (unit != _hoveredUnit)
                {
                    ClearHover();
                    _hoveredUnit = unit;
                    if (_hoveredUnit != null) _hoveredUnit.SetHighlight(true);
                }
            }
            else
            {
                ClearHover();
            }
        }

        private void ClearHover()
        {
            if (_hoveredUnit != null)
            {
                _hoveredUnit.SetHighlight(false);
                _hoveredUnit = null;
            }
        }

        // --------------------------------------------------------------------------------
        // PLACEMENT MODE
        // --------------------------------------------------------------------------------

        // Drag building state
        private bool _isDragging = false;
        private Vector3 _dragStartPos;
        private Vector3 _dragStartNormal;
        private Collider _dragStartCollider;
        private Vector3 _dragSnappedStart; // Cached snapped origin
        private List<Vector3> _dragPositions = new List<Vector3>();

        private void HandlePlacementMode()
        {
            if (Input.GetMouseButtonUp(1) && Time.time - _rightClickDownTime <= RIGHT_CLICK_CANCEL_HOLD_THRESHOLD) { ExitMode(); return; }
            
            // Manual rotation for all structures
            if (Input.GetKeyDown(KeyCode.R) && _selectedData != null)
            {
                ghostBuilder.Rotate();
                
                // If we are currently dragging, update the snapped start point to match the new rotation
                if (_isDragging)
                {
                    _dragSnappedStart = CalculatePlacementPosition(_dragStartPos, _dragStartNormal, _dragStartCollider);
                }
            }

            // Skip the frame where we just entered placing mode (prevents UI click from placing)
            if (_justEnteredPlacing)
            {
                _justEnteredPlacing = false;
                return;
            }

            if (_hasValidTarget && ghostBuilder.HasGhost)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    _isDragging = true;
                    _dragStartPos = _currentHitPos; // Raw hit
                    _dragStartNormal = _currentHitNormal;
                    _dragStartCollider = _currentHitCollider;
                    
                    // Lock the snapped origin immediately on press
                    _dragSnappedStart = CalculatePlacementPosition(_dragStartPos, _dragStartNormal, _dragStartCollider);
                    
                    BeginBatch();
                }

                if (_isDragging)
                {
                    // Use the locked start info to calculate current position to prevent height jumps
                    Vector3 currentPos = CalculatePlacementPosition(_currentHitPos, _dragStartNormal, _dragStartCollider);
                    _dragPositions = CalculateDragPositions(_dragSnappedStart, currentPos, _selectedData.size.x, _selectedData.size.z, ghostBuilder.CurrentRotation, _selectedData.structureType);
                }
                else
                {
                    // Normal hover preview
                    Vector3 currentPos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider);
                    _dragPositions.Clear();
                    _dragPositions.Add(currentPos);
                }

                // Update ghost previews
                bool allValid = true;
                float totalCost = 0f;
                // Gadget ใช้ Material เริ่มต้นเท่านั้น (เปลี่ยนไม่ได้)
                MaterialData mat = (_selectedData.isGadget)
                    ? _selectedData.defaultMaterial
                    : (_selectedMaterial != null ? _selectedMaterial : _selectedData.defaultMaterial);
                float materialPrice = mat != null ? mat.priceModifier : 0f;
                float itemPrice = GetEffectivePrice(_selectedData.basePrice + materialPrice);

                // NEW: Pre-calculate if any piece in the drag group has world support
                bool groupHasWorldSupport = false;
                if (_isDragging)
                {
                    foreach (var pos in _dragPositions)
                    {
                        if (IsSupportedByWorld(pos, ghostBuilder.CurrentRotation, _selectedData))
                        {
                            groupHasWorldSupport = true;
                            break;
                        }
                    }
                }
                
                foreach (var pos in _dragPositions)
                {
                    bool isOccupiedBySame = IsAlreadyOccupiedBySame(pos, ghostBuilder.CurrentRotation, _selectedData);
                    bool isClear = isOccupiedBySame || IsAreaClear(pos, ghostBuilder.CurrentRotation, _selectedData);
                    
                    // For dragging, pieces support each other if the group is supported somewhere
                    bool hasSupport = _isDragging ? groupHasWorldSupport : HasStructuralSupport(pos, ghostBuilder.CurrentRotation, _selectedData);
                    
                    // Relax 'placeOnStructureOnly' during drag if the group is supported
                    StructureUnit hitUnit = _currentHitCollider != null ? _currentHitCollider.GetComponentInParent<StructureUnit>() : null;
                    bool isFloor = hitUnit != null && hitUnit.Data.structureType == StructureType.Floor;
                    bool isTopSurface = _currentHitNormal.y > 0.9f;

                    // Check for structure/floor requirement
                    var ptOnPrefab = _selectedData.prefab != null ? _selectedData.prefab.GetComponentInChildren<Simulation.Character.PersonTarget>() : null;
                    bool isPlacingPerson = ptOnPrefab != null;
                    
                    // หากเป็นคน ให้วางที่พื้นได้เลย (Override placeOnStructureOnly)
                    bool effectivePlaceOnStructureOnly = isPlacingPerson ? false : _selectedData.placeOnStructureOnly;
                    
                    bool isOnStructure = !effectivePlaceOnStructureOnly || (isFloor && isTopSurface);
                    
                    bool doorValid = true;
                    if (_selectedData.structureType == StructureType.Door)
                    {
                        doorValid = FindWallAtPosition(pos, ghostBuilder.CurrentRotation) != null;
                    }

                    bool gadgetValid = IsValidGadgetPlacement(pos, ghostBuilder.CurrentRotation, _selectedData, _currentHitCollider, _currentHitNormal);

                    if (!(isClear && hasSupport && isOnStructure && doorValid && gadgetValid))
                    {
                        allValid = false;
                    }

                    // Only count cost for items that aren't already placed there
                    if (!isOccupiedBySame)
                    {
                        totalCost += itemPrice;
                    }

                    // เช็คว่า NPC ตัวนี้ถูกวางไปแล้วหรือยัง (ป้องกันการวางซ้ำ)
                    bool isNPC = false;
                    if (Simulation.NPC.NPCSkillManager.Instance != null)
                    {
                        foreach (var npcData in Simulation.NPC.NPCSkillManager.Instance.availableNPCs)
                        {
                            if (npcData != null && npcData.npcName == _selectedData.structureName)
                            {
                                isNPC = true;
                                break;
                            }
                        }
                    }

                    if (isNPC)
                    {
                        // ตรวจสอบในสิ่งก่อสร้างที่วางไว้แล้วว่ามีชื่อเดียวกันหรือไม่
                        bool alreadyPlaced = false;
                        foreach (var unit in _placedStructures)
                        {
                            if (unit != null && unit.gameObject.activeInHierarchy && unit.Data != null && unit.Data.structureName == _selectedData.structureName)
                            {
                                alreadyPlaced = true;
                                break;
                            }
                        }
                        
                        // ถ้าเกิดมีการวางตัวนี้ไปแล้ว หรือผู้เล่นกำลังลากวางหลายตัวพร้อมกัน ให้ปฏิเสธการวาง
                        if (alreadyPlaced || _dragPositions.Count > 1)
                        {
                            allValid = false;
                        }
                    }
                }

                // bool canAfford = _currentBudget >= totalCost;
                // if (!canAfford) allValid = false;
                
                ghostBuilder.UpdateGhosts(_dragPositions, ghostBuilder.CurrentRotation, allValid);

                // Placement execution on mouse up
                if (Input.GetMouseButtonUp(0) && _isDragging)
                {
                    _isDragging = false;
                    if (allValid && _dragPositions.Count > 0)
                    {
                        // จำชิ้นที่มีอยู่ "ก่อน" วาง เพื่อจะหา "ชิ้นที่เพิ่งวาง" ทีหลัง
                        var beforePlace = new HashSet<StructureUnit>(_placedStructures);

                        foreach (var pos in _dragPositions)
                        {
                            // Only place if there isn't already the exact same structure there
                            if (!IsAlreadyOccupiedBySame(pos, ghostBuilder.CurrentRotation, _selectedData))
                            {
                                PlaceStructure(pos, ghostBuilder.CurrentRotation, _currentHitCollider);
                            }
                        }

                        // ── Joint 2nd pass (กัน Joint bug ตอนลากวาง) ──
                        // เวลาวางทีละชิ้น ชิ้นที่วางก่อน "ยังไม่เห็น" ชิ้นที่วางทีหลัง → ผูก Joint กันไม่ครบ
                        // วางครบแล้วจึงผูก Joint ใหม่ให้ชิ้นที่เพิ่งวางทั้งหมด (เหมือน 2-pass ใน ConfirmGroupMove)
                        UnityEngine.Physics.SyncTransforms();
                        foreach (var u in _placedStructures)
                        {
                            if (u == null || !u.gameObject.activeInHierarchy) continue;
                            if (beforePlace.Contains(u)) continue; // เฉพาะชิ้นที่เพิ่งวางรอบนี้
                            AttachJoint(u.gameObject, null);
                            AttachSideJoints(u.gameObject);
                            IgnoreOverlappingCollisions(u);
                        }
                    }
                    _dragPositions.Clear();
                    EndBatch();
                }
            }
        }

        /// <summary>
        /// คำนวณตำแหน่งวางทั้งหมดจากการลาก
        /// - Normal: เติมเต็ม 2D พื้นที่สี่เหลี่ยม (X, Z)
        /// - Wall/Door: สร้างเป็นเส้นตรง 1D ตามแกนที่ลากยาวที่สุด
        /// </summary>
        private List<Vector3> CalculateDragPositions(Vector3 start, Vector3 end, float sizeX, float sizeZ, float rotation, StructureType type)
        {
            List<Vector3> positions = new List<Vector3>();

            // สลับแกนเมื่อหมุน 90 หรือ 270 องศา
            if (Mathf.Abs(rotation % 180f) > 45f)
            {
                float tmp = sizeX;
                sizeX = sizeZ;
                sizeZ = tmp;
            }

            float stepX = sizeX * gridSize;
            float stepZ = sizeZ * gridSize;

            float dx = end.x - start.x;
            float dz = end.z - start.z;

            if (type == StructureType.Normal || type == StructureType.Floor)
            {
                // ── 2D Fill in local space (Rotates the whole rectangle like Move Mode) ──
                Quaternion q = Quaternion.Euler(0, rotation, 0);
                Vector3 rel = end - start;
                Vector3 localRel = Quaternion.Inverse(q) * rel;

                int stepsX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Abs(localRel.x) / stepX + 0.5f));
                int stepsZ = Mathf.Max(0, Mathf.FloorToInt(Mathf.Abs(localRel.z) / stepZ + 0.5f));

                float signX = localRel.x >= 0 ? 1f : -1f;
                float signZ = localRel.z >= 0 ? 1f : -1f;

                for (int ix = 0; ix <= stepsX; ix++)
                {
                    for (int iz = 0; iz <= stepsZ; iz++)
                    {
                        Vector3 localPos = new Vector3(ix * stepX * signX, 0, iz * stepZ * signZ);
                        positions.Add(start + (q * localPos));
                    }
                }
            }
            else
            {
                // ── 1D Line in local space (Rotates the whole line like Move Mode) ──
                Quaternion q = Quaternion.Euler(0, rotation, 0);
                Vector3 rel = end - start;
                Vector3 localRel = Quaternion.Inverse(q) * rel;

                // Determine which local axis to extend along based on mouse position relative to rotation
                int stepsX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Abs(localRel.x) / stepX + 0.5f));
                int stepsZ = Mathf.Max(0, Mathf.FloorToInt(Mathf.Abs(localRel.z) / stepZ + 0.5f));

                if (stepsX >= stepsZ)
                {
                    float signX = localRel.x >= 0 ? 1f : -1f;
                    for (int i = 0; i <= stepsX; i++)
                    {
                        Vector3 localPos = new Vector3(i * stepX * signX, 0, 0);
                        positions.Add(start + (q * localPos));
                    }
                }
                else
                {
                    float signZ = localRel.z >= 0 ? 1f : -1f;
                    for (int i = 0; i <= stepsZ; i++)
                    {
                        Vector3 localPos = new Vector3(0, 0, i * stepZ * signZ);
                        positions.Add(start + (q * localPos));
                    }
                }
            }

            return positions;
        }

        // --------------------------------------------------------------------------------
        // MOVE MODE - Pickup Phase
        // --------------------------------------------------------------------------------

        private void HandlePickupMode()
        {
            if (Input.GetMouseButtonUp(1) && Time.time - _rightClickDownTime <= RIGHT_CLICK_CANCEL_HOLD_THRESHOLD) { ExitMode(); return; }

            if (_hasValidTarget)
            {
                Vector3 currentPos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider);

                if (Input.GetMouseButtonDown(0))
                {
                    if (_hoveredUnit != null)
                    {
                        // Start tracking for Click vs Drag
                        _isHoldingPickup = true;
                        _pickupStartTime = Time.time;
                        _pickupStartMousePos = Input.mousePosition;
                    }
                    else
                    {
                        // Start area selection for group moving
                        _isDragging = true;
                        _dragStartPos = currentPos;
                        _dragStartNormal = _currentHitNormal;
                        _dragStartCollider = _currentHitCollider;
                        _dragSelectedUnits.Clear();
                    }
                }

                if (_isHoldingPickup)
                {
                    float holdTime = Time.time - _pickupStartTime;
                    float mouseDist = Vector3.Distance(Input.mousePosition, _pickupStartMousePos);

                    // If they move the mouse or hold it long enough, it's a drag
                    if (holdTime > 0.25f || mouseDist > 15f)
                    {
                        List<StructureUnit> building = FindConnectedUnits(_hoveredUnit);
                        if (building.Count > 1)
                        {
                            EnterGroupMoveMode(building);
                        }
                        else
                        {
                            EnterMovingSubmode(_hoveredUnit);
                        }
                        _isDraggingMove = true; // Drag mode: place on release
                        _isHoldingPickup = false;
                    }
                    // If they release quickly without moving, it's a click-to-pick (SINGLE PIECE ONLY)
                    else if (Input.GetMouseButtonUp(0))
                    {
                        EnterMovingSubmode(_hoveredUnit);
                        _isDraggingMove = false; // Click mode: stick to mouse, place on next click
                        _isHoldingPickup = false;
                    }
                }

                if (_isDragging)
                {
                    Vector3 dragCurrentPos = CalculatePlacementPosition(_currentHitPos, _dragStartNormal, _dragStartCollider);
                    _dragPositions = CalculateDragPositions(_dragStartPos, dragCurrentPos, 1f, 1f, 0f, StructureType.Normal);
                    
                    foreach (var unit in _dragSelectedUnits) if (unit != null) unit.SetHighlight(false);
                    _dragSelectedUnits.Clear();

                    foreach (var pos in _dragPositions)
                    {
                        // Raycast from high above to support multiple floors
                        Vector3 rayStart = pos + Vector3.up * 50.0f;
                        RaycastHit[] selectionHits = UnityEngine.Physics.RaycastAll(rayStart, Vector3.down, 100.0f, structureLayer, QueryTriggerInteraction.Collide);
                        foreach (var hit in selectionHits)
                        {
                            StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                            if (unit != null)
                            {
                                _dragSelectedUnits.Add(unit);
                                unit.SetHighlight(true, Color.cyan);
                            }
                        }
                    }
                    ghostBuilder.UpdateGhosts(_dragPositions, 0f, true);
                }
            }

            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                _isDragging = false;
                if (_dragSelectedUnits.Count > 0)
                {
                    EnterGroupMoveMode(new List<StructureUnit>(_dragSelectedUnits));
                }
                _dragSelectedUnits.Clear();
                // Removed ghostBuilder.DestroyGhost() here - it was causing selected groups to disappear
            }
        }

        private void EnterGroupMoveMode(List<StructureUnit> units)
        {
            ClearHover();
            _movingGroup = units;
            _groupOffsets.Clear();
            _groupRotations.Clear();
            _groupOriginalStates.Clear();

            // Use the first unit or selection center as anchor? 
            // Let's use the average position.
            // Calculate horizontal center and vertical bottom
            Vector3 centerSum = Vector3.zero;
            float minBottomY = float.MaxValue;
            
            foreach (var u in units) 
            {
                centerSum += u.transform.position;
                
                // Calculate absolute bottom of this unit using its colliders
                foreach (Collider col in u.GetComponentsInChildren<Collider>())
                {
                    if (col.bounds.min.y < minBottomY) minBottomY = col.bounds.min.y;
                }
            }
            
            // Fallback if no colliders found
            if (minBottomY == float.MaxValue) minBottomY = centerSum.y / units.Count;
            
            Vector3 center = centerSum / units.Count;
            center.y = minBottomY; // Use the absolute lowest point as vertical anchor

            // Reset pivot offset for group anchor
            _pivotToBottomOffset = 0f;

            // Snap center XZ to cell grid directly
            // We ALWAYS use cell center (Normal) snapping for the anchor of a group,
            // even if it contains walls. The walls will have a +/- 0.5 offset which
            // will be preserved during movement, keeping them on the edges.
            float snappedX = SnapToCellCenter(center.x, 1f);
            float snappedZ = SnapToCellCenter(center.z, 1f);
            float yStep = heightStep > 0f ? heightStep : gridSize;
            float snappedY = Mathf.Round(minBottomY / yStep) * yStep;
            Vector3 snappedCenter = new Vector3(snappedX, snappedY, snappedZ);

            List<GameObject> prefabs = new List<GameObject>();
            foreach (var unit in units)
            {
                _groupOriginalStates.Add(new GroupOriginalState { unit = unit, pos = unit.transform.position, rot = unit.Rotation });
                _groupOffsets.Add(unit.transform.position - snappedCenter);
                _groupRotations.Add(unit.Rotation);
                prefabs.Add(unit.Data.prefab);
                unit.gameObject.SetActive(false);
            }

            ghostBuilder.CreateGroupGhost(prefabs, _groupOffsets, _groupRotations);
            _isDraggingMove = false; // Area selection move is click-to-place, not hold-to-drag
        }

        private void EnterMovingSubmode(StructureUnit unit)
        {
            ClearHover();
            
            // Save original state for Undo
            _moveOriginalPos = unit.transform.position;
            _moveOriginalRot = unit.Rotation;
            var joint = unit.GetComponent<Joint>();
            if (joint != null && joint.connectedBody != null)
                _moveOriginalTargetCol = joint.connectedBody.GetComponentInChildren<Collider>();
            else
                _moveOriginalTargetCol = null;
                
            _movingUnit = unit;
            _movingUnit.gameObject.SetActive(false);
            
            // Use Data-based offset to match 'Create New' behavior, but pass the instance 
            // to account for scaling if possible.
            _pivotToBottomOffset = GetPivotToBottomOffset(_movingUnit.Data, _movingUnit.gameObject);
            
            // ── Door Frame Ghost (Move) ──
            if (_movingUnit.Data.structureType == StructureType.Door && _movingUnit.Data.doorReplacementPrefab != null)
            {
                List<GameObject> prefabs = new List<GameObject> { _movingUnit.Data.prefab, _movingUnit.Data.doorReplacementPrefab };
                List<Vector3> offsets = new List<Vector3> { Vector3.zero, Vector3.zero };
                List<float> rotations = new List<float> { 0f, 0f };
                ghostBuilder.CreateGroupGhost(prefabs, offsets, rotations);
            }
            else
            {
                ghostBuilder.CreateGhost(_movingUnit.Data.prefab);
            }
            
            ghostBuilder.SetRotation(_movingUnit.Rotation); // Match original rotation
        }

        // --------------------------------------------------------------------------------
        // MOVE MODE - Placement Phase
        // --------------------------------------------------------------------------------

        private void HandleMovingMode()
        {
            if (Input.GetMouseButtonUp(1) && Time.time - _rightClickDownTime <= RIGHT_CLICK_CANCEL_HOLD_THRESHOLD) { CancelCurrentMove(); return; }
            if (Input.GetKeyDown(KeyCode.R)) ghostBuilder.Rotate();

            if (_hasValidTarget && ghostBuilder.HasGhost)
            {
                // For groups, we force 'Normal' snapping (cell center) for the anchor.
                // For single units, we use their specific snapping logic (e.g. walls snap to edges).
                bool forceNormal = _movingGroup.Count > 0;
                Vector3 placePos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider, forceNormal);
                ghostBuilder.UpdatePosition(placePos);
                
                bool allValid = true;
                if (_movingGroup.Count > 0)
                {
                    // Check validity for entire group
                    Quaternion groupRotQ = Quaternion.Euler(0, ghostBuilder.CurrentRotation, 0);
                    
                    // 1. Calculate all projected positions and check if at least one piece has world support
                    bool groupHasSupport = false;
                    List<Vector3> projectedPositions = new List<Vector3>();
                    List<float> projectedRotations = new List<float>();

                    for (int j = 0; j < _movingGroup.Count; j++)
                    {
                        Vector3 pPos = placePos + (groupRotQ * _groupOffsets[j]);
                        float pRot = (groupRotQ * Quaternion.Euler(0, _groupRotations[j], 0)).eulerAngles.y;
                        projectedPositions.Add(pPos);
                        projectedRotations.Add(pRot);

                        if (IsSupportedByWorld(pPos, pRot, _movingGroup[j].Data))
                        {
                            groupHasSupport = true;
                        }
                    }

                    // 2. Validate collision and support for each piece
                    for (int i = 0; i < _movingGroup.Count; i++)
                    {
                        StructureUnit unit = _movingGroup[i];
                        Vector3 piecePos = projectedPositions[i];
                        float pieceRot = projectedRotations[i];

                        bool isClear = IsAreaClear(piecePos, pieceRot, unit.Data);
                        // A group is supported if at least one of its members touches a support
                        bool hasSupport = groupHasSupport;

                        bool gadgetValid = IsValidGadgetPlacement(piecePos, pieceRot, unit.Data, _currentHitCollider, _currentHitNormal);

                        if (!isClear || !hasSupport || !gadgetValid) { allValid = false; break; }
                    }
                }
                else
                {
                    // Single unit validity — same logic as placing a new structure
                    bool isClear = IsAreaClear(placePos, ghostBuilder.CurrentRotation, _movingUnit.Data);
                    bool hasSupport = HasStructuralSupport(placePos, ghostBuilder.CurrentRotation, _movingUnit.Data);
                    
                    // placeOnStructureOnly check: needs to land on top of a floor structure
                    bool isOnStructure = true;
                    if (_movingUnit.Data.placeOnStructureOnly)
                    {
                        StructureUnit hitUnit = _currentHitCollider != null ? _currentHitCollider.GetComponentInParent<StructureUnit>() : null;
                        bool isFloor = hitUnit != null && hitUnit.Data != null && hitUnit.Data.structureType == StructureType.Floor;
                        bool isTopSurface = _currentHitNormal.y > 0.9f;
                        isOnStructure = isFloor && isTopSurface;
                    }
                    
                    bool gadgetValid = IsValidGadgetPlacement(placePos, ghostBuilder.CurrentRotation, _movingUnit.Data, _currentHitCollider, _currentHitNormal);
                    
                    allValid = isClear && hasSupport && isOnStructure && gadgetValid;
                }
                
                ghostBuilder.SetValid(allValid);

                if (Input.GetMouseButtonDown(0) || (Input.GetMouseButtonUp(0) && _isDraggingMove))
                {
                    if (ghostBuilder.IsValid)
                    {
                        if (_movingGroup.Count > 0) ConfirmGroupMove(placePos, ghostBuilder.CurrentRotation);
                        else ConfirmMove(placePos, ghostBuilder.CurrentRotation, _currentHitCollider);
                        _isDraggingMove = false;
                    }
                }
            }
        }

        // --------------------------------------------------------------------------------
        // DELETE MODE
        // --------------------------------------------------------------------------------

        private void HandleDeleteMode()
        {
            if (Input.GetMouseButtonUp(1) && Time.time - _rightClickDownTime <= RIGHT_CLICK_CANCEL_HOLD_THRESHOLD) { ExitMode(); return; }
            if (_hasValidTarget)
            {
                // Instant single-click deletion if hovering over a unit
                if (Input.GetMouseButtonDown(0) && _hoveredUnit != null)
                {
                    TrySellStructure(_hoveredUnit);
                    _hoveredUnit = null; 
                    return; // Done
                }

                Vector3 currentPos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider);

                if (Input.GetMouseButtonDown(0))
                {
                    _isDragging = true;
                    // Snap the start point immediately
                    _dragStartPos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider);
                    _dragStartNormal = _currentHitNormal;
                    _dragStartCollider = _currentHitCollider;
                    BeginBatch();
                    _dragSelectedUnits.Clear();
                }

                if (_isDragging)
                {
                    Vector3 dragCurrentPos = CalculatePlacementPosition(_currentHitPos, _dragStartNormal, _dragStartCollider);
                    _dragPositions = CalculateDragPositions(_dragStartPos, dragCurrentPos, 1f, 1f, 0f, StructureType.Normal);

                    // Clear old selection highlights
                    foreach (var unit in _dragSelectedUnits)
                    {
                        if (unit != null) unit.SetHighlight(false);
                    }
                    _dragSelectedUnits.Clear();

                    // Find units in area
                    foreach (var pos in _dragPositions)
                    {
                        // Raycast from high above to find all units at this grid position across all floors
                        Vector3 rayStart = pos + Vector3.up * 50.0f;
                        RaycastHit[] hits = UnityEngine.Physics.RaycastAll(rayStart, Vector3.down, 100.0f, structureLayer, QueryTriggerInteraction.Collide);
                        foreach (var hit in hits)
                        {
                            StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                            if (unit != null)
                            {
                                _dragSelectedUnits.Add(unit);
                                unit.SetHighlight(true, Color.red);
                            }
                        }
                    }

                    ghostBuilder.UpdateGhosts(_dragPositions, 0f, false);
                }
            }

            if (Input.GetMouseButtonUp(0) && _isDragging)
            {
                _isDragging = false;
                foreach (var unit in _dragSelectedUnits)
                {
                    if (unit != null) TrySellStructure(unit);
                }
                _dragSelectedUnits.Clear();
                _dragPositions.Clear();
                ghostBuilder.DestroyGhost();
                EndBatch();
            }
        }

        // --------------------------------------------------------------------------------
        // PAINT MODE
        // --------------------------------------------------------------------------------

        private void HandlePaintingMode()
        {
            if (Input.GetMouseButtonUp(1) && Time.time - _rightClickDownTime <= RIGHT_CLICK_CANCEL_HOLD_THRESHOLD) { ExitMode(); return; }

            if (Input.GetMouseButtonDown(0))
            {
                BeginBatch();
            }

            if (Input.GetMouseButton(0) && _hoveredUnit != null && _selectedMaterial != null)
            {
                // Gadget เปลี่ยน Material ไม่ได้
                if (_hoveredUnit.Data == null || !_hoveredUnit.Data.isGadget)
                {
                    ApplyMaterialToStructure(_hoveredUnit, _selectedMaterial);
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                EndBatch();
            }
        }

        // --------------------------------------------------------------------------------
        // PUBLIC INTERFACE (for UI)
        // --------------------------------------------------------------------------------

        public void SelectStructure(StructureData data)
        {
            if (!CanEnterBuildMode()) return;

            // Save current material before ExitMode clears it
            MaterialData savedMaterial = _selectedMaterial;
            ExitMode();
            _selectedData = data;
            _selectedMaterial = savedMaterial; // Restore material selection
            _currentMode = BuildMode.Placing;
            _justEnteredPlacing = true; // Prevent this frame's click from placing

            if (data != null && data.prefab != null)
            {
                _pivotToBottomOffset = GetPivotToBottomOffset(data);
                
                // ── Door Frame Ghost ──
                // If this is a door and has a replacement prefab, show both in the preview.
                if (data.structureType == StructureType.Door && data.doorReplacementPrefab != null)
                {
                    List<GameObject> prefabs = new List<GameObject> { data.prefab, data.doorReplacementPrefab };
                    List<Vector3> offsets = new List<Vector3> { Vector3.zero, Vector3.zero };
                    List<float> rotations = new List<float> { 0f, 0f };
                    ghostBuilder.CreateGroupGhost(prefabs, offsets, rotations);
                }
                else
                {
                    ghostBuilder.CreateGhost(data.prefab);
                }
            }
        }

        /// <summary>
        /// เริ่มโหมดสร้างโดยใช้ข้อมูลจาก GadgetData (Gadget)
        /// โดยจะแปลงเป็น StructureData ชั่วคราวเพื่อให้ระบบหลักทำงานได้
        /// </summary>
        public void SelectFurniture(GadgetData data)
        {
            if (data == null) return;

            // สร้าง StructureData จำลองขึ้นมา
            StructureData proxy = ScriptableObject.CreateInstance<StructureData>();
            proxy.structureName = data.GadgetName;
            proxy.basePrice = data.Price;
            proxy.baseMass = data.Mass;
            proxy.baseHP = data.HP;
            proxy.size = data.size;
            proxy.prefab = data.prefab;
            proxy.defaultMaterial = null;
            proxy.allowOverlap = data.allowOverlap;
            proxy.isGadget = true;
            proxy.placeOnStructureOnly = false;  // Gadget วางบน ground และ floor ได้
            proxy.breakVFX = data.breakVFX;
            proxy.breakSFX = data.breakSound;
            
            // เรียกใช้ระบบเลือกปกติ
            SelectStructure(proxy);
        }

        public void EnterMoveMode()
        {
            if (!CanEnterBuildMode()) return;
            ExitMode();
            _currentMode = BuildMode.Moving;
        }

        public void EnterDeleteMode()
        {
            if (!CanEnterBuildMode()) return;
            ExitMode();
            _currentMode = BuildMode.Deleting;
        }

        public void EnterPaintMode()
        {
            if (!CanEnterBuildMode()) return;
            ExitMode();
            _currentMode = BuildMode.Painting;
        }

        private bool CanEnterBuildMode()
        {
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                Debug.LogWarning("[BuildingSystem] Cannot enter build mode while simulation is running.");
                return false;
            }
            return true;
        }

        public void SelectMaterial(MaterialData material)
        {
            _selectedMaterial = material;
        }

        public void ExitMode()
        {
            if (_movingUnit != null) _movingUnit.gameObject.SetActive(true);
            foreach (var u in _movingGroup) if (u != null) u.gameObject.SetActive(true);
            _movingUnit = null;
            _movingGroup.Clear();
            _groupOffsets.Clear();
            _groupRotations.Clear();
            _groupOriginalStates.Clear();
            _selectedData = null;
            // Material persists across mode changes — don't clear it here
            _currentMode = BuildMode.Idle;
            _justEnteredPlacing = false;
            // Reset drag state to prevent carry-over when switching structures
            foreach (var unit in _dragSelectedUnits) if (unit != null) unit.SetHighlight(false);
            _isDragging = false;
            _isDraggingMove = false;
            _isHoldingPickup = false;
            _dragPositions.Clear();
            _dragSelectedUnits.Clear();
            EndBatch();
            ghostBuilder.DestroyGhost();
            ClearHover();
        }

        /// <summary>
        /// Fully clear the selected material (e.g. from a "reset material" button).
        /// </summary>
        public void ClearMaterial()
        {
            _selectedMaterial = null;
        }

        /// <summary>
        /// ลบโครงสร้างทั้งหมดที่วางไว้ คืนเงินทั้งหมด (ใช้กับ UI กดค้าง)
        /// </summary>
        public void DeleteAllStructures()
        {
            ExitMode();

            // คืนเงินและลบทีละตัว (ไม่ใส่ Undo เพราะเป็นการ Clear ทั้งหมด)
            for (int i = _placedStructures.Count - 1; i >= 0; i--)
            {
                StructureUnit unit = _placedStructures[i];
                if (unit == null) continue;

                float materialPrice = unit.CurrentMaterial != null ? unit.CurrentMaterial.priceModifier : 0f;
                float sellPrice = unit.Data.basePrice + materialPrice + unit.AdditionalRefundValue;
                _currentBudget += sellPrice;

                // ทำความสะอาด Joint ที่อ้างอิงถึงตัวนี้
                CleanupJointsReferencingUnit(unit);

                // ลบ Joint ก่อน
                var joints = unit.GetComponents<Joint>();
                foreach (var j in joints) Destroy(j);

                // เล่น VFX/SFX
                if (generalSellVFX != null) Instantiate(generalSellVFX, unit.transform.position, Quaternion.identity);

                Destroy(unit.gameObject);
            }

            _placedStructures.Clear();
            _undoStack.Clear();
            _redoStack.Clear();

            if (generalSellSound != null && mainCamera != null)
                AudioSource.PlayClipAtPoint(generalSellSound, mainCamera.transform.position);

            RecalculateMaxFloor();
            Debug.Log("<color=orange>🗑 Deleted ALL structures</color>");
        }

        // --------------------------------------------------------------------------------
        // INTERNAL LOGIC
        // --------------------------------------------------------------------------------

        private void PlaceStructure(Vector3 position, float rotation, Collider targetCollider = null)
        {
            // Gadget ใช้ Material เริ่มต้นเท่านั้น (เปลี่ยนไม่ได้)
            MaterialData mat = (_selectedData.isGadget)
                ? _selectedData.defaultMaterial
                : (_selectedMaterial != null ? _selectedMaterial : _selectedData.defaultMaterial);
            float materialPrice = mat != null ? mat.priceModifier : 0f;
            float totalCost = GetEffectivePrice(_selectedData.basePrice + materialPrice);

            // ── Door: find and replace the wall underneath ──
            StructureUnit replacedWall = null;
            if (_selectedData.structureType == StructureType.Door)
            {
                replacedWall = FindWallAtPosition(position, rotation);
                if (replacedWall == null)
                {
                    Debug.LogWarning("[BuildingSystem] Door placement failed: no wall found at target position.");
                    return;
                }
            }

            float snappedRotation = Mathf.Round(rotation / 90f) * 90f;
            GameObject obj = Instantiate(_selectedData.prefab, position, Quaternion.Euler(0, snappedRotation, 0));

            // ── Door Frame Replacement ──
            // If this is a door and it has a replacement prefab (like a DoorFrame),
            // instantiate it and parent it to the door so they are managed together.
            if (_selectedData.structureType == StructureType.Door && _selectedData.doorReplacementPrefab != null)
            {
                GameObject frame = Instantiate(_selectedData.doorReplacementPrefab, position, Quaternion.Euler(0, snappedRotation, 0));
                frame.transform.SetParent(obj.transform);
            }

            SetLayerRecursively(obj, structureLayer);
            obj.name = $"{_selectedData.prefab.name} {GetGridPositionString(position)}";

            StructureUnit unit = obj.GetComponent<StructureUnit>() ?? obj.AddComponent<StructureUnit>();
            unit.Initialize(_selectedData, mat, snappedRotation);

            // หากเป็นการวางประตู ให้เก็บมูลค่ากำแพงที่โดนทับไว้เพื่อคืนตอนขาย
            if (replacedWall != null)
            {
                float wallMatPrice = replacedWall.CurrentMaterial != null ? replacedWall.CurrentMaterial.priceModifier : 0f;
                float wallValue = replacedWall.Data.basePrice + wallMatPrice;
                unit.SetAdditionalRefundValue(wallValue);
            }
            
            // Start disabled so the Command can enable it
            obj.SetActive(false);

            // Capture the replaced wall for undo/redo
            StructureUnit capturedWall = replacedWall;

            ExecuteCommand(
                execute: () => {
                    _currentBudget -= totalCost;

                    // Hide the wall that the door replaces
                    if (capturedWall != null)
                    {
                        CleanupJointsReferencingUnit(capturedWall);
                        _placedStructures.Remove(capturedWall);
                        var wallJoints = capturedWall.GetComponents<Joint>();
                        foreach (var j in wallJoints) Destroy(j);
                        capturedWall.gameObject.SetActive(false);
                    }

                    obj.SetActive(true);
                    AttachJoint(obj, targetCollider);
                    _placedStructures.Add(unit);
                    AttachSideJoints(obj);
                    IgnoreOverlappingCollisions(unit);

                    if (mat != null)
                    {
                        if (mat.placeSound != null) AudioSource.PlayClipAtPoint(mat.placeSound, position);
                        if (mat.placeVFX != null) Instantiate(mat.placeVFX, position, Quaternion.identity);
                    }
                    else if (generalPlaceSound != null)
                    {
                        AudioSource.PlayClipAtPoint(generalPlaceSound, position);
                    }
                },
                undo: () => {
                    _currentBudget += totalCost;
                    CleanupJointsReferencingUnit(unit);
                    _placedStructures.Remove(unit);
                    
                    var joints = obj.GetComponents<Joint>();
                    foreach (var j in joints) Destroy(j);
                    
                    obj.SetActive(false);

                    // Restore the wall that was replaced
                    if (capturedWall != null)
                    {
                        capturedWall.gameObject.SetActive(true);
                        _placedStructures.Add(capturedWall);
                        AttachJoint(capturedWall.gameObject, targetCollider);
                        AttachSideJoints(capturedWall.gameObject);
                        IgnoreOverlappingCollisions(capturedWall);
                    }
                }
            );

            RecalculateMaxFloor();
        }

        private void ConfirmGroupMove(Vector3 anchorPos, float groupRotation)
        {
            List<GroupOriginalState> oldStates = new List<GroupOriginalState>(_groupOriginalStates);
            List<StructureUnit> group = new List<StructureUnit>(_movingGroup);
            List<Vector3> offsets = new List<Vector3>(_groupOffsets);
            List<float> rots = new List<float>(_groupRotations);

            ExecuteCommand(
                execute: () => {
                    Quaternion groupRotQ = Quaternion.Euler(0, groupRotation, 0);
                    // Pass 1: Set positions and enable everything
                    for (int i = 0; i < group.Count; i++)
                    {
                        Vector3 rotatedOffset = groupRotQ * offsets[i];
                        Vector3 piecePos = anchorPos + rotatedOffset;
                        float pieceRot = (groupRotQ * Quaternion.Euler(0, rots[i], 0)).eulerAngles.y;

                        group[i].transform.position = piecePos;
                        group[i].transform.rotation = Quaternion.Euler(0, pieceRot, 0);
                        group[i].SetRotation(pieceRot);
                        group[i].gameObject.SetActive(true);
                        group[i].SetHighlight(false);
                    }

                    // Pass 2: Attach joints once all pieces are active and in place
                    foreach (var u in group)
                    {
                        AttachJoint(u.gameObject, null);
                        AttachSideJoints(u.gameObject);
                        IgnoreOverlappingCollisions(u);
                    }
                },
                undo: () => {
                    // Pass 1: Restore positions
                    foreach (var state in oldStates)
                    {
                        state.unit.transform.position = state.pos;
                        state.unit.transform.rotation = Quaternion.Euler(0, state.rot, 0);
                        state.unit.SetRotation(state.rot);
                        state.unit.gameObject.SetActive(true);
                        state.unit.SetHighlight(false);
                    }

                    // Pass 2: Restore joints
                    foreach (var state in oldStates)
                    {
                        AttachJoint(state.unit.gameObject, null);
                        AttachSideJoints(state.unit.gameObject);
                        IgnoreOverlappingCollisions(state.unit);
                    }
                }
            );

            _movingGroup.Clear();
            _movingUnit = null;
            ghostBuilder.DestroyGhost();
            RecalculateMaxFloor();
        }

        public void RefreshAllJoints()
        {
            Debug.Log("[BuildingSystem] Refreshing all joints in scene...");
            
            // สำคัญ: ต้อง SyncTransforms เพื่อให้ Collider ทุกตัวอยู่ในตำแหน่งล่าสุดจริงๆ ก่อนยิงเรย์
            UnityEngine.Physics.SyncTransforms();

            foreach (var unit in _placedStructures)
            {
                if (unit == null || !unit.gameObject.activeInHierarchy) continue;
                
                // สั่งให้ชิ้นส่วนนั้นๆ หาจุดยึดใหม่
                // 1. หาจุดยึดพื้น/โครงสร้างล่าง
                AttachJoint(unit.gameObject, null);
                // 2. หาจุดยึดซ้ายขวาหน้าหลัง (บนล่าง)
                AttachSideJoints(unit.gameObject);
                // 3. ปิดการชนกันเองเพื่อป้องกันการระเบิด
                IgnoreOverlappingCollisions(unit);
            }
        }

        /// <summary>
        /// พยายามผูก Joint ใหม่ให้ชิ้นที่ "เสียตัวค้ำ" ระหว่างจำลอง (เช่น พื้น/ตัวค้ำพัง)
        /// คืน true ถ้าหาจุดยึดใหม่ได้ (พื้น/ground/เพื่อนบ้านที่ยังเหลือ), false ถ้าไม่มีที่ยึดเหลือจริงๆ
        /// ใช้ preserveGroundAnchor:false เพื่อไม่ให้สร้าง "สมอยึดพื้นปลอม" — ต้องเจอ support จริงเท่านั้น
        /// </summary>
        public bool TryReattachJoint(GameObject obj)
        {
            if (obj == null) return false;
            UnityEngine.Physics.SyncTransforms();
            AttachJoint(obj, null, false);
            AttachSideJoints(obj);
            return obj.GetComponent<Joint>() != null;
        }

        private void ConfirmMove(Vector3 position, float rotation, Collider targetCollider = null)
        {
            Vector3 oldPos = _moveOriginalPos;
            float oldRot = Mathf.Round(_moveOriginalRot / 90f) * 90f;
            float snappedRotation = Mathf.Round(rotation / 90f) * 90f;
            Collider oldTarget = _moveOriginalTargetCol;
            StructureUnit unit = _movingUnit;

            ExecuteCommand(
                execute: () => {
                    unit.transform.position = position;
                    unit.transform.rotation = Quaternion.Euler(0, snappedRotation, 0);
                    unit.SetRotation(snappedRotation);
                    unit.name = $"{unit.Data.prefab.name} {GetGridPositionString(position)}";
                    unit.gameObject.SetActive(true);
                    unit.SetHighlight(false);
                    AttachJoint(unit.gameObject, targetCollider);
                    AttachSideJoints(unit.gameObject);
                    IgnoreOverlappingCollisions(unit);

                    if (unit.CurrentMaterial != null && unit.CurrentMaterial.placeSound != null) 
                        AudioSource.PlayClipAtPoint(unit.CurrentMaterial.placeSound, position);
                    else if (generalPlaceSound != null)
                        AudioSource.PlayClipAtPoint(generalPlaceSound, position);
                },
                undo: () => {
                    unit.transform.position = oldPos;
                    unit.transform.rotation = Quaternion.Euler(0, oldRot, 0);
                    unit.SetRotation(oldRot);
                    unit.name = $"{unit.Data.prefab.name} {GetGridPositionString(oldPos)}";
                    unit.gameObject.SetActive(true);
                    unit.SetHighlight(false);
                    AttachJoint(unit.gameObject, oldTarget);
                    AttachSideJoints(unit.gameObject);
                    IgnoreOverlappingCollisions(unit);
                }
            );

            _movingUnit = null;
            ghostBuilder.DestroyGhost();
            RecalculateMaxFloor();
        }

        private void AttachJoint(GameObject structureObj, Collider targetCollider, bool preserveGroundAnchor = true)
        {
            // Sync physics transforms immediately to ensure newly activated objects have valid bounds in world space
            UnityEngine.Physics.SyncTransforms();

            Rigidbody newRb = structureObj.GetComponent<Rigidbody>();
            if (newRb == null) return;

            StructureUnit newUnit = structureObj.GetComponent<StructureUnit>();
            if (newUnit == null) return;

            // 1. Remove existing joints immediately to avoid stale components in the same frame
            Joint[] existingJoints = structureObj.GetComponents<Joint>();
            // จำไว้ก่อนลบ: เดิมชิ้นนี้ "ยึดกับพื้น/โลก" อยู่ไหม (ground anchor = connectedBody == null)
            // กันเคส recheck/refresh หาจุดยึดใหม่ไม่เจอ แล้วเผลอตัด Joint พื้นทิ้ง
            bool hadGroundAnchor = false;
            foreach (var j in existingJoints)
            {
                if (j != null && j.connectedBody == null) { hadGroundAnchor = true; break; }
            }
            foreach (var j in existingJoints) DestroyImmediate(j);

            // 2. Find the actual collider directly beneath this specific structure (or above if TMD).
            Collider actualTarget = null;
            
            bool isTmd = IsTunedMassDamper(newUnit.Data);
            if (isTmd)
            {
                // สำหรับ TMD: ค้นหาโครงสร้าง (Floor) ด้านบนเพื่อยึด Joint
                float pivotToTop = GetPivotToTopOffset(structureObj);
                Vector3 rayStart = structureObj.transform.position + new Vector3(0, pivotToTop - 0.1f, 0);
                
                Collider myCol = structureObj.GetComponentInChildren<Collider>();
                Vector3 boxHalfExtents = myCol != null ? myCol.bounds.extents : new Vector3(0.5f, 0.05f, 0.5f);
                boxHalfExtents.y = 0.05f;
                boxHalfExtents.x = Mathf.Max(0.05f, boxHalfExtents.x - 0.02f);
                boxHalfExtents.z = Mathf.Max(0.05f, boxHalfExtents.z - 0.02f);

                RaycastHit[] hits = UnityEngine.Physics.BoxCastAll(
                    rayStart,
                    boxHalfExtents,
                    Vector3.up,
                    Quaternion.identity,
                    0.4f,
                    structureLayer,
                    QueryTriggerInteraction.Ignore
                );
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    // กรองเป้าหมายที่ยึดไม่ได้ (ตัวเอง/trigger/ซากพัง-หลุด/gadget/prop ไดนามิก)
                    if (!TryGetAttachCandidate(hit.collider, structureObj, out _, out _))
                        continue;

                    var hitUnit = hit.collider.GetComponentInParent<StructureUnit>();
                    if (hitUnit != null && hitUnit.Data != null && hitUnit.Data.structureType == StructureType.Floor)
                    {
                        actualTarget = hit.collider;
                        break;
                    }
                }
            }
            else
            {
                // Raycast/Boxcast down from slightly ABOVE the bottom of the structure
                float pivotToBottom = GetPivotToBottomOffset(structureObj);
                
                // We start slightly ABOVE the bottom to catch the surface we are placed on,
                // but we must ignore our own colliders.
                Vector3 rayStart = structureObj.transform.position - new Vector3(0, pivotToBottom - 0.1f, 0);

                // ใช้ BoxCollider ท้องถิ่น (Local) เพื่อความแน่นอนของขนาดเมื่อเพิ่ง instantiated
                Vector3 boxHalfExtents = new Vector3(0.5f, 0.05f, 0.5f);
                BoxCollider myBoxCol = structureObj.GetComponentInChildren<BoxCollider>(true);
                if (myBoxCol != null)
                {
                    boxHalfExtents = Vector3.Scale(myBoxCol.size, myBoxCol.transform.lossyScale) * 0.5f;
                }
                else
                {
                    Collider myCol = structureObj.GetComponentInChildren<Collider>(true);
                    if (myCol != null)
                    {
                        boxHalfExtents = myCol.bounds.extents;
                    }
                }

                boxHalfExtents.y = 0.05f; // ทำให้บางในแนวตั้งเพื่อหาแผ่นรองรับ
                boxHalfExtents.x = Mathf.Max(0.05f, boxHalfExtents.x - 0.02f); // หดขอบเล็กน้อยมากๆ เท่านั้น เพื่อไม่ให้พลาดเสา/คานที่ขอบ
                boxHalfExtents.z = Mathf.Max(0.05f, boxHalfExtents.z - 0.02f);

                // หมุนตาม structureObj เพื่อให้ตรงแนวกับวัตถุพอดี (ช่วยให้ตรวจจับถูกต้องทุกองศาการหมุน)
                Quaternion boxRotation = structureObj.transform.rotation;

                RaycastHit[] hits = UnityEngine.Physics.BoxCastAll(
                    rayStart,
                    boxHalfExtents,
                    Vector3.down,
                    boxRotation,
                    0.4f,
                    groundLayer | structureLayer,
                    QueryTriggerInteraction.Ignore
                );
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

                foreach (var hit in hits)
                {
                    // กรองเป้าหมายที่ยึดไม่ได้ (ตัวเอง/trigger/ซากพัง-หลุด/gadget/prop ไดนามิก)
                    // กันเคสเช่น: เศษซากกองอยู่ใต้ชิ้น → เดิมจะเกาะซากแทนพื้น
                    if (!TryGetAttachCandidate(hit.collider, structureObj, out _, out _))
                        continue;

                    actualTarget = hit.collider;
                    break;
                }

                // Fallback: ตรวจ "พื้น/สภาพแวดล้อม/โครงสร้าง" ที่อยู่ใต้ฐานโดยตรง ด้วย OverlapBox
                // เหตุผลที่ไม่ใช้ BoxCast: BoxCast ของ Unity "มองไม่เห็น" collider ที่กล่องซ้อนทับ
                // อยู่ตั้งแต่เริ่ม cast → ถ้าชิ้นจมลงพื้น/ตัวค้ำเล็กน้อย (จาก snap หรือทศนิยม)
                // จะหาพื้นไม่เจอเลยทั้งที่วางทับอยู่ — นี่คือสาเหตุหลักของ "พื้นไม่ต่อ joint กับ ground"
                // OverlapBox เจอเสมอแม้ซ้อนกัน. ไม่ผูก Y=0 และไม่ผูก Layer (ยิงทุกชั้น)
                if (actualTarget == null)
                {
                    // ใช้ "ขอบล่างจริง" ของ Collider (ไม่พึ่ง pivot math ที่อาจคลาดเคลื่อน → ยิงผิดที่)
                    Collider baseCol = structureObj.GetComponentInChildren<Collider>();
                    Vector3 cc = baseCol != null ? baseCol.bounds.center : structureObj.transform.position;
                    float realBottom = baseCol != null ? baseCol.bounds.min.y : (structureObj.transform.position.y - pivotToBottom);

                    // แผ่นบางครอบฐาน: จาก realBottom+0.10 ลงไปถึง realBottom-0.20
                    // (ครอบทั้งเคส "จมลงไป" และ "ลอยห่างเล็กน้อย" — สั้นพอไม่ให้ของลอยไกลยึดมั่ว)
                    Vector3 slabHalf = boxHalfExtents;   // footprint เดิม (หดขอบ 2cm กันเกี่ยวเพื่อนบ้านด้านข้าง)
                    slabHalf.y = 0.15f;
                    Vector3 slabCenter = new Vector3(cc.x, realBottom - 0.05f, cc.z);

                    Collider[] underCols = UnityEngine.Physics.OverlapBox(
                        slabCenter, slabHalf, boxRotation, ~0, QueryTriggerInteraction.Ignore);

                    // เลือกโครงสร้างที่ใกล้จุดกึ่งกลางฐานที่สุดก่อน, ไม่มีค่อยใช้พื้น/สภาพแวดล้อม (ground)
                    Collider bestStructure = null, bestGround = null;
                    float bestSqrDist = float.MaxValue;
                    Vector3 basePoint = new Vector3(cc.x, realBottom, cc.z);

                    foreach (var uc in underCols)
                    {
                        if (!TryGetAttachCandidate(uc, structureObj, out _, out bool groundLike)) continue;

                        if (groundLike)
                        {
                            if (bestGround == null) bestGround = uc;
                            continue;
                        }

                        // โครงสร้าง: ใช้ระยะจากจุดกึ่งกลางฐาน (collider โครงสร้างถูกบังคับ convex แล้ว
                        // ใน StructureUnit.Initialize → ClosestPoint ใช้ได้ปลอดภัย)
                        float sqrDist = (uc.ClosestPoint(basePoint) - basePoint).sqrMagnitude;
                        if (sqrDist < bestSqrDist)
                        {
                            bestSqrDist = sqrDist;
                            bestStructure = uc;
                        }
                    }

                    actualTarget = bestStructure != null ? bestStructure : bestGround;
                }
            }

            // Fallback to targetCollider ONLY if it's adjacent/touching
            // This prevents dragged structures from forming long invisible joints to the start point
            if (actualTarget == null && targetCollider != null)
            {
                if (isTmd)
                {
                    var hitUnit = targetCollider.GetComponentInParent<StructureUnit>();
                    if (hitUnit == null || hitUnit.Data == null || hitUnit.Data.structureType != StructureType.Floor)
                    {
                        targetCollider = null;
                    }
                }

                if (targetCollider != null)
                {
                    bool isTouching = false;
                    Collider[] myCols = structureObj.GetComponentsInChildren<Collider>();
                    Collider[] targetCols = targetCollider.GetComponentsInChildren<Collider>();
                    
                    UnityEngine.Physics.SyncTransforms();

                    foreach (var mc in myCols)
                    {
                        Bounds expanded = mc.bounds;
                        expanded.Expand(0.2f); // tolerance for adjacency
                        foreach (var tc in targetCols)
                        {
                            if (expanded.Intersects(tc.bounds))
                            {
                                isTouching = true;
                                break;
                            }
                        }
                        if (isTouching) break;
                    }

                    if (isTouching)
                    {
                        actualTarget = targetCollider;
                    }
                }
            }

            if (actualTarget == null)
            {
                // หาจุดยึดใหม่ไม่เจอ แต่เดิมเคยยึดพื้นไว้ → คงสมอยึดพื้น (ground anchor) ไว้ ไม่ตัดทิ้ง
                if (hadGroundAnchor && preserveGroundAnchor)
                {
                    FixedJoint keepGround = structureObj.AddComponent<FixedJoint>();
                    keepGround.connectedBody = null; // null = ยึดกับโลก (พื้น)
                }
                return;
            }

            // 3. Identify the target Rigidbody
            bool isGround = ((1 << actualTarget.gameObject.layer) & groundLayer) != 0;
            Rigidbody targetRb = null;

            // Try to get Rigidbody from StructureUnit first
            var targetUnit = actualTarget.GetComponentInParent<StructureUnit>();
            if (targetUnit != null)
            {
                targetRb = targetUnit.GetComponent<Rigidbody>();
            }

            // Fallback to searching up the hierarchy
            if (actualTarget != null && targetRb == null)
            {
                targetRb = actualTarget.GetComponentInParent<Rigidbody>();
            }

            // ถ้าสิ่งที่อยู่ข้างล่างไม่ใช่โครงสร้าง (ไม่มี StructureUnit) และไม่มี Rigidbody
            // = พื้น/สภาพแวดล้อมที่อยู่กับที่ → ถือเป็น "ground" แล้วยึดกับโลก
            // (กันเคส: พื้นไม่ได้อยู่ใน groundLayer ที่ตั้งไว้ ทำให้ wall บนพื้นไม่ยอม Joint)
            if (!isGround && targetUnit == null && targetRb == null)
            {
                isGround = true;
            }

            // ไม่ให้โครงสร้างอื่นมายึดกับ Gadget (Gadget เป็นจุดยึดให้ใครไม่ได้ — มีได้แค่ Joint หลักของตัวเอง)
            if (targetUnit != null && targetUnit.Data != null && targetUnit.Data.isGadget)
            {
                targetRb = null;
                if (!isGround) actualTarget = null;
            }

            // 4. Safety Check: Cannot connect to itself
            if (targetRb == newRb)
            {
                targetRb = null;
                // If the only thing we found was ourselves, we should check if we have other support
                // For now, if it's ourselves, we treat it as if no support was found via raycast
                if (!isGround) actualTarget = null; 
            }

            if (actualTarget == null)
            {
                // หาจุดยึดใหม่ไม่เจอ แต่เดิมเคยยึดพื้นไว้ → คงสมอยึดพื้น (ground anchor) ไว้ ไม่ตัดทิ้ง
                if (hadGroundAnchor && preserveGroundAnchor)
                {
                    FixedJoint keepGround = structureObj.AddComponent<FixedJoint>();
                    keepGround.connectedBody = null; // null = ยึดกับโลก (พื้น)
                }
                return;
            }

            // 5. Only create a joint if it's ground or another structure with a Rigidbody.
            if (isGround || targetRb != null)
            {
                FixedJoint fixedJoint = structureObj.AddComponent<FixedJoint>();
                fixedJoint.connectedBody = targetRb; // null = fixed to world (correct for ground)

                // Ignore physics collision between the structure and ALL colliders of the target
                Collider[] myColliders = structureObj.GetComponentsInChildren<Collider>();
                Collider[] targetColliders = actualTarget.transform.root.GetComponentsInChildren<Collider>();
                
/*
                foreach (var col in myColliders)
                {
                    foreach (var tCol in targetColliders)
                    {
                        if (col != null && tCol != null)
                            UnityEngine.Physics.IgnoreCollision(col, tCol, true);
                    }
                }
*/            }
        }

        /// <summary>
        /// สร้าง FixedJoint เชื่อมกับโครงสร้างข้างเคียง (ซ้าย/ขวา/หน้า/หลัง/บน/ล่าง)
        /// เรียกหลัง AttachJoint เพื่อให้โครงสร้างมี Joint หลายทาง
        /// ถ้า Joint หลักพัง ยังมี Joint ข้างๆ ยึดอยู่
        /// </summary>
        private void AttachSideJoints(GameObject structureObj)
        {
            StructureUnit newUnit = structureObj.GetComponent<StructureUnit>();
            Rigidbody newRb = structureObj.GetComponent<Rigidbody>();
            if (newUnit == null || newRb == null) return;

            // Gadget เชื่อมเฉพาะ Joint หลัก (TMD เกาะบน, ตัวอื่นเกาะล่าง)
            if (newUnit.Data != null && newUnit.Data.isGadget) return;

            Collider[] myColliders = structureObj.GetComponentsInChildren<Collider>();
            if (myColliders.Length == 0) return;

            // อัปเดต transform ก่อนคำนวณ bounds (กัน bounds เป็น 0 ในเฟรมแรกหลัง SetActive)
            UnityEngine.Physics.SyncTransforms();

            // Joint หลักต่อกับอะไรไว้แล้ว — ไม่ต้องต่อซ้ำ
            Joint mainJoint = structureObj.GetComponent<Joint>();
            Rigidbody mainConnected = mainJoint != null ? mainJoint.connectedBody : null;

            // AABB รวม (ใช้คำนวณแกนสัมผัส + กฎพื้น เท่านั้น)
            Bounds myBounds = GetWorldBounds(myColliders);

            bool newIsFloor = newUnit.Data != null && newUnit.Data.structureType == StructureType.Floor;

            // ── หาเพื่อนบ้าน "ด้วย Collider จริง" ──
            // ยิง OverlapBox ตามรูปทรง/การหมุนของแต่ละ Collider (ขยายเล็กน้อย) เพื่อจับชิ้นที่ "สัมผัสจริง"
            // แม่นกว่าการใช้ AABB ที่พองเกินจริงเมื่อชิ้นถูกหมุน
            // เก็บ "คู่ collider ที่สัมผัสกันจริง" ไว้ด้วย เพื่อใช้ตัดสินแกนสัมผัสแบบ per-collider
            // เมื่อ AABB รวมทั้งชิ้นตัดสินไม่ได้ (ชิ้น compound เช่น บันได ทำให้กล่องอ้วนเกินจริง)
            Dictionary<StructureUnit, KeyValuePair<Collider, Collider>> neighbors =
                new Dictionary<StructureUnit, KeyValuePair<Collider, Collider>>();
            foreach (var myCol in myColliders)
            {
                if (myCol == null || myCol.isTrigger) continue;
                GetColliderOrientedBox(myCol, out Vector3 oc, out Vector3 ohe, out Quaternion orot);
                ohe += Vector3.one * jointHitboxExpand;

                Collider[] overlaps = UnityEngine.Physics.OverlapBox(oc, ohe, orot, structureLayer, QueryTriggerInteraction.Ignore);
                foreach (var hitCol in overlaps)
                {
                    if (hitCol == null) continue;
                    var ou = hitCol.GetComponentInParent<StructureUnit>();
                    if (ou != null && ou != newUnit && !neighbors.ContainsKey(ou))
                        neighbors.Add(ou, new KeyValuePair<Collider, Collider>(myCol, hitCol));
                }
            }

            foreach (var kv in neighbors)
            {
                StructureUnit unit = kv.Key;
                if (unit == null || unit.Data == null) continue;

                // ไม่ต่อ Joint กับ Gadget (Gadget มี Joint หลักตัวเดียว ไม่ให้ใครมาเกาะ)
                if (unit.Data.isGadget) continue;

                // ไม่ต่อกับชิ้นที่พัง/หลุดไปแล้ว (สำคัญตอน TryReattachJoint ระหว่างจำลอง)
                var nStress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                if (nStress != null && (nStress.IsBroken || nStress.IsDetached)) continue;

                Rigidbody otherRb = unit.GetComponent<Rigidbody>();
                if (otherRb == null || otherRb == mainConnected) continue;
                if (HasJointTo(structureObj, otherRb)) continue;   // กันต่อซ้ำฝั่งเรา
                if (HasJointTo(unit.gameObject, newRb)) continue;  // กันต่อซ้ำสองทิศ (อีกฝั่งต่อมาหาเราแล้ว)

                Collider[] otherColliders = unit.GetComponentsInChildren<Collider>();
                if (otherColliders.Length == 0) continue;
                Bounds otherBounds = GetWorldBounds(otherColliders);

                // หาทิศสัมผัสแบบแกนตรง: 0=X(ซ้าย/ขวา), 1=Y(บน/ล่าง), 2=Z(หน้า/หลัง)
                // คืน -1 = สัมผัสแบบขอบ/มุม (แนวทแยง) → ข้าม (Joint ได้แค่ 6 ด้านตรงๆ)
                int contactAxis = GetFaceContactAxis(myBounds, otherBounds);

                // AABB รวมตัดสินไม่ได้ → ลองใหม่จาก "คู่ collider ที่สัมผัสจริง"
                // (แก้เคสบันได/ชิ้น compound ติดกับเพื่อนบ้านแต่ไม่ยอมต่อ joint)
                if (contactAxis < 0 && kv.Value.Key != null && kv.Value.Value != null)
                    contactAxis = GetFaceContactAxis(kv.Value.Key.bounds, kv.Value.Value.bounds);

                if (contactAxis < 0) continue;

                // กฎพื้น:
                //  - floor ↔ โครงสร้างอื่น (ไม่ใช่ floor): ต่อได้แค่ บน/ล่าง (แกน Y)
                //  - floor ↔ floor: ต่อได้ทุกด้าน (วางพื้นต่อกันเป็นแพลตฟอร์มได้)
                bool otherIsFloor = unit.Data.structureType == StructureType.Floor;
                bool oneIsFloor = newIsFloor ^ otherIsFloor;
                if (oneIsFloor && contactAxis != 1) continue;

                FixedJoint sideJoint = structureObj.AddComponent<FixedJoint>();
                sideJoint.connectedBody = otherRb;
            }
        }

        /// <summary>
        /// คืนกล่อง (oriented) ของ Collider สำหรับใช้กับ Physics.OverlapBox
        /// BoxCollider → ใช้ขนาด/การหมุนจริง ; ชนิดอื่น → ใช้ AABB (rotation = identity)
        /// </summary>
        private void GetColliderOrientedBox(Collider col, out Vector3 center, out Vector3 halfExtents, out Quaternion rotation)
        {
            if (col is BoxCollider box)
            {
                Transform t = box.transform;
                center = t.TransformPoint(box.center);
                Vector3 s = Vector3.Scale(box.size, t.lossyScale);
                halfExtents = new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)) * 0.5f;
                rotation = t.rotation;
            }
            else
            {
                Bounds b = col.bounds;
                center = b.center;
                halfExtents = b.extents;
                rotation = Quaternion.identity;
            }
        }

        /// <summary>
        /// รวม world AABB ของ Collider หลายตัวให้เป็นกล่องเดียว
        /// </summary>
        private Bounds GetWorldBounds(Collider[] cols)
        {
            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
            return b;
        }

        /// <summary>
        /// หาแกนที่สองกล่องสัมผัสกันแบบ "หน้าต่อหน้า" (axis-aligned)
        /// คืน 0=X, 1=Y, 2=Z ; คืน -1 ถ้าสัมผัสแบบขอบ/มุม (แนวทแยง) ซึ่งต้องข้าม
        /// </summary>
        private int GetFaceContactAxis(Bounds a, Bounds b)
        {
            Vector3 diff = b.center - a.center;
            Vector3 sumHalf = (a.size + b.size) * 0.5f;
            // eps เล็กลงจาก 0.05 → 0.02: ชิ้นบางๆ (เช่น กำแพงหนา ~0.1) ที่มีช่องว่างเล็กน้อย
            // จาก snap จะยังนับว่า "ซ้อนทับ" ในแกนนั้น — เดิมถูกตัดเป็นแนวทแยงทั้งที่ติดกันจริง
            float eps = 0.02f;

            // นับแกนที่ "ซ้อนทับ" กัน — หน้าต่อหน้าต้องซ้อนทับอย่างน้อย 2 แกน
            int overlapCount = 0;
            if (Mathf.Abs(diff.x) < sumHalf.x - eps) overlapCount++;
            if (Mathf.Abs(diff.y) < sumHalf.y - eps) overlapCount++;
            if (Mathf.Abs(diff.z) < sumHalf.z - eps) overlapCount++;
            if (overlapCount < 2) return -1; // ขอบ/มุม → แนวทแยง

            // ทิศสัมผัส = แกนที่อัตราส่วน (ระยะห่าง / ครึ่งผลรวมขนาด) มากสุด
            float rx = sumHalf.x > 0.0001f ? Mathf.Abs(diff.x) / sumHalf.x : 0f;
            float ry = sumHalf.y > 0.0001f ? Mathf.Abs(diff.y) / sumHalf.y : 0f;
            float rz = sumHalf.z > 0.0001f ? Mathf.Abs(diff.z) / sumHalf.z : 0f;

            if (ry >= rx && ry >= rz) return 1;
            if (rx >= ry && rx >= rz) return 0;
            return 2;
        }

        /// <summary>
        /// มี Joint ต่อกับ Rigidbody เป้าหมายอยู่แล้วหรือยัง (กันสร้าง Joint ซ้ำ)
        /// </summary>
        private bool HasJointTo(GameObject obj, Rigidbody target)
        {
            foreach (var j in obj.GetComponents<Joint>())
                if (j != null && j.connectedBody == target) return true;
            return false;
        }

        /// <summary>
        /// ตรวจว่า Collider นี้ใช้เป็น "จุดยึด Joint" ได้หรือไม่ และยึดแบบไหน
        /// คืน false ถ้า: เป็น trigger / เป็นของตัวเอง / โครงสร้างพัง-หลุดแล้ว / เป็น Gadget /
        /// เป็นวัตถุมี Rigidbody ที่ไม่ใช่โครงสร้าง (เศษซาก/prop ไดนามิก — ห้ามเชื่อมเด็ดขาด)
        /// targetRb   = Rigidbody ของโครงสร้างเป้าหมาย (null ถ้า groundLike)
        /// groundLike = วัตถุนิ่งในฉากที่ไม่ใช่โครงสร้าง (พื้น/สภาพแวดล้อม) → ยึดกับโลกได้
        /// </summary>
        private bool TryGetAttachCandidate(Collider col, GameObject self, out Rigidbody targetRb, out bool groundLike)
        {
            targetRb = null;
            groundLike = false;

            if (col == null || col.isTrigger) return false;
            if (col.gameObject == self || col.transform.IsChildOf(self.transform)) return false;

            var unit = col.GetComponentInParent<StructureUnit>();
            if (unit != null)
            {
                if (unit.gameObject == self) return false;
                if (unit.Data != null && unit.Data.isGadget) return false; // กฎเดิม: ห้ามยึดกับ Gadget

                // ห้ามยึดกับชิ้นที่พัง/หลุดแล้ว (เศษซาก) — เดิมไม่กรอง ทำให้เกาะซากแทนพื้น/ตัวค้ำจริง
                var stress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                if (stress != null && (stress.IsBroken || stress.IsDetached)) return false;

                targetRb = unit.GetComponent<Rigidbody>();
                return targetRb != null;
            }

            // ไม่ใช่โครงสร้าง: ถ้ามี Rigidbody (prop/เศษไดนามิกอื่น) ห้ามเชื่อม
            // ถ้านิ่งสนิท (ไม่มี Rigidbody) → ถือเป็นพื้น/สภาพแวดล้อม ยึดกับโลกได้
            if (col.GetComponentInParent<Rigidbody>() != null) return false;
            groundLike = true;
            return true;
        }

        /// <summary>
        /// เมื่อลบโครงสร้าง ให้ตรวจหาโครงสร้างอื่นที่มี Joint อ้างอิงถึงตัวนี้
        /// แล้วลบ Joint นั้นออก เพื่อป้องกัน null reference → Break ผิดปกติ
        /// ถ้ายังมี Joint อื่นเหลืออยู่ โครงสร้างจะไม่พัง
        /// </summary>
        private void CleanupJointsReferencingUnit(StructureUnit deletedUnit)
        {
            if (deletedUnit == null) return;
            Rigidbody deletedRb = deletedUnit.GetComponent<Rigidbody>();
            if (deletedRb == null) return;

            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit == deletedUnit) continue;

                Joint[] joints = unit.GetComponents<Joint>();
                foreach (var joint in joints)
                {
                    if (joint != null && joint.connectedBody == deletedRb)
                    {
                        Destroy(joint);
                    }
                }
            }
        }

        private void CancelCurrentMove()
        {
            if (_movingUnit != null)
            {
                _movingUnit.gameObject.SetActive(true);
                _movingUnit.SetHighlight(false);
            }
            foreach (var u in _movingGroup)
            {
                if (u != null)
                {
                    u.gameObject.SetActive(true);
                    u.SetHighlight(false);
                }
            }
            _movingUnit = null;
            _movingGroup.Clear();
            _isDraggingMove = false;
            ghostBuilder.DestroyGhost();
            ClearHover();
        }

        private void TrySellStructure(StructureUnit unit)
        {
            float materialPrice = unit.CurrentMaterial != null ? unit.CurrentMaterial.priceModifier : 0f;
            float sellPrice = unit.Data.basePrice + materialPrice + unit.AdditionalRefundValue;

            Collider targetCol = null;
            var joint = unit.GetComponent<Joint>();
            if (joint != null && joint.connectedBody != null)
            {
                targetCol = joint.connectedBody.GetComponentInChildren<Collider>();
            }

            ExecuteCommand(
                execute: () => {
                    _currentBudget += sellPrice;
                    CleanupJointsReferencingUnit(unit);
                    _placedStructures.Remove(unit);
                    
                    var joints = unit.GetComponents<Joint>();
                    foreach (var j in joints) Destroy(j);
                    
                    unit.gameObject.SetActive(false);
                    
                    if (generalSellSound != null) AudioSource.PlayClipAtPoint(generalSellSound, unit.transform.position);
                    if (generalSellVFX != null) Instantiate(generalSellVFX, unit.transform.position, Quaternion.identity);
                },
                undo: () => {
                    _currentBudget -= sellPrice;
                    unit.gameObject.SetActive(true);
                    unit.SetHighlight(false); // Reset visual after undoing delete
                    AttachJoint(unit.gameObject, targetCol);
                    _placedStructures.Add(unit);
                    IgnoreOverlappingCollisions(unit);
                }
            );

            RecalculateMaxFloor();
        }

        private void ApplyMaterialToStructure(StructureUnit unit, MaterialData material)
        {
            if (unit.CurrentMaterial == material) return;

            MaterialData oldMaterial = unit.CurrentMaterial;
            MaterialData newMaterial = material;
            
            float oldPrice = oldMaterial != null ? oldMaterial.priceModifier : 0f;
            float newPrice = newMaterial.priceModifier;
            float diff = newPrice - oldPrice;

            // Allow negative budget as per mission requirements

            ExecuteCommand(
                execute: () => {
                    _currentBudget -= diff;
                    unit.ChangeMaterial(newMaterial);
                    
                    var stress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                    if (stress != null)
                    {
                        float comp = unit.Data.baseMaxCompression * newMaterial.compressionMultiplier;
                        float tens = unit.Data.baseMaxTension     * newMaterial.tensionMultiplier;
                        stress.InitializeStress(unit.CurrentHP, comp, tens);
                    }

                    if (newMaterial.placeSound != null) 
                        AudioSource.PlayClipAtPoint(newMaterial.placeSound, unit.transform.position);
                    else if (generalPaintSound != null)
                        AudioSource.PlayClipAtPoint(generalPaintSound, unit.transform.position);
                        
                    if (newMaterial.placeVFX != null) Instantiate(newMaterial.placeVFX, unit.transform.position, Quaternion.identity);
                },
                undo: () => {
                    _currentBudget += diff;
                    unit.ChangeMaterial(oldMaterial);
                    
                    var stress = unit.GetComponent<Simulation.Physics.StructuralStress>();
                    if (stress != null)
                    {
                        float comp = unit.Data.baseMaxCompression * (oldMaterial != null ? oldMaterial.compressionMultiplier : 1f);
                        float tens = unit.Data.baseMaxTension     * (oldMaterial != null ? oldMaterial.tensionMultiplier : 1f);
                        stress.InitializeStress(unit.CurrentHP, comp, tens);
                    }
                }
            );
        }

        // --------------------------------------------------------------------------------
        // HELPER FUNCTIONS
        // --------------------------------------------------------------------------------

        /// <summary>
        /// Determine the active StructureData for placement calculations.
        /// In Placing mode use _selectedData, in Moving mode use _movingUnit.Data.
        /// </summary>
        private List<StructureUnit> FindConnectedUnits(StructureUnit start)
        {
            if (start == null) return new List<StructureUnit>();

            HashSet<StructureUnit> connected = new HashSet<StructureUnit>();
            Stack<StructureUnit> toVisit = new Stack<StructureUnit>();
            
            toVisit.Push(start);
            connected.Add(start);

            while (toVisit.Count > 0)
            {
                StructureUnit current = toVisit.Pop();
                Rigidbody currentRb = current.GetComponent<Rigidbody>();
                
                // 1. Find all units this structure is connected to via its own Joints
                Joint[] joints = current.GetComponents<Joint>();
                foreach (var j in joints)
                {
                    if (j.connectedBody != null)
                    {
                        StructureUnit other = j.connectedBody.GetComponent<StructureUnit>();
                        if (other != null && !connected.Contains(other))
                        {
                            connected.Add(other);
                            toVisit.Push(other);
                        }
                    }
                }

                // 2. Find all units that have Joints pointing to THIS structure
                if (currentRb != null)
                {
                    foreach (var unit in _placedStructures)
                    {
                        if (unit == null || connected.Contains(unit)) continue;
                        
                        Joint[] otherJoints = unit.GetComponents<Joint>();
                        foreach (var oj in otherJoints)
                        {
                            if (oj != null && oj.connectedBody == currentRb)
                            {
                                connected.Add(unit);
                                toVisit.Push(unit);
                                break;
                            }
                        }
                    }
                }
            }

            return new List<StructureUnit>(connected);
        }

        private StructureData GetActiveStructureData()
        {
            if (_selectedData != null) return _selectedData;
            if (_movingUnit != null) return _movingUnit.Data;
            if (_movingGroup.Count > 0) return _movingGroup[0].Data;
            return null;
        }

        private Vector3 CalculatePlacementPosition(Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider, bool forceNormalType = false)
        {
            StructureData activeData = GetActiveStructureData();
            StructureType placementType = (activeData != null && !forceNormalType) ? activeData.structureType : StructureType.Normal;

            // ── Door: ถ้าเล็งโดน "กำแพง" โดยตรง ให้ดูดเข้าทับกำแพงนั้นและหันตามทันที ──
            // (ต้องทำก่อน logic isSideHit ที่ปกติจะดันตำแหน่งออกไปช่องถัดไป ซึ่งทำให้ประตูเด้งหนีกำแพง)
            if (placementType == StructureType.Door && hitCollider != null)
            {
                StructureUnit hitWall = hitCollider.GetComponentInParent<StructureUnit>();
                if (hitWall != null && hitWall.Data != null && hitWall.Data.structureType == StructureType.Wall)
                {
                    if (ghostBuilder != null && !_isDragging)
                    {
                        float wr = Mathf.Round(hitWall.Rotation / 90f) * 90f;
                        ghostBuilder.SetRotation(((wr % 360f) + 360f) % 360f);
                    }
                    return hitWall.transform.position;
                }
            }

            float rawX = hitPoint.x;
            float rawZ = hitPoint.z;

            // เมื่อคลิกด้านข้างของ Structure ให้เลื่อนตำแหน่งไปตาม normal
            // เพื่อบังคับให้ snap ไปช่องถัดไปแทนช่องเดิม
            bool isSideHit = Mathf.Abs(hitNormal.y) < 0.5f;
            if (activeData != null && IsTunedMassDamper(activeData))
            {
                isSideHit = false;
            }
            else if (isSideHit && hitCollider != null
                && hitCollider.GetComponentInParent<StructureUnit>() != null)
            {
                float absX = Mathf.Abs(hitNormal.x);
                float absZ = Mathf.Abs(hitNormal.z);

                if (absX > absZ && absX > 0.3f)
                {
                    rawX += Mathf.Sign(hitNormal.x) * gridSize * 0.51f;
                }
                else if (absZ > 0.3f)
                {
                    rawZ += Mathf.Sign(hitNormal.z) * gridSize * 0.51f;
                }
            }

            // ── X / Z Snapping based on StructureType ──
            float x, z;
            if (placementType == StructureType.Wall || placementType == StructureType.Door)
            {
                // Wall / Door: snap to grid EDGES (lines between cells) based on mouse position relative to cell center
                float offsetX = (gridColumns % 2 != 0) ? gridSize * 0.5f : 0f;
                float offsetZ = (gridRows % 2 != 0) ? gridSize * 0.5f : 0f;
                
                float leftEdge = Mathf.Floor((rawX - offsetX) / gridSize) * gridSize + offsetX;
                float rightEdge = leftEdge + gridSize;
                float bottomEdge = Mathf.Floor((rawZ - offsetZ) / gridSize) * gridSize + offsetZ;
                float topEdge = bottomEdge + gridSize;

                float centerX = leftEdge + gridSize * 0.5f;
                float centerZ = bottomEdge + gridSize * 0.5f;

                float dx = rawX - centerX;
                float dz = rawZ - centerZ;

                float targetRot = 0f;

                // Determine closest edge to the mouse cursor inside the cell
                if (Mathf.Abs(dx) > Mathf.Abs(dz))
                {
                    if (dx < 0f)
                    {
                        // Left Edge
                        x = leftEdge; 
                        z = centerZ;
                        targetRot = 0f;
                    }
                    else
                    {
                        // Right Edge
                        x = rightEdge;
                        z = centerZ;
                        targetRot = 180f;
                    }
                }
                else
                {
                    if (dz < 0f)
                    {
                        // Bottom Edge
                        x = centerX;
                        z = bottomEdge;
                        targetRot = 270f;
                    }
                    else
                    {
                        // Top Edge
                        x = centerX;
                        z = topEdge; 
                        targetRot = 90f;
                    }
                }

                // Automatically update the ghost rotation to align with the snapped edge
                if (ghostBuilder != null && !_isDragging)
                {
                    ghostBuilder.SetRotation(targetRot);
                }

                // ── Door: ดูดเข้าหา Wall ที่ใกล้ที่สุด แล้ว "หัน" ให้ตรงด้านกับกำแพง ──
                // เลือก Wall โดยวัดระยะแนวราบ (XZ) เป็นหลัก และจำกัดความต่างของระดับ (Y)
                // เพื่อไม่ให้ประตูดูดข้ามชั้น จากนั้นบังคับ rotation ของประตูให้เท่ากับกำแพง
                // (สำคัญ: FindWallAtPosition ใช้ rotation ในการจับคู่ ถ้าไม่ตรงจะวางไม่ได้/หันผิดด้าน)
                if (placementType == StructureType.Door)
                {
                    StructureUnit nearbyWall = null;
                    float minWallDist = gridSize * 0.6f;  // ระยะดึงดูด สัมพันธ์กับขนาด grid
                    float maxYDiff    = gridSize * 0.5f;  // กันไม่ให้ดูดข้ามชั้น

                    Vector2 probeXZ = new Vector2(x, z);
                    foreach (var unit in _placedStructures)
                    {
                        if (unit == null || unit.Data == null || unit.Data.structureType != StructureType.Wall) continue;

                        Vector3 wp = unit.transform.position;
                        if (Mathf.Abs(wp.y - hitPoint.y) > maxYDiff) continue; // คนละชั้น ข้ามไป

                        float dHoriz = Vector2.Distance(probeXZ, new Vector2(wp.x, wp.z));
                        if (dHoriz < minWallDist)
                        {
                            minWallDist = dHoriz;
                            nearbyWall = unit;
                        }
                    }

                    if (nearbyWall != null)
                    {
                        // 1) หันประตูให้ตรงด้านเดียวกับกำแพง (snap เป็นช่วง 90°)
                        //    ทำให้บานประตูหันถูกด้าน และ FindWallAtPosition จับคู่กำแพงได้ (tolerance 10°/180°)
                        if (ghostBuilder != null && !_isDragging)
                        {
                            float wallRot = Mathf.Round(nearbyWall.Rotation / 90f) * 90f;
                            ghostBuilder.SetRotation(((wallRot % 360f) + 360f) % 360f);
                        }

                        // 2) ดูดตำแหน่งให้ทับกำแพงพอดี (รวมแกน Y ตามกำแพง)
                        return nearbyWall.transform.position;
                    }
                }
            }
            else
            {
                // Normal structures: snap to cell CENTER, accounting for multi-cell size
                float sizeX = activeData != null ? activeData.size.x : 1f;
                float sizeZ = activeData != null ? activeData.size.z : 1f;
                float rot = ghostBuilder != null ? ghostBuilder.CurrentRotation : 0f;

                // Swap size axes when rotated 90/270
                if (Mathf.Abs(rot % 180f) > 45f)
                {
                    float tmp = sizeX;
                    sizeX = sizeZ;
                    sizeZ = tmp;
                }

                float offsetX = (gridColumns % 2 != 0) ? gridSize * 0.5f : 0f;
                float offsetZ = (gridRows % 2 != 0) ? gridSize * 0.5f : 0f;

                float centerX = SnapToCellCenter(rawX, sizeX, offsetX);
                float centerZ = SnapToCellCenter(rawZ, sizeZ, offsetZ);

                x = centerX;
                z = centerZ;
            }

            // ── Y Calculation (unchanged logic) ──
            float y = hitPoint.y;

            // Use the passed hitCollider parameter (not the global _currentHitCollider)
            // so that internal calls with explicit collider work correctly
            if (hitCollider != null)
            {
                StructureUnit hitUnit = hitCollider.GetComponentInParent<StructureUnit>();
                if (hitUnit != null && hitUnit.Data != null && hitUnit.Data.prefab != null)
                {
                    float hitUnitPivotToBottom = GetPivotToBottomOffset(hitUnit.Data);
                    float hitUnitPivotToTop    = GetPivotToTopOffset(hitUnit.Data.prefab);

                    float bottomY = hitUnit.transform.position.y - hitUnitPivotToBottom;
                    float topY    = hitUnit.transform.position.y + hitUnitPivotToTop;

                    bool isBottomHit = hitNormal.y <= -0.5f;

                    if (activeData != null && IsTunedMassDamper(activeData) && hitUnit.Data.structureType == StructureType.Floor)
                    {
                        isBottomHit = true;
                        isSideHit = false;
                    }

                    if (isSideHit)
                    {
                        y = bottomY;
                    }
                    else if (isBottomHit)
                    {
                        float newObjHeight = heightStep > 0 ? heightStep : gridSize;
                        if (activeData != null && activeData.prefab != null)
                        {
                            newObjHeight = GetPivotToTopOffset(activeData.prefab) + GetPivotToBottomOffset(activeData);
                        }
                        y = bottomY - newObjHeight;
                    }
                    else if (hitUnit.Data.placementSinkThrough && (activeData == null || !activeData.requiresSupport))
                    {
                        y = bottomY;
                    }
                    else
                    {
                        y = topY;
                    }
                }
            }

            // Gadget: ใช้ Y จาก Surface จริง (ไม่ snap ตาม grid) เพื่อให้วางบนโครงสร้างได้พอดี
            bool isGadget = activeData != null && activeData.isGadget;

            if (snapYToGrid && !isGadget)
            {
                float yStep = heightStep > 0f ? heightStep : gridSize;
                if (yStep > 0f)
                {
                    y = Mathf.Round(y / yStep) * yStep;
                }
            }

            // ถ้าคำนวณตำแหน่งเสร็จแล้วพบว่ามันพยายามจะมุดดิน (Y ติดลบ)
            // ให้ปัด (Clamp) ตำแหน่งฐานกลับขึ้นมาอยู่ที่ระดับพื้น (Y = 0) เสมอ
            if (y < 0f)
            {
                y = 0f;
            }

            y += _pivotToBottomOffset;

            Vector3 resultPos = new Vector3(x, y, z);

            // Stack/push up vertically if this exact cell position is already occupied by the same structure data
            if (activeData != null && !activeData.isGadget)
            {
                float yStep = heightStep > 0f ? heightStep : gridSize;
                int maxIterations = 50; // Safety cap to prevent infinite loop
                for (int i = 0; i < maxIterations; i++)
                {
                    if (IsAlreadyOccupiedBySame(resultPos, ghostBuilder != null ? ghostBuilder.CurrentRotation : 0f, activeData))
                    {
                        resultPos.y += yStep;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return resultPos;
        }

        // ── Snap helpers ──────────────────────────────────────────────

        /// <summary>
        /// Snap a coordinate to the center of a cell group.
        /// For size=1: center of a single cell (offset by half grid).
        /// For size=2: center spans two cells, etc.
        /// </summary>
        private float SnapToCellCenter(float raw, float cellCount, float gridOffset = 0f)
        {
            if (!useGridSnap) return raw;

            // The total span of this structure in world units
            float span = cellCount * gridSize;

            // Snap the LEFT edge to the nearest grid line, then offset to center
            float leftEdge = (raw - gridOffset) - span * 0.5f;
            float snappedLeft = Mathf.Round(leftEdge / gridSize) * gridSize;
            return snappedLeft + span * 0.5f + gridOffset;
        }

        /// <summary>
        /// Wall snapping: choose the nearest grid edge (line between cells).
        /// The wall is perpendicular to the edge it sits on.
        /// One axis goes to the grid line, the other goes to the cell center.
        /// snappedToXLine: true if the wall sits on a vertical (X-axis) grid line.
        /// </summary>
        private float SnapWallAxis(float rawX, float rawZ, out float snappedZ, out bool snappedToXLine)
        {
            if (!useGridSnap)
            {
                snappedZ = rawZ;
                snappedToXLine = true;
                return rawX;
            }

            // Snap both axes to nearest grid line first
            float lineX = Mathf.Round(rawX / gridSize) * gridSize;
            float lineZ = Mathf.Round(rawZ / gridSize) * gridSize;

            // Distance from each axis to its nearest grid line
            float distToLineX = Mathf.Abs(rawX - lineX);
            float distToLineZ = Mathf.Abs(rawZ - lineZ);

            // Cell center = grid line offset by half
            float centerX = Mathf.Floor(rawX / gridSize) * gridSize + gridSize * 0.5f;
            float centerZ = Mathf.Floor(rawZ / gridSize) * gridSize + gridSize * 0.5f;

            if (distToLineX < distToLineZ)
            {
                // Closer to a vertical grid line → wall sits on X line, extends along Z
                snappedZ = centerZ;
                snappedToXLine = true;
                return lineX;
            }
            else
            {
                // Closer to a horizontal grid line → wall sits on Z line, extends along X
                snappedZ = lineZ;
                snappedToXLine = false;
                return centerX;
            }
        }

        /// <summary>
        /// Check if a specific structure is already placed at this position and rotation.
        /// </summary>
        private bool IsAlreadyOccupiedBySame(Vector3 position, float rotation, StructureData data)
        {
            float tolerance = 0.1f;
            float rotTolerance = 1f;

            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit.Data != data) continue;

                float dist = Vector3.Distance(position, unit.transform.position);
                float rotDiff = Quaternion.Angle(Quaternion.Euler(0, rotation, 0), Quaternion.Euler(0, unit.Rotation, 0));

                if (dist < tolerance && rotDiff < rotTolerance) return true;
            }
            return false;
        }

        /// <summary>
        /// Used by Door placement to find which wall to replace.
        /// </summary>
        private StructureUnit FindWallAtPosition(Vector3 position, float rotation)
        {
            float tolerance = gridSize * 0.3f;
            float rotTolerance = 10f;

            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit == _movingUnit) continue;
                if (unit.Data == null || unit.Data.structureType != StructureType.Wall) continue;

                float dist = Vector3.Distance(position, unit.transform.position);
                float rotDiff = Quaternion.Angle(
                    Quaternion.Euler(0, rotation, 0),
                    Quaternion.Euler(0, unit.Rotation, 0)
                );

                // Also accept 180° difference (same wall facing opposite direction)
                bool sameOrientation = rotDiff < rotTolerance || Mathf.Abs(rotDiff - 180f) < rotTolerance;

                if (dist < tolerance && sameOrientation)
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// Distance from the prefab pivot to its TOP face (positive value).
        /// </summary>
        private float GetPivotToTopOffset(GameObject prefab)
        {
            (Vector3 center, Vector3 size) = GetPrefabBounds(prefab);
            // top = center.y + size.y * 0.5f  →  offset from pivot = center.y + size.y * 0.5f
            return center.y + size.y * 0.5f;
        }

        private void SetLayerRecursively(GameObject obj, LayerMask layerMask)
        {
            int layerIndex = 0;
            for (int i = 0; i < 32; i++)
            {
                if ((layerMask.value & (1 << i)) != 0)
                {
                    layerIndex = i;
                    break;
                }
            }

            SetLayer(obj, layerIndex);
        }

        private void SetLayer(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayer(child.gameObject, layer);
            }
        }

        private float GetPivotToBottomOffset(StructureData data, GameObject instance = null)
        {
            if (data == null) return 0f;
            
            // If we have an instance, its scale might have been modified.
            // We should prioritize the physical bounds of the instance if it has a collider.
            if (instance != null)
            {
                float offset = GetPivotToBottomOffset(instance);
                if (offset != 0f) return offset;
            }

            if (data.prefab == null) return 0f;

            if (data.pivotAtCenter)
            {
                // If explicitly centered, the offset is half the height
                float yStep = heightStep > 0f ? heightStep : gridSize;
                return (data.size.y * yStep) * 0.5f;
            }

            return GetPivotToBottomOffset(data.prefab);
        }

        private float GetPivotToBottomOffset(GameObject prefab)
        {
            (Vector3 center, Vector3 size) = GetPrefabBounds(prefab);
            return (size.y * 0.5f) - center.y;
        }

        private (Vector3 center, Vector3 size) GetPrefabBounds(GameObject prefab)
        {
            if (prefab == null) return (Vector3.zero, Vector3.one);

            // Use BoxCollider if it exists for perfectly consistent size.
            BoxCollider bc = prefab.GetComponentInChildren<BoxCollider>(true);
            if (bc != null)
            {
                Vector3 size = Vector3.Scale(bc.size, bc.transform.lossyScale);
                size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

                Vector3 offsetFromRoot = bc.transform.TransformPoint(bc.center) - prefab.transform.position;
                Vector3 unrotatedOffset = Quaternion.Inverse(prefab.transform.rotation) * offsetFromRoot;

                return (unrotatedOffset, size);
            }

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return (Vector3.zero, Vector3.one);

            Bounds prefabBounds = new Bounds(Vector3.zero, Vector3.zero);
            bool boundsInitialized = false;

            foreach (var r in renderers)
            {
                MeshFilter mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null)
                {
                    Bounds localB = mf.sharedMesh.bounds;
                    
                    Vector3 worldCenter = r.transform.TransformPoint(localB.center);
                    Vector3 offsetFromRoot = worldCenter - prefab.transform.position;
                    Vector3 unrotatedCenter = Quaternion.Inverse(prefab.transform.rotation) * offsetFromRoot;
                    
                    Vector3 worldSize = Vector3.Scale(localB.size, r.transform.lossyScale);
                    worldSize = new Vector3(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z));
                    
                    Bounds transformedBounds = new Bounds(unrotatedCenter, worldSize);

                    if (!boundsInitialized) { prefabBounds = transformedBounds; boundsInitialized = true; }
                    else { prefabBounds.Encapsulate(transformedBounds); }
                }
            }
            return (prefabBounds.center, prefabBounds.size);
        }

        private string GetGridPositionString(Vector3 position)
        {
            int gridX = Mathf.RoundToInt(position.x / (gridSize > 0 ? gridSize : 1f));
            int gridY = Mathf.RoundToInt(position.y / (heightStep > 0 ? heightStep : 1f));
            int gridZ = Mathf.RoundToInt(position.z / (gridSize > 0 ? gridSize : 1f));
            return $"({gridX}, {gridY}, {gridZ})";
        }

        private Bounds GetGridBounds(Vector3 position, float rotation, StructureData data)
        {
            if (data == null) return new Bounds(position, Vector3.zero);

            (Vector3 localCenter, Vector3 localSize) = GetPrefabBounds(data.prefab);
            Vector3 extents = localSize * 0.5f;

            // Swap X and Z for 90 or 270 degree rotations
            if (Mathf.Abs(rotation % 180f) > 45f)
            {
                extents = new Vector3(extents.z, extents.y, extents.x);
            }

            // Calculate world center using the exact calculated rotation
            Quaternion globalRot = Quaternion.Euler(0, rotation, 0) * data.prefab.transform.rotation;
            Vector3 worldCenter = position + (globalRot * localCenter);

            // Shrink by 5% so adjacent surfaces don't trigger overlap
            return new Bounds(worldCenter, extents * 1.9f); 
        }

        private bool IsAreaClear(Vector3 position, float rotation, StructureData structureData)
        {
            if (structureData == null) return true;

            // ── 1. Check Grid Boundaries ──────────────────────────────
            if (!IsWithinBounds(position, rotation, structureData))
            {
                return false;
            }

            // ── 2. Check Overlaps with other structures ───────────────
            Bounds boundsA = GetGridBounds(position, rotation, structureData);

            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit == _movingUnit || _movingGroup.Contains(unit)) continue;

                // NEW: ข้ามการเช็คการทับซ้อนกับตัวละคร (PersonTarget) 
                // เพื่อไม่ให้มาร์กเกอร์คนไปบล็อกการวางสิ่งก่อสร้างหรือ Gadget
                if (unit.GetComponent<Simulation.Character.PersonTarget>() != null) continue;

                // Check for exact duplicate placements regardless of allowOverlap
                float dist = Vector3.Distance(position, unit.transform.position);
                float rotDiff = Quaternion.Angle(Quaternion.Euler(0, rotation, 0), Quaternion.Euler(0, unit.Rotation, 0));
                
                if (dist < 0.1f && rotDiff < 1f)
                {
                    // Door is allowed to overlap with a Wall (it replaces it)
                    bool isDoorOnWall = structureData.structureType == StructureType.Door
                                     && unit.Data != null
                                     && unit.Data.structureType == StructureType.Wall;
                    if (!isDoorOnWall)
                    {
                        // Perfectly overlapping identical positions are never allowed
                        return false;
                    }
                }

                // If both allow overlap, skip intersection check
                if (structureData.allowOverlap || (unit.Data != null && unit.Data.allowOverlap)) 
                {
                    continue;
                }

                Bounds boundsB = GetGridBounds(unit.transform.position, unit.Rotation, unit.Data);

                if (boundsA.Intersects(boundsB))
                {
                    return false;
                }
            }

            return true;
        }

        private void IgnoreOverlappingCollisions(StructureUnit newUnit)
        {
            if (newUnit == null) return;
            
            Collider[] myColliders = newUnit.GetComponentsInChildren<Collider>(true);
            if (myColliders.Length == 0) return;

            // Ignore collisions กับโครงสร้างทั้งหมดที่วางไว้แล้ว
            // รับประกันว่าไม่มีชิ้นส่วนไหนดันกันทางฟิสิกส์ ไม่ว่าจะอยู่จุดไหนก็ตาม
            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit == newUnit) continue;
                
                Collider[] otherColliders = unit.GetComponentsInChildren<Collider>(true);
/*
                foreach (var myCol in myColliders)
                {
                    foreach (var otherCol in otherColliders)
                    {
                        if (myCol != null && otherCol != null)
                            UnityEngine.Physics.IgnoreCollision(myCol, otherCol, true);
                    }
                }
*/            }
        }

        /// <summary>
        /// ตรวจสอบว่าโครงสร้างอยู่ในขอบเขตของ Grid (X, Z) และไม่จมดิน (Y) หรือไม่
        /// </summary>
        private bool IsWithinBounds(Vector3 position, float rotation, StructureData data)
        {
            if (data == null) return true;

            // 1. คำนวณขนาด X, Z ตามการหมุน (ถ้าหมุน 90/270 ให้สลับแกน)
            float sizeX = data.size.x;
            float sizeZ = data.size.z;
            if (Mathf.Abs(rotation % 180f) > 45f)
            {
                sizeX = data.size.z;
                sizeZ = data.size.x;
            }

            // 2. คำนวณขอบเขตในโลก (World Space)
            // พื้นที่ Grid อยู่ที่เซ็นเตอร์ (0,0) กระจายออกไปครึ่งหนึ่งของ totalWidth/totalDepth
            float halfWidth = (sizeX * gridSize) * 0.5f;
            float halfDepth = (sizeZ * gridSize) * 0.5f;

            float minX = position.x - halfWidth;
            float maxX = position.x + halfWidth;
            float minZ = position.z - halfDepth;
            float maxZ = position.z + halfDepth;

            // ขอบเขต Grid สูงสุด
            float gridLimitX = (gridColumns * gridSize) * 0.5f;
            float gridLimitZ = (gridRows * gridSize) * 0.5f;

            // 3. ตรวจสอบ X, Z (เผื่อค่าครึ่ง grid เพื่อให้วางของตรงขอบได้)
            float tolerance = gridSize * 0.5f + 0.01f;
            if (minX < -gridLimitX - tolerance || maxX > gridLimitX + tolerance) return false;
            if (minZ < -gridLimitZ - tolerance || maxZ > gridLimitZ + tolerance) return false;

            // 4. ตรวจสอบ Y (ห้ามจมดิน)
            // position.y คือตำแหน่ง Pivot, เราต้องหาตำแหน่งฐาน (Bottom)
            float pivotToBottom = GetPivotToBottomOffset(data);
            float bottomY = position.y - pivotToBottom;

            if (bottomY < -0.01f) return false;

            return true;
        }
        // --------------------------------------------------------------------------------
        // STRUCTURAL SUPPORT CHECK (ป้องกันวางลอยกลางอากาศ)
        // --------------------------------------------------------------------------------

        /// <summary>
        /// ตรวจสอบว่าตำแหน่งที่จะวางมี "ฐานรองรับ" หรือไม่ (พื้น หรือ สิ่งก่อสร้างข้างเคียง)
        /// ป้องกันการวางลอยกลางอากาศโดยไม่มีจุดยึด
        /// </summary>
        private bool HasStructuralSupport(Vector3 position, float rotation, StructureData data, List<Vector3> groupPositions = null)
        {
            if (data == null || data.prefab == null) return true;

            // 1. ตรวจสอบจุดยึดกับโลกจริง (พื้นดิน หรือ โครงสร้างที่วางไปแล้ว)
            if (IsSupportedByWorld(position, rotation, data)) return true;

            // 2. พิเศษ: สำหรับระบบลากสร้าง (Drag) ให้ถือว่าชิ้นส่วนที่กำลังลาก "เกาะ" กันเองได้
            // โดยต้องมีอย่างน้อยหนึ่งชิ้นในกลุ่มที่เกาะกับโลกจริง
            if (_isDragging && _dragPositions != null && _dragPositions.Count > 1)
            {
                // เพื่อประสิทธิภาพ เราจะเช็คแค่ว่า "มีสักชิ้นในกลุ่มที่มีจุดยึดโลก" 
                // และ "ชิ้นนี้อยู่ใกล้ชิ้นอื่นในกลุ่ม"
                bool groupHasWorldSupport = false;
                foreach (var p in _dragPositions)
                {
                    if (IsSupportedByWorld(p, rotation, data))
                    {
                        groupHasWorldSupport = true;
                        break;
                    }
                }

                if (groupHasWorldSupport)
                {
                    // ชิ้นส่วนในกลุ่มลากเดียวกันถือว่ารองรับกันเอง
                    foreach (var otherPos in _dragPositions)
                    {
                        if (otherPos == position) continue;
                        // ถ้าอยู่ติดกัน (Grid size) ให้ถือว่าเกาะกัน
                        if (Vector3.Distance(position, otherPos) < gridSize * 1.5f) return true;
                    }
                }
            }

            // 3. Group support (for moving an existing building)
            if (groupPositions != null && groupPositions.Count > 1)
            {
                // Similar logic to drag placement: if any piece in group has world support, they can support each other
                bool groupHasWorldSupport = false;
                foreach (var p in groupPositions)
                {
                    if (IsSupportedByWorld(p, rotation, data))
                    {
                        groupHasWorldSupport = true;
                        break;
                    }
                }

                if (groupHasWorldSupport)
                {
                    foreach (var otherPos in groupPositions)
                    {
                        if (otherPos == position) continue;
                        if (Vector3.Distance(position, otherPos) < gridSize * 1.5f) return true;
                    }
                }
            }

            return false;
        }

        private bool IsTunedMassDamper(StructureData data)
        {
            if (data == null) return false;
            if (data.prefab != null && data.prefab.GetComponentInChildren<TunedMassDamper>() != null) return true;
            if (!data.isGadget) return false;
            string lowerName = !string.IsNullOrEmpty(data.structureName) ? data.structureName.ToLower() : "";
            return lowerName.Contains("damper") || lowerName.Contains("tuned") || lowerName.Contains("tmd");
        }

        /// <summary>
        /// ตรวจสอบการวางของ Gadget โดยห้ามวางบนอย่างอื่นยกเว้น Ground, Floor, หรือ Pillar
        /// และห้ามวางทับซ้อนหรือวางทับบน Gadget ตัวอื่น
        /// ใช้ hitCollider จากระบบ Raycast หลัก (เมาส์) โดยตรง แทนการยิง Raycast ใหม่
        /// เพื่อป้องกัน Raycast ทะลุรูของ Gadget ไปชน Ground
        /// </summary>
        private bool IsValidGadgetPlacement(Vector3 position, float rotation, StructureData data, Collider hitCollider, Vector3 hitNormal)
        {
            if (data == null || !data.isGadget) return true;

            // 1. ตรวจสอบว่าเมาส์ชี้ไปที่พื้นผิวที่อนุญาตหรือไม่ (Ground, Floor, Pillar เท่านั้น สำหรับ gadget ทั่วไป)
            if (hitCollider == null) return false;

            bool isTmd = IsTunedMassDamper(data);

            // เช็คว่าเป็น Ground Layer หรือไม่
            bool isGround = ((1 << hitCollider.gameObject.layer) & groundLayer.value) != 0;
            
            if (isTmd)
            {
                // TMD: ต้องวางบน floor เท่านั้น (ห้ามวางบน Ground หรือ Pillar)
                if (isGround) return false;

                StructureUnit hitUnit = hitCollider.GetComponentInParent<StructureUnit>();
                if (hitUnit == null || hitUnit.Data == null || hitUnit.Data.structureType != StructureType.Floor) return false;
                // (เงื่อนไขระดับชั้นที่ 2 ถูกนำออกตามต้องการเพื่อให้สร้างบนชั้นใดก็ได้)

                // ตรวจสอบว่ามีพื้น (Floor) อยู่ตรงด้านบนของตำแหน่งที่จะวางจริงหรือไม่
                float pivotToTop = GetPivotToTopOffset(data.prefab);
                Vector3 checkStart = position + new Vector3(0, pivotToTop - 0.1f, 0);
                Vector3 boxHalfExtents = new Vector3(0.4f, 0.05f, 0.4f);
                RaycastHit[] hits = UnityEngine.Physics.BoxCastAll(
                    checkStart,
                    boxHalfExtents,
                    Vector3.up,
                    Quaternion.identity,
                    0.4f,
                    structureLayer
                );
                
                bool foundFloorAbove = false;
                foreach (var hit in hits)
                {
                    if (hit.collider.gameObject.name.Contains("Ghost")) continue;
                    var u = hit.collider.GetComponentInParent<StructureUnit>();
                    if (u != null && u.Data != null && u.Data.structureType == StructureType.Floor)
                    {
                        foundFloorAbove = true;
                        break;
                    }
                }

                if (!foundFloorAbove) return false;
            }
            else
            {
                // Gadget อื่นๆ ทั้งหมด ยกเว้น TMD: ให้วางด้านบน (y+) เท่านั้น
                if (hitNormal.y < 0.5f) return false;

                if (!isGround)
                {
                    // ไม่ใช่ Ground → เช็คว่าเป็น Structure ที่อนุญาตหรือไม่
                    StructureUnit hitUnit = hitCollider.GetComponentInParent<StructureUnit>();
                    if (hitUnit == null || hitUnit.Data == null) return false;
                    
                    // ห้ามวางบน Gadget ตัวอื่น
                    if (hitUnit.Data.isGadget) return false;
                    
                    // อนุญาตเฉพาะ Floor หรือ Pillar
                    bool isFloor = hitUnit.Data.structureType == StructureType.Floor;
                    bool isPillar = (hitUnit.Data == pillarReference) || 
                                   hitUnit.Data.structureName.IndexOf("pillar", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                                   hitUnit.Data.structureName.IndexOf("column", System.StringComparison.OrdinalIgnoreCase) >= 0 || 
                                   hitUnit.Data.structureName.IndexOf("เสา", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    
                    if (!isFloor && !isPillar) return false;
                }
            }

            // 2. ตรวจสอบว่าไม่ไปทับซ้อนกับ Gadget ตัวอื่นที่วางไว้แล้ว
            Bounds boundsA = GetGridBounds(position, rotation, data);
            
            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit == _movingUnit || _movingGroup.Contains(unit)) continue;
                if (unit.Data == null || !unit.Data.isGadget) continue;

                Bounds boundsB = GetGridBounds(unit.transform.position, unit.Rotation, unit.Data);
                
                if (boundsA.Intersects(boundsB))
                {
                    return false;
                }
                
                // ห้ามวางตรงตำแหน่ง XZ เดียวกัน (ป้องกันต่อชั้นขึ้นไปบน Gadget)
                float xzDist = Vector2.Distance(new Vector2(position.x, position.z), new Vector2(unit.transform.position.x, unit.transform.position.z));
                if (xzDist < gridSize * 0.5f)
                {
                    return false;
                }
            }

            // 3. สำหรับ SpikeTrap และ BalloonLauncher ต้องตรวจสอบว่ามีพื้นดิน (Ground) หรือพื้นสิ่งก่อสร้าง (Floor) รองรับด้านล่างโดยตรงหรือไม่
            bool isSpikeOrBalloon = false;
            if (data.prefab != null)
            {
                if (data.prefab.GetComponentInChildren<SpikeTrap>() != null || 
                    data.prefab.GetComponentInChildren<BalloonLauncher>() != null)
                {
                    isSpikeOrBalloon = true;
                }
            }

            if (!isSpikeOrBalloon)
            {
                string lowerName = !string.IsNullOrEmpty(data.structureName) ? data.structureName.ToLower() : "";
                if (lowerName.Contains("spike") || lowerName.Contains("balloon") || lowerName.Contains("launcher") || lowerName.Contains("shooter"))
                {
                    isSpikeOrBalloon = true;
                }
            }

            if (isSpikeOrBalloon)
            {
                float pivotToBottom = GetPivotToBottomOffset(data);
                Vector3 checkStart = position - new Vector3(0, pivotToBottom - 0.1f, 0);
                Vector3 boxHalfExtents = new Vector3(data.size.x * gridSize * 0.4f, 0.05f, data.size.z * gridSize * 0.4f);
                
                RaycastHit[] hitsUnder = UnityEngine.Physics.BoxCastAll(
                    checkStart,
                    boxHalfExtents,
                    Vector3.down,
                    Quaternion.Euler(0, rotation, 0),
                    0.3f,
                    groundLayer | structureLayer
                );

                bool foundSupportBelow = false;
                foreach (var hit in hitsUnder)
                {
                    if (hit.collider == null) continue;
                    if (hit.collider.gameObject.name.Contains("Ghost")) continue;
                    
                    StructureUnit hitUnit = hit.collider.GetComponentInParent<StructureUnit>();
                    if (hitUnit != null && (hitUnit == _movingUnit || _movingGroup.Contains(hitUnit))) continue;

                    bool isGroundHit = ((1 << hit.collider.gameObject.layer) & groundLayer.value) != 0;
                    bool isFloorHit = hitUnit != null && hitUnit.Data != null && hitUnit.Data.structureType == StructureType.Floor;

                    if (isGroundHit || isFloorHit)
                    {
                        foundSupportBelow = true;
                        break;
                    }
                }

                if (!foundSupportBelow)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// ตรวจสอบว่าตำแหน่งนี้มีจุดยึดกับโลกจริงหรือไม่ (พื้นดิน หรือ โครงสร้างที่วางไว้แล้ว)
        /// ไม่นับรวมชิ้นส่วนที่กำลังลากสร้างอยู่ในขณะนี้
        /// </summary>
        private bool IsSupportedByWorld(Vector3 position, float rotation, StructureData data)
        {
            // 1. ตรวจสอบพื้นดิน หรือ สิ่งก่อสร้างที่วางไปแล้ว ด้านล่างโดยตรง
            float pivotToBottom = GetPivotToBottomOffset(data);
            Vector3 bottomCenter = position - new Vector3(0, pivotToBottom, 0);
            Ray downRay = new Ray(bottomCenter + Vector3.up * 0.1f, Vector3.down);
            
            // ตรวจสอบทั้งพื้นดิน (groundLayer) และโครงสร้างอื่น (structureLayer)
            RaycastHit[] hits = UnityEngine.Physics.RaycastAll(downRay, 0.4f, groundLayer | structureLayer);
            foreach (var hit in hits)
            {
                StructureUnit unit = hit.collider.GetComponentInParent<StructureUnit>();
                if (unit != null && (unit == _movingUnit || _movingGroup.Contains(unit))) continue;
                return true;
            }

            // 2. ตรวจสอบการสัมผัสกับสิ่งก่อสร้างอื่น หรือพื้นผิวข้างเคียง (Adjacency)
            Bounds b = GetGridBounds(position, rotation, data);
            Vector3 checkSize = b.size + new Vector3(0.2f, 0.2f, 0.2f);
            
            Collider[] overlaps = UnityEngine.Physics.OverlapBox(b.center, checkSize * 0.5f, Quaternion.Euler(0, rotation, 0), groundLayer | structureLayer);
            foreach (var col in overlaps)
            {
                StructureUnit colUnit = col.GetComponentInParent<StructureUnit>();
                if (colUnit != null && (colUnit == _movingUnit || _movingGroup.Contains(colUnit))) continue;
                return true;
            }
            return false;
        }

        // --------------------------------------------------------------------------------
        // GRID VISUALIZATION (Scene View)
        // --------------------------------------------------------------------------------

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Draw the grid in Scene view based on gridColumns × gridRows
            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.3f);

            float totalWidth  = gridColumns * gridSize;
            float totalDepth  = gridRows * gridSize;
            float startX = -totalWidth * 0.5f;
            float startZ = -totalDepth * 0.5f;

            // Draw current floor level
            float floorY = Application.isPlaying ? GetFloorY(_currentFloor) : 0f;

            // Vertical lines (along Z)
            for (int x = 0; x <= gridColumns; x++)
            {
                float xPos = startX + x * gridSize;
                Gizmos.DrawLine(
                    new Vector3(xPos, floorY, startZ),
                    new Vector3(xPos, floorY, startZ + totalDepth)
                );
            }

            // Horizontal lines (along X)
            for (int z = 0; z <= gridRows; z++)
            {
                float zPos = startZ + z * gridSize;
                Gizmos.DrawLine(
                    new Vector3(startX, floorY, zPos),
                    new Vector3(startX + totalWidth, floorY, zPos)
                );
            }

            // Draw border in a brighter color
            Gizmos.color = new Color(0.3f, 0.6f, 1f, 0.7f);
            Vector3 bottomLeft  = new Vector3(startX, floorY, startZ);
            Vector3 bottomRight = new Vector3(startX + totalWidth, floorY, startZ);
            Vector3 topLeft     = new Vector3(startX, floorY, startZ + totalDepth);
            Vector3 topRight    = new Vector3(startX + totalWidth, floorY, startZ + totalDepth);
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
#endif
    }
}
