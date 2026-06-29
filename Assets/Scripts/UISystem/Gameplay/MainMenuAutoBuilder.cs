using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulation.Building;
using Simulation.Data;
using Simulation.Mission;
using Simulation.Physics;

namespace Simulation.UI
{
    public enum MainMenuBuildStyle
    {
        Random,
        Solid,
        Terraced,
        LShape,
        Atrium,
        TwinCore,
        Ruined
    }

    public class MainMenuAutoBuilder : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BuildingSystem buildingSystem;

        [Header("Structure Data")]
        [SerializeField] private StructureData pillarData;
        [SerializeField] private StructureData floorData;

        [Header("Materials")]
        [SerializeField] private MaterialData[] materials;

        [Header("Saved Builds")]
        [SerializeField] private bool useSavedBuildsWhenAvailable = true;
        [Range(0f, 1f)]
        [SerializeField] private float savedBuildChance = 0.65f;
        [SerializeField] private StructureData[] savedBuildStructures;
        [SerializeField] private MaterialData[] savedBuildMaterials;
        [SerializeField] private GadgetData[] savedBuildGadgets;

        [Header("Disasters")]
        [SerializeField] private DisasterData[] disasters;

        [Header("Budget")]
        [SerializeField] private int minBudget = 5000;
        [SerializeField] private int maxBudget = 50000;

        [Header("Floors")]
        [SerializeField] private int minFloors = 1;
        [SerializeField] private int maxFloors = 10;

        [Header("Footprint")]
        [SerializeField] private int minFootprint = 3;
        [SerializeField] private int maxFootprint = 6;

        [Header("Complex Shape")]
        [SerializeField] private MainMenuBuildStyle buildStyle = MainMenuBuildStyle.Random;
        [Range(0f, 1f)]
        [SerializeField] private float complexity = 0.65f;
        [Range(0f, 0.45f)]
        [SerializeField] private float notchChance = 0.18f;
        [Range(0f, 1f)]
        [SerializeField] private float setbackStrength = 0.55f;
        [Range(0f, 0.7f)]
        [SerializeField] private float ruinDamageChance = 0.28f;
        [Range(0f, 1f)]
        [SerializeField] private float brokenTopBias = 0.75f;

        [Header("Disaster Timing")]
        [SerializeField] private int minDisasters = 1;
        [SerializeField] private int maxDisasters = 3;
        [SerializeField] private float firstDisasterDelay = 4f;
        [SerializeField] private Vector2 disasterGap = new Vector2(5f, 9f);

        [Header("Roof Detail")]
        [Range(0f, 1f)]
        [SerializeField] private float roofFeatureChance = 0.65f;
        [SerializeField] private int maxRoofFeatureHeight = 2;

        [Header("Timing")]
        [SerializeField] private float startDelay = 0.5f;
        [SerializeField] private float placeStepDelay = 0.04f;
        [SerializeField] private float startSimDelay = 1f;

        private void Start()
        {
            StartCoroutine(RunRoutine());
        }

        private IEnumerator RunRoutine()
        {
            if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

            BuildingSystem bs = buildingSystem != null ? buildingSystem : BuildingSystem.Instance;
            if (bs == null)
            {
                Debug.LogError("[MainMenuAutoBuilder] Missing BuildingSystem in this scene.");
                yield break;
            }

            int budget = Random.Range(minBudget, maxBudget + 1);
            bs.SetBudget(budget);

            if (TryUseRandomSavedBuild(bs))
            {
                if (startSimDelay > 0f) yield return new WaitForSeconds(startSimDelay);
                if (SimulationManager.Instance != null && !SimulationManager.Instance.IsSimulating)
                {
                    SimulationManager.Instance.StartSimulation();
                }

                yield return StartCoroutine(TriggerStaggeredDisasters());
                yield break;
            }

            if (pillarData == null || pillarData.prefab == null)
            {
                Debug.LogError("[MainMenuAutoBuilder] Missing pillarData.");
                yield break;
            }

            int floors = Random.Range(Mathf.Max(1, minFloors), Mathf.Max(minFloors, maxFloors) + 1);

            int maxW = Mathf.Max(1, Mathf.Min(maxFootprint, bs.GridColumns));
            int maxD = Mathf.Max(1, Mathf.Min(maxFootprint, bs.GridRows));
            int w = Random.Range(Mathf.Clamp(minFootprint, 1, maxW), maxW + 1);
            int d = Random.Range(Mathf.Clamp(minFootprint, 1, maxD), maxD + 1);

            int col0 = (bs.GridColumns - w) / 2;
            int row0 = (bs.GridRows - d) / 2;
            MainMenuBuildStyle style = ResolveBuildStyle();

            yield return StartCoroutine(BuildTower(bs, floors, col0, row0, w, d, style));

            if (startSimDelay > 0f) yield return new WaitForSeconds(startSimDelay);
            if (SimulationManager.Instance != null && !SimulationManager.Instance.IsSimulating)
            {
                SimulationManager.Instance.StartSimulation();
            }

            yield return StartCoroutine(TriggerStaggeredDisasters());
        }

