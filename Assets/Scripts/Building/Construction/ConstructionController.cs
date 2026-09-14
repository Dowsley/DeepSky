using DeepSky.Building.Layout;
using DeepSky.Building.Presentation;
using DeepSky.Player;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;

namespace DeepSky.Building.Construction
{
    public enum ConstructionAction { Floor, Window, Level, Opening, Ladder, Remove, Base }

    /// <summary>Targets and applies unlimited construction operations.</summary>
    public sealed class ConstructionController : MonoBehaviour
    {
        [SerializeField] private BuildingWorld buildings = null!;
        [SerializeField] private Camera view = null!;
        [SerializeField] private PlayerInputContext input = null!;
        [SerializeField] private LineRenderer preview = null!;
        [Tooltip("Aim reach in grid units; vertical reach scales with level height.")]
        [SerializeField, Min(1f)] private float gridReach = 3f;
        [SerializeField, Min(1f)] private float siteReach = 15f;

        private ConstructionTarget target;
        private Habitat? habitat;
        private Vector3Int cell = Vector3Int.zero;
        private Vector2 site = Vector2.zero;
        private Bounds selection = new();
        private bool hasSelection = false;
        private RoomSide facing = RoomSide.North;
        private LevelConnection connection;
        private readonly Vector3[] outline = new Vector3[16];

        public ConstructionAction Action { get; private set; } = ConstructionAction.Floor;
        public string Reason { get; private set; } = "";
        public bool Active => input.ConstructionActive && !input.InventoryOpen;

        /// <summary>Validates scene bindings.</summary>
        private void Awake()
        {
            Assert.IsNotNull(buildings, nameof(buildings));
            Assert.IsNotNull(view, nameof(view));
            Assert.IsNotNull(input, nameof(input));
            Assert.IsNotNull(preview, nameof(preview));
        }

        /// <summary>Reads construction input, validates the aim target and displays placement feedback.</summary>
        private void Update()
        {
            preview.enabled = false;
            if (!Active || !input.GameplayActive)
            {
                return;
            }
            Keyboard? keyboard = Keyboard.current;
            Mouse? mouse = Mouse.current;
            if (keyboard != null)
            {
                for (int i = 0; i < 7; i++)
                {
                    if (keyboard[(Key)((int)Key.Digit1 + i)].wasPressedThisFrame)
                    {
                        Action = (ConstructionAction)i;
                    }
                }
            }
            if (mouse != null && Mathf.Abs(mouse.scroll.ReadValue().y) > .01f)
            {
                int step = mouse.scroll.ReadValue().y > 0f ? -1 : 1;
                Action = (ConstructionAction)(((int)Action + step + 7) % 7);
            }
            FindTarget();
            ShowPreview();
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && hasSelection && Reason.Length == 0)
            {
                Apply();
            }
        }

        /// <summary>Clears the transient selection when construction is disabled.</summary>
        private void OnDisable()
        {
            if (preview)
            {
                preview.enabled = false;
            }
        }

        /// <summary>Selects the nearest eligible grid region or a seabed site.</summary>
        private void FindTarget()
        {
            hasSelection = false;
            target = default;
            habitat = null;
            Reason = "Aim at a base";
            var ray = new Ray(view.transform.position, view.transform.forward);
            facing = (RoomSide)(Mathf.RoundToInt(view.transform.eulerAngles.y / 90f) % 4);
            if (Action == ConstructionAction.Base)
            {
                if (!Physics.Raycast(ray, out RaycastHit hit, siteReach, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) || !hit.collider.GetComponent<DeepSky.Harvesting.MineableTerrain>())
                {
                    Reason = "Aim at the seabed";
                    return;
                }
                site = new Vector2(Mathf.Floor(hit.point.x / 2f) * 2f + 1f, Mathf.Floor(hit.point.z / 2f) * 2f + 1f);
                Reason = buildings.CheckNewBase(site, out Vector3 anchor);
                selection = buildings.StarterBounds(anchor);
                hasSelection = true;
                return;
            }
            float closest = float.PositiveInfinity;
            foreach (Habitat owner in buildings.Habitats)
            {
                foreach (ConstructionTarget candidate in ConstructionPicker.Candidates(owner, ray, gridReach, Action == ConstructionAction.Remove))
                {
                    string reason = CheckTarget(owner, candidate, out Vector3Int operationCell, out LevelConnection planned);
                    bool valid = reason.Length == 0;
                    if (hasSelection && (Reason.Length == 0 && !valid || valid == (Reason.Length == 0) && candidate.Distance >= closest))
                    {
                        continue;
                    }
                    target = candidate;
                    habitat = owner;
                    cell = operationCell;
                    connection = planned;
                    Reason = reason;
                    closest = candidate.Distance;
                    hasSelection = true;
                    if (valid)
                    {
                        break;
                    }
                }
            }
            if (habitat)
            {
                SetSelectionBounds();
            }
        }

