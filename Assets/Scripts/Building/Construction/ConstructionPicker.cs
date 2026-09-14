using System.Collections.Generic;
using DeepSky.Building.Layout;
using DeepSky.Building.Presentation;
using UnityEngine;

namespace DeepSky.Building.Construction
{
    /// <summary>Samples structural selection regions in room-grid space.</summary>
    public static class ConstructionPicker
    {
        /// <summary>Finds floor, ceiling, side and optional ladder candidates in distance order.</summary>
        /// <param name="habitat">Base whose room occupancy is sampled.</param>
        /// <param name="ray">World-space aim ray with normalized direction.</param>
        /// <param name="reach">Positive reach in grid units, scaled by cell width and level height.</param>
        /// <param name="includeLadders">Whether ladder removal regions should be included.</param>
        /// <returns>Candidate regions within reach, including openings without colliders.</returns>
        public static IEnumerable<ConstructionTarget> Candidates(Habitat habitat, Ray ray, float reach, bool includeLadders)
        {
            float size = habitat.Plan.CellSize;
            float height = habitat.Plan.LevelHeight;
            Vector3 scaled = new(ray.direction.x / size, ray.direction.y / height, ray.direction.z / size);
            float metresPerStep = 1f / scaled.magnitude;
            for (float step = 0f; step < reach; step += .1f)
            {
                float distance = step * metresPerStep;
                Vector3 point = ray.GetPoint(distance);
                Vector3Int cell = habitat.CellAt(point);
                if (habitat.Grid.TryGet(cell, out RoomCell room))
                {
                    float fraction = (point.y - habitat.CellCenter(cell).y) / height;
                    if (includeLadders && habitat.Grid.TryGet(cell + Vector3Int.up, out RoomCell above)
                        && above.Floor == RoomFloor.Ladder)
                    {
                        yield return new ConstructionTarget(cell + Vector3Int.up, BuildingSurface.Ladder, above.LadderSide, distance);
                    }
                    if (fraction < .1f)
                    {
                        yield return new ConstructionTarget(cell,
                            includeLadders && room.Floor == RoomFloor.Ladder ? BuildingSurface.Ladder : BuildingSurface.Floor,
                            room.LadderSide, distance);
                    }
                    if (fraction > .9f)
                    {
                        yield return new ConstructionTarget(cell, BuildingSurface.Ceiling, default, distance);
                    }
                }
                else
                {
                    for (int i = 0; i < 4; i++)
                    {
                        var side = (RoomSide)i;
                        Vector3Int owner = cell - RoomCell.Offset(side);
                        if (habitat.Grid.TryGet(owner, out _))
                        {
                            yield return new ConstructionTarget(owner, BuildingSurface.Wall, side, distance);
                        }
                    }
                }
            }
        }
    }
}