        private bool TryUseRandomSavedBuild(BuildingSystem bs)
        {
            if (!useSavedBuildsWhenAvailable || Random.value > savedBuildChance) return false;

            BuildingSaveData save = BuildingSaveStore.LoadRandom();
            if (save == null || save.pieces == null || save.pieces.Count == 0) return false;

            StructureData[] loadStructures = savedBuildStructures != null && savedBuildStructures.Length > 0
                ? savedBuildStructures
                : new[] { pillarData, floorData };
            MaterialData[] loadMaterials = savedBuildMaterials != null && savedBuildMaterials.Length > 0
                ? savedBuildMaterials
                : materials;

            bool loaded = bs.ImportPlacedStructures(save.pieces, loadStructures, loadMaterials, savedBuildGadgets, true);
            if (loaded)
            {
                Debug.Log($"[MainMenuAutoBuilder] Loaded random saved build: {save.displayName}");
            }

            return loaded;
        }

        private IEnumerator BuildTower(BuildingSystem bs, int floors, int col0, int row0, int w, int d, MainMenuBuildStyle style)
        {
            List<List<Vector2Int>> plans = GenerateFloorPlans(floors, w, d, style);

            for (int floor = 1; floor <= floors; floor++)
            {
                bool placedAnyThisFloor = false;
                List<Vector2Int> cells = plans[Mathf.Min(floor - 1, plans.Count - 1)];

                foreach (var cell in cells)
                {
                    MaterialData mat = PickMaterialWithinBudget(bs, pillarData);
                    Vector3 pos = bs.GridCellToWorld(col0 + cell.x, row0 + cell.y, floor, pillarData);
                    if (bs.TryAutoPlace(pillarData, mat, pos, 0f))
                    {
                        placedAnyThisFloor = true;
                        if (placeStepDelay > 0f) yield return new WaitForSeconds(placeStepDelay);
                    }
                }

                if (floorData != null && floorData.prefab != null)
                {
                    foreach (var cell in cells)
                    {
                        MaterialData mat = PickMaterialWithinBudget(bs, floorData);
                        Vector3 pos = bs.GridCellToWorld(col0 + cell.x, row0 + cell.y, floor + 1, floorData);
                        if (bs.TryAutoPlace(floorData, mat, pos, 0f))
                        {
                            if (placeStepDelay > 0f) yield return new WaitForSeconds(placeStepDelay);
                        }
                    }
                }

                if (!placedAnyThisFloor) yield break;
            }

            yield return StartCoroutine(BuildRoofFeature(bs, floors + 1, col0, row0, plans[plans.Count - 1], style));
        }

        private MainMenuBuildStyle ResolveBuildStyle()
        {
            if (buildStyle != MainMenuBuildStyle.Random) return buildStyle;
            return (MainMenuBuildStyle)Random.Range(1, 7);
        }