        /// <summary>Validates a candidate using layout and world-space placement rules.</summary>
        /// <param name="owner">Base being edited.</param>
        /// <param name="candidate">Sampled structural region.</param>
        /// <param name="operationCell">Cell changed by the operation.</param>
        /// <param name="planned">Vertical connection when selecting Level.</param>
        /// <returns>Empty for an eligible operation, otherwise a placement reason.</returns>
        private string CheckTarget(Habitat owner, ConstructionTarget candidate, out Vector3Int operationCell, out LevelConnection planned)
        {
            operationCell = candidate.Cell;
            planned = default;
            switch (Action)
            {
                case ConstructionAction.Floor:
                    if (candidate.Surface == BuildingSurface.Wall)
                    {
                        operationCell += RoomCell.Offset(candidate.Side);
                    }
                    else if (candidate.Surface != BuildingSurface.Floor)
                    {
                        return "Aim at a floor opening or side";
                    }
                    if (owner.Grid.TryGet(operationCell, out _))
                    {
                        return owner.Grid.CheckFloorChange(operationCell, RoomFloor.Solid);
                    }
                    string expansion = owner.Grid.CheckExpansion(operationCell);
                    return expansion.Length > 0 ? expansion : buildings.CheckRoomSpace(owner, operationCell);
                case ConstructionAction.Window:
                    if (candidate.Surface != BuildingSurface.Wall)
                    {
                        return "Aim at an exterior wall";
                    }
                    owner.Grid.TryGet(operationCell, out RoomCell wall);
                    return wall.HasWindow(candidate.Side) ? "Already glass" : "";
                case ConstructionAction.Level:
                    if (candidate.Surface != BuildingSurface.Floor && candidate.Surface != BuildingSurface.Ceiling)
                    {
                        return "Aim at a floor or ceiling";
                    }
                    string level = owner.Grid.CheckLevel(operationCell, candidate.Surface == BuildingSurface.Ceiling,
                        (RoomSide)(((int)facing + 2) % 4), out planned);
                    if (level.Length > 0)
                    {
                        return level;
                    }
                    foreach (Vector3Int room in planned.Rooms())
                    {
                        if (!owner.Grid.TryGet(room, out _))
                        {
                            string space = buildings.CheckRoomSpace(owner, room);
                            if (space.Length > 0)
                            {
                                return space;
                            }
                        }
                    }
                    return "";
                case ConstructionAction.Opening:
                    return candidate.Surface == BuildingSurface.Floor
                        ? owner.Grid.CheckFloorChange(operationCell, RoomFloor.Opening) : "Aim at a floor";
                case ConstructionAction.Ladder:
                    return candidate.Surface == BuildingSurface.Floor
                        ? owner.Grid.CheckLadder(operationCell, facing, out _, out _) : "Aim at a floor beside an opening";
                case ConstructionAction.Remove:
                    if (candidate.Surface == BuildingSurface.Ceiling && owner.Grid.TryGet(operationCell + Vector3Int.up, out _))
                    {
                        operationCell += Vector3Int.up;
                        return owner.Grid.CheckFloorChange(operationCell, RoomFloor.Opening);
                    }
                    return candidate.Surface switch
                    {
                        BuildingSurface.Floor or BuildingSurface.Ladder => owner.Grid.CheckFloorChange(operationCell, RoomFloor.Opening),
                        BuildingSurface.Wall => owner.Grid.CheckRemoval(operationCell),
                        _ => "Aim at a floor, ladder or exterior wall"
                    };
                default:
                    return "Aim at a base";
            }
        }

