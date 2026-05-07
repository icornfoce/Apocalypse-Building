using UnityEngine;
using System.Collections.Generic;
using Simulation.Data;
using Simulation.Physics;

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

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }
        }

        private void Start()
        {
            if (mainCamera == null) mainCamera = UnityEngine.Camera.main;
            if (ghostBuilder == null) ghostBuilder = GetComponent<GhostBuilder>();
            
            // Auto-set heightStep from pillarReference if available
            if (pillarReference != null && pillarReference.prefab != null)
            {
                (Vector3 center, Vector3 size) = GetPrefabBounds(pillarReference.prefab);
                heightStep = size.y;
                Debug.Log($"[BuildingSystem] Auto-set Height Step to {heightStep} from '{pillarReference.structureName}' height.");
            }

            // หา CameraController จากกล้องหลัก
            if (mainCamera != null)
                _cameraController = mainCamera.GetComponent<Simulation.Camera.CameraController>();
        }

        private void Update()
        {
            // ปิดระบบสร้างทั้งหมดถ้ากำลังเริ่มการจำลองฟิสิกส์ (ป้องกันการวางของขณะร่วง)
            if (SimulationManager.Instance != null && SimulationManager.Instance.IsSimulating)
            {
                if (_currentMode != BuildMode.Idle) ExitMode();
                return;
            }

            UpdateRaycast();
            HandleHoverHighlight();
            HandleFloorSwitch();

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
                
                if (generalUndoSound != null) AudioSource.PlayClipAtPoint(generalUndoSound, mainCamera.transform.position);
                RecalculateMaxFloor();
            }
            else if (generalErrorSound != null)
            {
                AudioSource.PlayClipAtPoint(generalErrorSound, mainCamera.transform.position);
            }
        }

        public void Redo()
        {
            if (_redoStack.Count > 0)
            {
                var action = _redoStack.Pop();
                action.Redo();
                _undoStack.Push(action);

                if (generalRedoSound != null) AudioSource.PlayClipAtPoint(generalRedoSound, mainCamera.transform.position);
                RecalculateMaxFloor();
            }
            else if (generalErrorSound != null)
            {
                AudioSource.PlayClipAtPoint(generalErrorSound, mainCamera.transform.position);
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
        private void RecalculateMaxFloor()
        {
            _maxOccupiedFloor = 1;
            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit.Data == null) continue;
                
                float y = unit.transform.position.y;
                // ถ้า Pivot อยู่ตรงกลาง ให้ขยับลงมาที่ฐานก่อนคำนวณชั้น เพื่อความแม่นยำสูงสุด
                if (unit.Data.pivotAtCenter)
                {
                    y -= unit.Data.size.y * 0.5f;
                }

                int floor = Mathf.FloorToInt(y / HeightStep + 0.1f) + 1;
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
            if (_cameraController != null)
            {
                _cameraController.SetFloorView(_currentFloor, GetFloorY(_currentFloor));
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

            // ใช้ SphereCastAll (รัศมีเล็กๆ เพื่อจับขอบ Floor บางได้)
            float castRadius = gridSize * 0.15f;
            RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(ray, castRadius, 500f, combinedMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            // อ่านชุด occluded colliders จากกล้อง (ถ้ามี)
            var occluded = _cameraController != null ? _cameraController.OccludedColliders : null;

            // ── ให้ความสำคัญ Structure มากกว่า Ground ──
            // ถ้ามีทั้ง Structure hit และ Ground hit → เลือก Structure ก่อน
            // เพื่อแก้ปัญหา Floor บาง → Ray ทะลุไปชนพื้นดินก่อน
            RaycastHit? bestStructureHit = null;
            RaycastHit? bestGroundHit = null;

            foreach (var hit in hits)
            {
                if (occluded != null && occluded.Contains(hit.collider)) continue;

                bool isStructure = hit.collider.GetComponentInParent<StructureUnit>() != null;

                if (isStructure && bestStructureHit == null)
                {
                    bestStructureHit = hit;
                }
                else if (!isStructure && bestGroundHit == null)
                {
                    bestGroundHit = hit;
                }

                // หยุดค้นหาเมื่อเจอทั้งสองแบบแล้ว
                if (bestStructureHit != null && bestGroundHit != null) break;
            }

            // เลือก Structure hit ก่อน ถ้ามี (แม้จะไกลกว่าพื้นดินนิดหน่อย)
            RaycastHit? chosen = bestStructureHit ?? bestGroundHit;

            if (chosen.HasValue)
            {
                _currentHitPos      = chosen.Value.point;
                _currentHitNormal   = chosen.Value.normal;
                _currentHitCollider = chosen.Value.collider;
                _hasValidTarget     = true;
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
        private List<Vector3> _dragPositions = new List<Vector3>();

        private void HandlePlacementMode()
        {
            if (Input.GetMouseButtonDown(1)) { ExitMode(); return; }
            
            // Manual rotation for all structures
            if (Input.GetKeyDown(KeyCode.R) && _selectedData != null)
            {
                ghostBuilder.Rotate();
            }

            // Skip the frame where we just entered placing mode (prevents UI click from placing)
            if (_justEnteredPlacing)
            {
                _justEnteredPlacing = false;
                return;
            }

            if (_hasValidTarget && ghostBuilder.HasGhost)
            {
                Vector3 currentPos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider);
                
                if (Input.GetMouseButtonDown(0))
                {
                    _isDragging = true;
                    _dragStartPos = _currentHitPos; // Use raw hit position as stable anchor
                    _dragStartNormal = _currentHitNormal;
                    _dragStartCollider = _currentHitCollider;
                    BeginBatch();
                }

                if (_isDragging)
                {
                    // Recalculate snapped start and end every frame based on current rotation
                    // This allows walls to pivot between vertical/horizontal edges correctly when R is pressed.
                    Vector3 snappedStart = CalculatePlacementPosition(_dragStartPos, _dragStartNormal, _dragStartCollider);
                    _dragPositions = CalculateDragPositions(snappedStart, currentPos, _selectedData.size.x, _selectedData.size.z, ghostBuilder.CurrentRotation, _selectedData.structureType);
                }
                else
                {
                    _dragPositions.Clear();
                    _dragPositions.Add(currentPos);
                }

                // Update ghost previews
                bool allValid = true;
                float totalCost = 0f;
                MaterialData mat = _selectedMaterial != null ? _selectedMaterial : _selectedData.defaultMaterial;
                float materialPrice = mat != null ? mat.priceModifier : 0f;
                float itemPrice = _selectedData.basePrice + materialPrice;

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
                    bool isClear = IsAreaClear(pos, ghostBuilder.CurrentRotation, _selectedData);
                    
                    // For dragging, pieces support each other if the group is supported somewhere
                    bool hasSupport = _isDragging ? groupHasWorldSupport : HasStructuralSupport(pos, ghostBuilder.CurrentRotation, _selectedData);
                    
                    // Relax 'placeOnStructureOnly' during drag if the group is supported
                    StructureUnit hitUnit = _currentHitCollider != null ? _currentHitCollider.GetComponentInParent<StructureUnit>() : null;
                    bool isFloor = hitUnit != null && hitUnit.Data.structureType == StructureType.Floor;
                    bool isTopSurface = _currentHitNormal.y > 0.9f;

                    bool isOnStructure = !_selectedData.placeOnStructureOnly || (isFloor && isTopSurface);
                    
                    bool doorValid = true;
                    if (_selectedData.structureType == StructureType.Door)
                    {
                        doorValid = FindWallAtPosition(pos, ghostBuilder.CurrentRotation) != null;
                    }

                    if (!(isClear && hasSupport && isOnStructure && doorValid))
                    {
                        allValid = false;
                    }
                    totalCost += itemPrice;
                }

                bool canAfford = _currentBudget >= totalCost;
                ghostBuilder.UpdateGhosts(_dragPositions, ghostBuilder.CurrentRotation, allValid);

                // Placement execution on mouse up
                if (Input.GetMouseButtonUp(0) && _isDragging)
                {
                    _isDragging = false;
                    if (allValid && _dragPositions.Count > 0)
                    {
                        foreach (var pos in _dragPositions)
                        {
                            PlaceStructure(pos, ghostBuilder.CurrentRotation, _currentHitCollider);
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
            if (Input.GetMouseButtonDown(1)) { ExitMode(); return; }

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
                        EnterGroupMoveMode(building);
                        _isDraggingMove = true; // Drag mode: place on release
                        _isHoldingPickup = false;
                    }
                    // If they release quickly without moving, it's a click-to-pick (SINGLE PIECE ONLY)
                    else if (Input.GetMouseButtonUp(0))
                    {
                        List<StructureUnit> singleUnit = new List<StructureUnit> { _hoveredUnit };
                        EnterGroupMoveMode(singleUnit);
                        _isDraggingMove = false; // Click mode: stick to mouse, place on next click
                        _isHoldingPickup = false;
                    }
                }

                if (_isDragging)
                {
                    _dragPositions = CalculateDragPositions(_dragStartPos, currentPos, 1f, 1f, 0f, StructureType.Normal);
                    
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
                
                // Calculate bottom of this unit
                float bottomY = u.transform.position.y;
                if (u.Data.pivotAtCenter) bottomY -= u.Data.size.y * 0.5f;
                if (bottomY < minBottomY) minBottomY = bottomY;
            }
            
            Vector3 center = centerSum / units.Count;
            center.y = minBottomY; // Use the absolute lowest point as vertical anchor

            // Snap center to grid to ensure relative offsets are grid-aligned
            center = CalculatePlacementPosition(center, Vector3.up, null);

            List<GameObject> prefabs = new List<GameObject>();
            foreach (var unit in units)
            {
                _groupOriginalStates.Add(new GroupOriginalState { unit = unit, pos = unit.transform.position, rot = unit.Rotation });
                _groupOffsets.Add(unit.transform.position - center);
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

            // Initialize offset for the pickup
            _pivotToBottomOffset = GetPivotToBottomOffset(_movingUnit.Data);
            ghostBuilder.CreateGhost(_movingUnit.Data.prefab);
            ghostBuilder.SetRotation(_movingUnit.Rotation); // Match original rotation
        }

        // --------------------------------------------------------------------------------
        // MOVE MODE - Placement Phase
        // --------------------------------------------------------------------------------

        private void HandleMovingMode()
        {
            if (Input.GetMouseButtonDown(1)) { CancelCurrentMove(); return; }
            if (Input.GetKeyDown(KeyCode.R)) ghostBuilder.Rotate();

            if (_hasValidTarget && ghostBuilder.HasGhost)
            {
                Vector3 placePos = CalculatePlacementPosition(_currentHitPos, _currentHitNormal, _currentHitCollider);
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

                        if (!isClear || !hasSupport) { allValid = false; break; }
                    }
                }
                else
                {
                    // Single unit validity
                    bool isClear = IsAreaClear(placePos, ghostBuilder.CurrentRotation, _movingUnit.Data);
                    bool hasSupport = HasStructuralSupport(placePos, ghostBuilder.CurrentRotation, _movingUnit.Data);
                    StructureUnit hitUnit = _currentHitCollider != null ? _currentHitCollider.GetComponentInParent<StructureUnit>() : null;
                    bool isFloor = hitUnit != null && hitUnit.Data.structureType == StructureType.Floor;
                    bool isTopSurface = _currentHitNormal.y > 0.9f;
                    bool isOnStructure = !_movingUnit.Data.placeOnStructureOnly || (isFloor && isTopSurface);
                    allValid = isClear && hasSupport && isOnStructure;
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
            if (Input.GetMouseButtonDown(1)) { ExitMode(); return; }
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
                    _dragStartPos = currentPos;
                    _dragStartNormal = _currentHitNormal;
                    _dragStartCollider = _currentHitCollider;
                    BeginBatch();
                    _dragSelectedUnits.Clear();
                }

                if (_isDragging)
                {
                    _dragPositions = CalculateDragPositions(_dragStartPos, currentPos, 1f, 1f, 0f, StructureType.Normal);

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
            if (Input.GetMouseButtonDown(1)) { ExitMode(); return; }

            if (Input.GetMouseButtonDown(0))
            {
                BeginBatch();
            }

            if (Input.GetMouseButton(0) && _hoveredUnit != null && _selectedMaterial != null)
            {
                ApplyMaterialToStructure(_hoveredUnit, _selectedMaterial);
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
                ghostBuilder.CreateGhost(data.prefab);
            }
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
                float sellPrice = unit.Data.basePrice + materialPrice;
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
            MaterialData mat = _selectedMaterial != null ? _selectedMaterial : _selectedData.defaultMaterial;
            float materialPrice = mat != null ? mat.priceModifier : 0f;
            float totalCost = _selectedData.basePrice + materialPrice;

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

            GameObject obj = Instantiate(_selectedData.prefab, position, Quaternion.Euler(0, rotation, 0));
            SetLayerRecursively(obj, structureLayer);
            obj.name = $"{_selectedData.prefab.name} {GetGridPositionString(position)}";

            StructureUnit unit = obj.GetComponent<StructureUnit>() ?? obj.AddComponent<StructureUnit>();
            unit.Initialize(_selectedData, mat, rotation);
            
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
                        AttachJoint(group[i].gameObject, null); // Group move uses generic attachment
                        AttachSideJoints(group[i].gameObject);
                        IgnoreOverlappingCollisions(group[i]);
                    }
                },
                undo: () => {
                    foreach (var state in oldStates)
                    {
                        state.unit.transform.position = state.pos;
                        state.unit.transform.rotation = Quaternion.Euler(0, state.rot, 0);
                        state.unit.SetRotation(state.rot);
                        state.unit.gameObject.SetActive(true);
                        state.unit.SetHighlight(false);
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

        private void ConfirmMove(Vector3 position, float rotation, Collider targetCollider = null)
        {
            Vector3 oldPos = _moveOriginalPos;
            float oldRot = _moveOriginalRot;
            Collider oldTarget = _moveOriginalTargetCol;
            StructureUnit unit = _movingUnit;

            ExecuteCommand(
                execute: () => {
                    unit.transform.position = position;
                    unit.transform.rotation = Quaternion.Euler(0, rotation, 0);
                    unit.SetRotation(rotation);
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

        private void AttachJoint(GameObject structureObj, Collider targetCollider)
        {
            Rigidbody newRb = structureObj.GetComponent<Rigidbody>();
            if (newRb == null) return;

            // 1. Remove existing joints
            Joint[] existingJoints = structureObj.GetComponents<Joint>();
            foreach (var j in existingJoints) Destroy(j);

            // 2. Find the actual collider directly beneath this specific structure.
            // This is crucial for drag-placement, as the mouse-hit collider (targetCollider)
            // might not be the one supporting this specific instance in a line of structures.
            Collider actualTarget = null;
            
            // Raycast down from slightly ABOVE the bottom of the structure
            float pivotToBottom = GetPivotToBottomOffset(structureObj);
            
            // We start slightly ABOVE the bottom to catch the surface we are placed on,
            // but we must ignore our own colliders.
            Vector3 rayStart = structureObj.transform.position - new Vector3(0, pivotToBottom - 0.1f, 0);
            RaycastHit[] hits = UnityEngine.Physics.RaycastAll(rayStart, Vector3.down, 0.4f, groundLayer | structureLayer);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // Skip if we hit ourselves (check root to be safe with compound colliders)
                if (hit.collider.gameObject == structureObj || hit.collider.transform.IsChildOf(structureObj.transform))
                    continue;

                actualTarget = hit.collider;
                break;
            }

            // Fallback to targetCollider ONLY if it's adjacent/touching
            // This prevents dragged structures from forming long invisible joints to the start point
            if (actualTarget == null && targetCollider != null)
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

            if (actualTarget == null) return;

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
            if (targetRb == null)
            {
                targetRb = actualTarget.GetComponentInParent<Rigidbody>();
            }

            // 4. Safety Check: Cannot connect to itself
            if (targetRb == newRb)
            {
                targetRb = null;
                // If the only thing we found was ourselves, we should check if we have other support
                // For now, if it's ourselves, we treat it as if no support was found via raycast
                if (!isGround) actualTarget = null; 
            }

            if (actualTarget == null) return;

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

            // หา connected body ของ main joint เพื่อไม่สร้างซ้ำ
            Joint mainJoint = structureObj.GetComponent<Joint>();
            Rigidbody mainConnected = mainJoint != null ? mainJoint.connectedBody : null;

            Collider[] myColliders = structureObj.GetComponentsInChildren<Collider>();
            if (myColliders.Length == 0) return;

            // บังคับให้อัปเดต Bounds ของ Collider ทันทีหลังจาก SetActive(true)
            // ป้องกันปัญหา Bounds เป็น (0,0,0) ในเฟรมแรกที่ถูกสร้าง ทำให้หาชิ้นส่วนรอบๆ ไม่เจอ
            UnityEngine.Physics.SyncTransforms();

            foreach (var unit in _placedStructures)
            {
                if (unit == null || unit == newUnit) continue;

                Rigidbody otherRb = unit.GetComponent<Rigidbody>();
                if (otherRb == null || otherRb == mainConnected) continue;

                bool isAdjacent = false;
                Collider[] otherColliders = unit.GetComponentsInChildren<Collider>();

                // ใช้ Bounds Expansion ในการเช็คว่าของอยู่ติดกันหรือไม่
                // วิธีนี้รองรับชิ้นส่วนทุกขนาด (ช่วยแก้ปัญหา Connected body ไม่ยอมต่อกับของที่กว้างกว่า 1 ช่อง)
                foreach (var myCol in myColliders)
                {
                    Bounds expandedBounds = myCol.bounds;
                    expandedBounds.Expand(0.2f); // ขยายขอบเขตออกเล็กน้อยเพื่อหาของที่อยู่ติดกัน

                    foreach (var otherCol in otherColliders)
                    {
                        if (expandedBounds.Intersects(otherCol.bounds))
                        {
                            isAdjacent = true;
                            break;
                        }
                    }
                    if (isAdjacent) break;
                }

                if (!isAdjacent) continue;

                // สร้าง FixedJoint เชื่อมกับเพื่อนบ้าน
                FixedJoint sideJoint = structureObj.AddComponent<FixedJoint>();
                sideJoint.connectedBody = otherRb;
            }
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
            float sellPrice = unit.Data.basePrice + materialPrice;

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
                        float comp = unit.Data.baseMaxCompression + newMaterial.compressionModifier;
                        float tens = unit.Data.baseMaxTension     + newMaterial.tensionModifier;
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
                        float comp = unit.Data.baseMaxCompression + (oldMaterial != null ? oldMaterial.compressionModifier : 0f);
                        float tens = unit.Data.baseMaxTension     + (oldMaterial != null ? oldMaterial.tensionModifier : 0f);
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

        private Vector3 CalculatePlacementPosition(Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider)
        {
            StructureData activeData = GetActiveStructureData();
            StructureType placementType = activeData != null ? activeData.structureType : StructureType.Normal;

            float rawX = hitPoint.x;
            float rawZ = hitPoint.z;

            // เมื่อคลิกด้านข้างของ Structure ให้เลื่อนตำแหน่งไปตาม normal
            // เพื่อบังคับให้ snap ไปช่องถัดไปแทนช่องเดิม
            bool isSideHit = Mathf.Abs(hitNormal.y) < 0.5f;
            if (isSideHit && hitCollider != null
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
                // Wall / Door: snap to grid EDGES (lines between cells)
                // We snap based on the current rotation (manual via 'R' key)
                // The wall revolves around the cell center in 4 directions
                float currentRot = ghostBuilder != null ? ghostBuilder.CurrentRotation : 0f;
                
                float leftEdge = Mathf.Floor(rawX / gridSize) * gridSize;
                float rightEdge = leftEdge + gridSize;
                float bottomEdge = Mathf.Floor(rawZ / gridSize) * gridSize;
                float topEdge = bottomEdge + gridSize;

                float centerX = leftEdge + gridSize * 0.5f;
                float centerZ = bottomEdge + gridSize * 0.5f;

                // Base position is the cell center
                float baseStartX = centerX;
                float baseStartZ = centerZ;

                // Offset to the edges based on 4 directions
                // 0: Left (-X), 90: Top (+Z), 180: Right (+X), 270: Bottom (-Z)
                float rot = Mathf.Round(currentRot / 90f) * 90f;
                float modRot = rot % 360f;
                if (modRot < 0) modRot += 360f;

                if (Mathf.Abs(modRot - 0f) < 1f || Mathf.Abs(modRot - 360f) < 1f)
                {
                    // Left Edge
                    x = leftEdge; 
                    z = centerZ;
                }
                else if (Mathf.Abs(modRot - 90f) < 1f)
                {
                    // Top Edge
                    x = centerX;
                    z = topEdge; 
                }
                else if (Mathf.Abs(modRot - 180f) < 1f)
                {
                    // Right Edge
                    x = rightEdge;
                    z = centerZ;
                }
                else // 270
                {
                    // Bottom Edge
                    x = centerX;
                    z = bottomEdge;
                }

                // สำหรับ Door: ให้ลองหา Wall ที่ใกล้ที่สุดเพื่อ Snap เข้าหาโดยตรง (ช่วยให้วางง่ายขึ้น)
                if (placementType == StructureType.Door)
                {
                    StructureUnit nearbyWall = null;
                    float minWallDist = 1.0f; // ระยะดึงดูดเข้าหา Wall
                    
                    foreach (var unit in _placedStructures)
                    {
                        if (unit == null || unit.Data == null || unit.Data.structureType != StructureType.Wall) continue;
                        float d = Vector3.Distance(new Vector3(x, hitPoint.y, z), unit.transform.position);
                        if (d < minWallDist)
                        {
                            minWallDist = d;
                            nearbyWall = unit;
                        }
                    }

                    if (nearbyWall != null)
                    {
                        x = nearbyWall.transform.position.x;
                        z = nearbyWall.transform.position.z;
                        float snappedY = nearbyWall.transform.position.y; // Lock แกน Y ตาม Wall

                        // Manual rotation only
                        return new Vector3(x, snappedY, z); // Snap จบตรงนี้เลย (ใช้ Y ของ Wall โดยตรง)
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

                float centerX = SnapToCellCenter(rawX, sizeX);
                float centerZ = SnapToCellCenter(rawZ, sizeZ);

                x = centerX;
                z = centerZ;
            }

            // ── Y Calculation (unchanged logic) ──
            float y = hitPoint.y;

            if (_currentHitCollider != null)
            {
                StructureUnit hitUnit = _currentHitCollider.GetComponentInParent<StructureUnit>();
                if (hitUnit != null && hitUnit.Data != null && hitUnit.Data.prefab != null)
                {
                    float hitUnitPivotToBottom = GetPivotToBottomOffset(hitUnit.Data);
                    float hitUnitPivotToTop    = GetPivotToTopOffset(hitUnit.Data.prefab);

                    float bottomY = hitUnit.transform.position.y - hitUnitPivotToBottom;
                    float topY    = hitUnit.transform.position.y + hitUnitPivotToTop;

                    if (isSideHit)
                    {
                        y = bottomY;
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

            if (snapYToGrid)
            {
                float yStep = heightStep > 0f ? heightStep : gridSize;
                if (yStep > 0f)
                {
                    y = Mathf.Round(y / yStep) * yStep;
                }
            }

            y += _pivotToBottomOffset;

            return new Vector3(x, y, z);
        }

        // ── Snap helpers ──────────────────────────────────────────────

        /// <summary>
        /// Snap a coordinate to the center of a cell group.
        /// For size=1: center of a single cell (offset by half grid).
        /// For size=2: center spans two cells, etc.
        /// </summary>
        private float SnapToCellCenter(float raw, float cellCount)
        {
            if (!useGridSnap) return raw;

            // The total span of this structure in world units
            float span = cellCount * gridSize;

            // Snap the LEFT edge to the nearest grid line, then offset to center
            float leftEdge = raw - span * 0.5f;
            float snappedLeft = Mathf.Round(leftEdge / gridSize) * gridSize;
            return snappedLeft + span * 0.5f;
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
        /// Find an existing Wall-type StructureUnit at the given position and rotation.
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

        private float GetPivotToBottomOffset(StructureData data)
        {
            if (data == null || data.prefab == null) return 0f;

            if (data.pivotAtCenter)
            {
                // If explicitly centered, the offset is half the height
                // heightStep is the height of one grid unit vertically
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