        private List<List<Vector2Int>> GenerateFloorPlans(int floors, int w, int d, MainMenuBuildStyle style)
        {
            List<Vector2Int> baseCells = GenerateBaseCells(w, d, style);
            List<List<Vector2Int>> plans = new List<List<Vector2Int>>();
            List<Vector2Int> previousFloorCells = null;

            for (int floor = 1; floor <= floors; floor++)
            {
                List<Vector2Int> cells = new List<Vector2Int>(baseCells);
                ApplyVerticalComplexity(cells, w, d, floor, floors, style);
                ApplyRuinDamage(cells, w, d, floor, floors, style);

                if (previousFloorCells != null)
                {
                    KeepOnlySupportedCells(cells, previousFloorCells);
                }

                if (cells.Count == 0)
                {
                    cells.Add(previousFloorCells != null && previousFloorCells.Count > 0
                        ? previousFloorCells[Random.Range(0, previousFloorCells.Count)]
                        : new Vector2Int(w / 2, d / 2));
                }

                plans.Add(cells);
                previousFloorCells = new List<Vector2Int>(cells);
            }

            return plans;
        }

        private List<Vector2Int> GenerateBaseCells(int w, int d, MainMenuBuildStyle style)
        {
            bool[,] mask = new bool[w, d];
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < d; y++)
                {
                    mask[x, y] = true;
                }
            }

            if (style == MainMenuBuildStyle.LShape && w >= 3 && d >= 3)
            {
                int cutW = Mathf.Max(1, w / 2);
                int cutD = Mathf.Max(1, d / 2);
                for (int x = w - cutW; x < w; x++)
                {
                    for (int y = d - cutD; y < d; y++)
                    {
                        mask[x, y] = false;
                    }
                }
            }
            else if (style == MainMenuBuildStyle.Atrium && w >= 4 && d >= 4)
            {
                int centerX = w / 2;
                int centerY = d / 2;
                mask[centerX, centerY] = false;
                if (w > 5) mask[centerX - 1, centerY] = false;
                if (d > 5) mask[centerX, centerY - 1] = false;
            }
            else if (style == MainMenuBuildStyle.TwinCore && w >= 4)
            {
                int bridgeY = d / 2;
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < d; y++)
                    {
                        mask[x, y] = x <= 1 || x >= w - 2 || y == bridgeY;
                    }
                }
            }
            else if (style == MainMenuBuildStyle.Ruined)
            {
                PunchRuinScars(mask, w, d);
            }

            ApplyBaseNotches(mask, w, d);
            return MaskToCells(mask, w, d);
        }

        private void PunchRuinScars(bool[,] mask, int w, int d)
        {
            int scars = Random.Range(1, Mathf.Max(2, Mathf.RoundToInt(1 + complexity * 4f)));
            for (int s = 0; s < scars; s++)
            {
                bool vertical = Random.value > 0.5f;
                int line = vertical ? Random.Range(0, w) : Random.Range(0, d);
                int start = vertical ? Random.Range(0, d) : Random.Range(0, w);
                int length = Random.Range(1, Mathf.Max(2, (vertical ? d : w) / 2 + 1));

                for (int i = 0; i < length; i++)
                {
                    int x = vertical ? line : start + i;
                    int y = vertical ? start + i : line;
                    if (x < 0 || y < 0 || x >= w || y >= d) continue;
                    if (CountCells(mask, w, d) <= 3) return;
                    mask[x, y] = false;
                }
            }
        }

        private void ApplyBaseNotches(bool[,] mask, int w, int d)
        {
            if (complexity <= 0f || notchChance <= 0f) return;

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < d; y++)
                {
                    if (!mask[x, y] || !IsEdgeCell(x, y, w, d)) continue;
                    if (Random.value > notchChance * complexity) continue;
                    if (CountCells(mask, w, d) <= 3) return;

                    mask[x, y] = false;
                }
            }
        }

        private void ApplyVerticalComplexity(List<Vector2Int> cells, int w, int d, int floor, int floors, MainMenuBuildStyle style)
        {
            if (floor <= 1 || cells.Count <= 3) return;

            float height01 = floors <= 1 ? 0f : (floor - 1f) / (floors - 1f);
            int inset = 0;

            if (style == MainMenuBuildStyle.Terraced)
            {
                inset = Mathf.FloorToInt(height01 * Mathf.Lerp(1f, 3f, setbackStrength));
            }
            else if (style == MainMenuBuildStyle.TwinCore && height01 > 0.45f)
            {
                RemoveBridgeCells(cells, w, d);
            }
            else if (Random.value < setbackStrength * height01 * complexity)
            {
                inset = 1;
            }

            for (int step = 0; step < inset; step++)
            {
                RemoveOuterRing(cells);
                if (cells.Count <= 3) break;
            }

            if (style == MainMenuBuildStyle.Atrium && floor > Mathf.Max(2, floors / 2))
            {
                RemoveRandomCorner(cells, w, d);
            }
        }

        private void ApplyRuinDamage(List<Vector2Int> cells, int w, int d, int floor, int floors, MainMenuBuildStyle style)
        {
            if (style != MainMenuBuildStyle.Ruined || floor <= 1 || cells.Count <= 3) return;

            float height01 = floors <= 1 ? 0f : (floor - 1f) / (floors - 1f);
            float chance = ruinDamageChance * Mathf.Lerp(0.35f, 1.5f, Mathf.Lerp(height01, 1f, brokenTopBias));

            for (int i = cells.Count - 1; i >= 0; i--)
            {
                if (cells.Count <= 3) break;

                Vector2Int cell = cells[i];
                bool exposed = IsEdgeCell(cell.x, cell.y, w, d) || Random.value < 0.25f * complexity;
                if (!exposed) continue;

                if (Random.value < chance)
                {
                    cells.RemoveAt(i);
                }
            }

            if (floor > Mathf.Max(2, floors / 2) && Random.value < complexity)
            {
                RemoveRandomCorner(cells, w, d);
            }

            if (height01 > 0.65f && Random.value < complexity * 0.8f)
            {
                RemoveOuterRing(cells);
            }
        }

        private void KeepOnlySupportedCells(List<Vector2Int> cells, List<Vector2Int> supportedCells)
        {
            cells.RemoveAll(c => !supportedCells.Contains(c));
        }

        private void RemoveOuterRing(List<Vector2Int> cells)
        {
            if (cells.Count <= 3) return;

            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;

            foreach (var cell in cells)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minY = Mathf.Min(minY, cell.y);
                maxY = Mathf.Max(maxY, cell.y);
            }

            List<Vector2Int> kept = new List<Vector2Int>();
            foreach (var cell in cells)
            {
                bool outer = cell.x == minX || cell.x == maxX || cell.y == minY || cell.y == maxY;
                if (!outer) kept.Add(cell);
            }

            if (kept.Count >= 3)
            {
                cells.Clear();
                cells.AddRange(kept);
            }
        }

        private void RemoveBridgeCells(List<Vector2Int> cells, int w, int d)
        {
            int bridgeY = d / 2;
            cells.RemoveAll(c => c.y == bridgeY && c.x > 1 && c.x < w - 2);
        }

        private void RemoveRandomCorner(List<Vector2Int> cells, int w, int d)
        {
            Vector2Int[] corners =
            {
                new Vector2Int(0, 0),
                new Vector2Int(0, d - 1),
                new Vector2Int(w - 1, 0),
                new Vector2Int(w - 1, d - 1)
            };

            if (cells.Count > 3)
            {
                cells.Remove(corners[Random.Range(0, corners.Length)]);
            }
        }

        private List<Vector2Int> MaskToCells(bool[,] mask, int w, int d)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < d; y++)
                {
                    if (mask[x, y]) cells.Add(new Vector2Int(x, y));
                }
            }
            return cells;
        }

        private IEnumerator BuildRoofFeature(BuildingSystem bs, int roofFloor, int col0, int row0, List<Vector2Int> topCells, MainMenuBuildStyle style)
        {
            if (topCells == null || topCells.Count == 0) yield break;
            float chance = style == MainMenuBuildStyle.Ruined
                ? Mathf.Max(roofFeatureChance, 0.85f)
                : roofFeatureChance;
            if (Random.value > chance * Mathf.Max(0.1f, complexity)) yield break;

            int height = Random.Range(1, Mathf.Max(1, maxRoofFeatureHeight) + 1);
            List<Vector2Int> roofCells = PickRoofFeatureCells(topCells);
            if (style == MainMenuBuildStyle.Ruined && roofCells.Count > 1 && Random.value < complexity)
            {
                roofCells.RemoveAt(Random.Range(0, roofCells.Count));
            }

            for (int h = 0; h < height; h++)
            {
                foreach (var cell in roofCells)
                {
                    MaterialData mat = PickMaterialWithinBudget(bs, pillarData);
                    Vector3 pos = bs.GridCellToWorld(col0 + cell.x, row0 + cell.y, roofFloor + h, pillarData);
                    if (bs.TryAutoPlace(pillarData, mat, pos, 0f) && placeStepDelay > 0f)
                    {
                        yield return new WaitForSeconds(placeStepDelay);
                    }
                }
            }

            if (floorData == null || floorData.prefab == null) yield break;

            foreach (var cell in roofCells)
            {
                MaterialData mat = PickMaterialWithinBudget(bs, floorData);
                Vector3 pos = bs.GridCellToWorld(col0 + cell.x, row0 + cell.y, roofFloor + height, floorData);
                if (bs.TryAutoPlace(floorData, mat, pos, 0f) && placeStepDelay > 0f)
                {
                    yield return new WaitForSeconds(placeStepDelay);
                }
            }
        }

        private List<Vector2Int> PickRoofFeatureCells(List<Vector2Int> topCells)
        {
            Vector2 center = Vector2.zero;
            foreach (var cell in topCells)
            {
                center += new Vector2(cell.x, cell.y);
            }
            center /= topCells.Count;

            List<Vector2Int> sorted = new List<Vector2Int>(topCells);
            sorted.Sort((a, b) =>
            {
                float da = Vector2.SqrMagnitude(new Vector2(a.x, a.y) - center);
                float db = Vector2.SqrMagnitude(new Vector2(b.x, b.y) - center);
                return da.CompareTo(db);
            });

            int count = Mathf.Clamp(Random.Range(1, 4), 1, sorted.Count);
            List<Vector2Int> picked = new List<Vector2Int>();
            for (int i = 0; i < count; i++)
            {
                picked.Add(sorted[i]);
            }

            return picked;
        }

        private bool IsEdgeCell(int x, int y, int w, int d)
        {
            return x == 0 || y == 0 || x == w - 1 || y == d - 1;
        }

        private int CountCells(bool[,] mask, int w, int d)
        {
            int count = 0;
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < d; y++)
                {
                    if (mask[x, y]) count++;
                }
            }
            return count;
        }

        private MaterialData PickMaterialWithinBudget(BuildingSystem bs, StructureData data)
        {
            if (materials == null || materials.Length == 0) return null;

            List<MaterialData> affordable = new List<MaterialData>();
            MaterialData cheapest = null;
            float cheapestCost = float.MaxValue;

            foreach (var material in materials)
            {
                if (material == null) continue;

                float cost = bs.GetEffectivePrice(data.basePrice + material.priceModifier);
                if (cost < cheapestCost)
                {
                    cheapestCost = cost;
                    cheapest = material;
                }

                if (cost <= bs.CurrentBudget)
                {
                    affordable.Add(material);
                }
            }

            if (affordable.Count > 0) return affordable[Random.Range(0, affordable.Count)];
            return cheapest;
        }

        private IEnumerator TriggerStaggeredDisasters()
        {
            if (disasters == null || disasters.Length == 0) yield break;
            if (MissionManager.Instance == null) yield break;

            List<DisasterData> pool = new List<DisasterData>();
            foreach (var disaster in disasters)
            {
                if (disaster != null && !pool.Contains(disaster)) pool.Add(disaster);
            }

            if (pool.Count == 0) yield break;

            for (int i = pool.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                DisasterData tmp = pool[i];
                pool[i] = pool[j];
                pool[j] = tmp;
            }

            int count = Mathf.Clamp(Random.Range(minDisasters, maxDisasters + 1), 1, pool.Count);

            if (firstDisasterDelay > 0f) yield return new WaitForSeconds(firstDisasterDelay);

            for (int i = 0; i < count; i++)
            {
                if (SimulationManager.Instance == null || !SimulationManager.Instance.IsSimulating) yield break;

                MissionManager.Instance.TriggerDisasterDirectly(pool[i]);

                if (i < count - 1)
                {
                    float gap = Random.Range(disasterGap.x, disasterGap.y);
                    yield return new WaitForSeconds(gap);
                }
            }
        }
    }
}