        /// <summary>Outlines the edited floor, side or vertical connection at its grid position.</summary>
        private void SetSelectionBounds()
        {
            if (!habitat)
            {
                return;
            }
            float height = habitat.Plan.LevelHeight;
            float size = habitat.Plan.CellSize;
            Vector3 center = habitat.CellCenter(cell);
            selection = new Bounds(center, new Vector3(size, .12f, size));
            if (Action == ConstructionAction.Level && Reason.Length == 0)
            {
                selection = new Bounds(habitat.CellCenter(connection.Lower) + Vector3.up * height * .5f,
                    new Vector3(size, height, size));
                selection.Encapsulate(new Bounds(habitat.CellCenter(connection.Landing), new Vector3(size, .12f, size)));
                if (connection.Downward)
                {
                    selection.Encapsulate(new Bounds(habitat.CellCenter(connection.LowerLanding), new Vector3(size, .12f, size)));
                }
            }
            else if (Action == ConstructionAction.Ladder && Reason.Length == 0
                && habitat.Grid.CheckLadder(cell, facing, out Vector3Int opening, out _) == "")
            {
                selection = new Bounds(habitat.CellCenter(opening) - Vector3.up * height * .5f,
                    new Vector3(size, height, size));
            }
            else if (target.Surface == BuildingSurface.Wall)
            {
                selection = new Bounds(center + Vector3.up * height * .5f, new Vector3(size, height, size));
            }
        }

        /// <summary>Applies the validated operation without charging the inventory.</summary>
        private void Apply()
        {
            if (Action == ConstructionAction.Base)
            {
                buildings.TryPlace(site);
                return;
            }
            if (!habitat)
            {
                return;
            }
            switch (Action)
            {
                case ConstructionAction.Floor:
                    if (habitat.Grid.TryGet(cell, out _))
                    {
                        habitat.Grid.TrySetFloor(cell, RoomFloor.Solid);
                    }
                    else
                    {
                        habitat.Grid.TryExpand(cell);
                    }
                    break;
                case ConstructionAction.Window:
                    habitat.Grid.TryAddWindow(cell, target.Side);
                    break;
                case ConstructionAction.Level:
                    habitat.Grid.TryAddLevel(cell, target.Surface == BuildingSurface.Ceiling,
                        (RoomSide)(((int)facing + 2) % 4));
                    break;
                case ConstructionAction.Opening:
                    habitat.Grid.TrySetFloor(cell, RoomFloor.Opening);
                    break;
                case ConstructionAction.Ladder:
                    habitat.Grid.TryAddLadder(cell, facing);
                    break;
                case ConstructionAction.Remove:
                    if (target.Surface != BuildingSurface.Wall)
                    {
                        habitat.Grid.TrySetFloor(cell, RoomFloor.Opening);
                    }
                    else
                    {
                        habitat.Grid.TryRemove(cell);
                        buildings.RemoveEmpty(habitat);
                    }
                    break;
            }
        }

        /// <summary>Draws a world-space wireframe with validity and removal colors.</summary>
        private void ShowPreview()
        {
            if (!hasSelection)
            {
                return;
            }
            Vector3 a = selection.min;
            Vector3 b = selection.max;
            outline[0] = new(a.x,a.y,a.z); outline[1] = new(b.x,a.y,a.z);
            outline[2] = new(b.x,a.y,b.z); outline[3] = new(a.x,a.y,b.z);
            outline[4] = outline[0]; outline[5] = new(a.x,b.y,a.z);
            outline[6] = new(b.x,b.y,a.z); outline[7] = new(b.x,b.y,b.z);
            outline[8] = new(a.x,b.y,b.z); outline[9] = outline[5];
            outline[10] = outline[8]; outline[11] = outline[3];
            outline[12] = outline[2]; outline[13] = outline[7];
            outline[14] = outline[6]; outline[15] = outline[1];
            preview.positionCount = outline.Length;
            preview.SetPositions(outline);
            Color color = Reason.Length > 0 ? new Color(.9f,.25f,.18f,.9f)
                : Action == ConstructionAction.Remove ? new Color(1f,.6f,.2f,.9f) : new Color(.5f,.95f,.8f,.9f);
            preview.startColor = color;
            preview.endColor = color;
            preview.enabled = true;
        }
    }
}
