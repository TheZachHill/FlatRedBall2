using System;
using System.Collections.Generic;
using System.Numerics;
using MonoGame.Extended.Tilemaps;
using FlatRedBall2.Collision;
using XnaVec2 = Microsoft.Xna.Framework.Vector2;

namespace FlatRedBall2.Tiled;

/// <summary>
/// Generates a <see cref="TileShapes"/> from a <see cref="TilemapTileLayer"/>
/// by matching tiles on their <see cref="TilemapTileData.Class"/> attribute or a custom property.
/// The all-layers overloads (<see cref="GenerateFromClass(Tilemap, string, float, float)"/>,
/// <see cref="GenerateFromProperty(Tilemap, string, float, float)"/>) also match rectangle and
/// polygon objects on any <see cref="TilemapObjectLayer"/> — the single-layer overloads are
/// tile-layer only.
/// </summary>
/// <remarks>
/// <para>
/// Both public methods take <c>mapX</c> and <c>mapY</c> parameters that position the map in
/// world space. <c>mapX</c> is the <b>left edge</b> of the map; <c>mapY</c> is the <b>top edge</b>
/// (because Tiled's origin is top-left). The generator converts to Y-up internally — callers do
/// not need to flip anything.
/// </para>
/// <para>
/// Tile matching uses the tileset metadata from <see cref="Tilemap.Tilesets"/>. Only tiles with a
/// non-zero global ID that pass the predicate produce collision rectangles.
/// </para>
/// <para>
/// Object matching checks <see cref="TilemapRectangleObject"/> and <see cref="TilemapPolygonObject"/>
/// entries directly — tile objects (Tiled's "Insert Tile" tool) are not matched; use
/// <see cref="TileMap.CreateEntities"/> for those. Each matched object is positioned relative to
/// whichever grid cell contains its center, the same convention used for per-tile
/// <see cref="TilemapTileData.CollisionObjects"/> — an object must fit within roughly one cell to
/// be found by broad-phase collision queries. Rotated objects (<c>Rotation != 0</c>) throw
/// <see cref="InvalidOperationException"/> rather than silently producing wrong geometry.
/// </para>
/// </remarks>
public static class TileMapCollisions
{
    /// <summary>
    /// Scans every tile in <paramref name="layer"/> and adds a collision rectangle for each tile
    /// whose tileset <see cref="TilemapTileData.Class"/> equals <paramref name="className"/>
    /// (case-insensitive).
    /// </summary>
    /// <param name="tilemap">The parsed tilemap — provides tile dimensions and tileset metadata.</param>
    /// <param name="layer">The tile layer to scan.</param>
    /// <param name="className">
    /// The <see cref="TilemapTileData.Class"/> value to match (case-insensitive).
    /// In Tiled, this is the "Class" field on a tile in the tileset editor.
    /// </param>
    /// <param name="mapX">Left edge of the map in world space.</param>
    /// <param name="mapY">
    /// Top edge of the map in world space (Tiled convention). The generator converts to Y-up
    /// internally — pass the top edge, not the bottom.
    /// </param>
    /// <returns>A <see cref="TileShapes"/> containing one rectangle per matching tile.</returns>
    public static TileShapes GenerateFromClass(
        Tilemap tilemap,
        TilemapTileLayer layer,
        string className,
        float mapX = 0f,
        float mapY = 0f)
    {
        return Generate(tilemap, layer, mapX, mapY,
            tileData => string.Equals(tileData.Class, className, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Scans every tile in <paramref name="layer"/> and adds a collision rectangle for each tile
    /// whose tileset definition contains a custom property named <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="tilemap">The parsed tilemap — provides tile dimensions and tileset metadata.</param>
    /// <param name="layer">The tile layer to scan.</param>
    /// <param name="propertyName">
    /// The custom property name to look for on each tile's tileset data.
    /// The property value is ignored — only its presence matters.
    /// </param>
    /// <param name="mapX">Left edge of the map in world space.</param>
    /// <param name="mapY">
    /// Top edge of the map in world space (Tiled convention). The generator converts to Y-up
    /// internally — pass the top edge, not the bottom.
    /// </param>
    /// <returns>A <see cref="TileShapes"/> containing one rectangle per matching tile.</returns>
    public static TileShapes GenerateFromProperty(
        Tilemap tilemap,
        TilemapTileLayer layer,
        string propertyName,
        float mapX = 0f,
        float mapY = 0f)
    {
        return Generate(tilemap, layer, mapX, mapY,
            tileData => tileData.Properties.TryGetValue(propertyName, out _));
    }

    /// <summary>
    /// Scans every tile in all tile layers of <paramref name="tilemap"/> and adds a collision
    /// rectangle for each tile whose tileset <see cref="TilemapTileData.Class"/> equals
    /// <paramref name="className"/> (case-insensitive).
    /// </summary>
    /// <param name="tilemap">The parsed tilemap — provides tile dimensions and tileset metadata.</param>
    /// <param name="className">
    /// The <see cref="TilemapTileData.Class"/> value to match (case-insensitive).
    /// </param>
    /// <param name="mapX">Left edge of the map in world space.</param>
    /// <param name="mapY">
    /// Top edge of the map in world space (Tiled convention). The generator converts to Y-up
    /// internally — pass the top edge, not the bottom.
    /// </param>
    /// <returns>A <see cref="TileShapes"/> containing one rectangle per matching tile across all layers.</returns>
    public static TileShapes GenerateFromClass(
        Tilemap tilemap,
        string className,
        float mapX = 0f,
        float mapY = 0f)
    {
        return GenerateFromAllLayers(tilemap, mapX, mapY,
            tileData => string.Equals(tileData.Class, className, StringComparison.OrdinalIgnoreCase),
            obj => string.Equals(obj.Class, className, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Scans every tile in all tile layers of <paramref name="tilemap"/> and adds a collision
    /// rectangle for each tile whose tileset definition contains a custom property named
    /// <paramref name="propertyName"/>.
    /// </summary>
    /// <param name="tilemap">The parsed tilemap — provides tile dimensions and tileset metadata.</param>
    /// <param name="propertyName">
    /// The custom property name to look for on each tile's tileset data.
    /// The property value is ignored — only its presence matters.
    /// </param>
    /// <param name="mapX">Left edge of the map in world space.</param>
    /// <param name="mapY">
    /// Top edge of the map in world space (Tiled convention). The generator converts to Y-up
    /// internally — pass the top edge, not the bottom.
    /// </param>
    /// <returns>A <see cref="TileShapes"/> containing one rectangle per matching tile across all layers.</returns>
    public static TileShapes GenerateFromProperty(
        Tilemap tilemap,
        string propertyName,
        float mapX = 0f,
        float mapY = 0f)
    {
        return GenerateFromAllLayers(tilemap, mapX, mapY,
            tileData => tileData.Properties.TryGetValue(propertyName, out _),
            obj => obj.Properties.TryGetValue(propertyName, out _));
    }

    /// <summary>
    /// Repopulates an existing <see cref="TileShapes"/> from the given layer using the
    /// supplied predicate. Caller is responsible for clearing <paramref name="target"/> first if
    /// stale cells should be removed. Used by <see cref="TileMap.TryReload"/>.
    /// </summary>
    internal static void RegenerateInto(
        Tilemap tilemap,
        TilemapTileLayer layer,
        Func<TilemapTileData, bool> predicate,
        TileShapes target)
    {
        AddMatchingTiles(tilemap, layer, predicate, target);
    }

    /// <summary>
    /// All-layers variant of <see cref="RegenerateInto(Tilemap, TilemapTileLayer, Func{TilemapTileData, bool}, TileShapes)"/>.
    /// Also re-scans object layers using <paramref name="objectPredicate"/>.
    /// </summary>
    internal static void RegenerateInto(
        Tilemap tilemap,
        Func<TilemapTileData, bool> predicate,
        Func<TilemapObject, bool> objectPredicate,
        TileShapes target)
    {
        // target.X/Y were set from mapX/mapY by the original Generate call; mapY (the map's top
        // edge) is recovered from target.Y (the bottom edge) since object conversion needs the
        // top-edge convention. See GenerateFromAllLayers.
        float mapX = target.X;
        float mapY = target.Y + tilemap.Height * tilemap.TileHeight;

        foreach (var layer in tilemap.Layers)
        {
            switch (layer)
            {
                case TilemapTileLayer tileLayer:
                    AddMatchingTiles(tilemap, tileLayer, predicate, target);
                    break;
                case TilemapObjectLayer objectLayer:
                    AddMatchingObjects(objectLayer, mapX, mapY, objectPredicate, target);
                    break;
            }
        }
    }

    /// <summary>
    /// Core generator. Iterates every cell in the layer, resolves tileset metadata for non-empty
    /// tiles, and adds a collision rectangle for each tile that satisfies <paramref name="predicate"/>.
    /// Tiled rows (Y-down) are flipped to engine rows (Y-up).
    /// </summary>
    private static TileShapes Generate(
        Tilemap tilemap,
        TilemapTileLayer layer,
        float mapX,
        float mapY,
        Func<TilemapTileData, bool> predicate)
    {
        // mapY is the top edge (Tiled convention). TileShapes.Y is the bottom edge
        // (Y-up convention). Convert: bottom = top - totalHeight.
        var collection = new TileShapes
        {
            X = mapX,
            Y = mapY - layer.Height * tilemap.TileHeight,
            GridSize = tilemap.TileWidth
        };

        AddMatchingTiles(tilemap, layer, predicate, collection);
        return collection;
    }

    private static TileShapes GenerateFromAllLayers(
        Tilemap tilemap,
        float mapX,
        float mapY,
        Func<TilemapTileData, bool> tilePredicate,
        Func<TilemapObject, bool> objectPredicate)
    {
        var collection = new TileShapes
        {
            X = mapX,
            // Map-level height, not a per-layer height — must be set before any object layer is
            // processed (object layers may appear before tile layers in Tiled's layer order).
            Y = mapY - tilemap.Height * tilemap.TileHeight,
            GridSize = tilemap.TileWidth
        };

        foreach (var layer in tilemap.Layers)
        {
            switch (layer)
            {
                case TilemapTileLayer tileLayer:
                    AddMatchingTiles(tilemap, tileLayer, tilePredicate, collection);
                    break;
                case TilemapObjectLayer objectLayer:
                    AddMatchingObjects(objectLayer, mapX, mapY, objectPredicate, collection);
                    break;
            }
        }

        return collection;
    }

    /// <summary>
    /// Scans rectangle and polygon objects on <paramref name="layer"/> and adds a collision shape
    /// for each whose <see cref="TilemapObject.Class"/> or properties satisfy
    /// <paramref name="predicate"/>. Each shape is positioned relative to whichever grid cell
    /// contains its center — the same convention used for per-tile <see cref="TilemapTileData.CollisionObjects"/>.
    /// Tile objects (placed with Tiled's "Insert Tile" tool) are not matched here — use
    /// <see cref="TileMap.CreateEntities"/> for those.
    /// </summary>
    private static void AddMatchingObjects(
        TilemapObjectLayer layer,
        float mapX,
        float mapY,
        Func<TilemapObject, bool> predicate,
        TileShapes collection)
    {
        foreach (var obj in layer.Objects)
        {
            if (!predicate(obj)) continue;

            switch (obj)
            {
                case TilemapRectangleObject rectObj:
                    AddRectangleObject(rectObj, mapX, mapY, collection);
                    break;
                case TilemapPolygonObject polyObj:
                    AddPolygonObject(polyObj, mapX, mapY, collection);
                    break;
            }
        }
    }

    private static void AddRectangleObject(TilemapRectangleObject rectObj, float mapX, float mapY, TileShapes collection)
    {
        ThrowIfRotated(rectObj);

        float w = rectObj.Size.X;
        float h = rectObj.Size.Y;
        // Tiled rect: top-left (Position.X, Position.Y), Y-down from map top-left.
        float worldCenterX = mapX + rectObj.Position.X + w / 2f;
        float worldCenterY = mapY - (rectObj.Position.Y + h / 2f);

        var (col, row) = collection.GetCellAt(new Vector2(worldCenterX, worldCenterY));
        var cellCenter = collection.GetCellWorldPosition(col, row);

        collection.AddRectangleTileAtCell(col, row,
            worldCenterX - cellCenter.X, worldCenterY - cellCenter.Y, w, h);
    }

    private static void AddPolygonObject(TilemapPolygonObject polyObj, float mapX, float mapY, TileShapes collection)
    {
        if (polyObj.Points == null || polyObj.Points.Length < 3) return;
        ThrowIfRotated(polyObj);

        var worldPoints = new List<Vector2>(polyObj.Points.Length);
        float sumX = 0f, sumY = 0f;
        foreach (var p in polyObj.Points)
        {
            XnaVec2 tiled = polyObj.Position + p;
            float wx = mapX + tiled.X;
            float wy = mapY - tiled.Y;
            worldPoints.Add(new Vector2(wx, wy));
            sumX += wx;
            sumY += wy;
        }

        // Owning cell is the polygon's average-point centroid — there's no single "position"
        // corner to key off, unlike a rectangle's top-left.
        var centroid = new Vector2(sumX / worldPoints.Count, sumY / worldPoints.Count);
        var (col, row) = collection.GetCellAt(centroid);
        var cellCenter = collection.GetCellWorldPosition(col, row);

        var localPoints = new List<Vector2>(worldPoints.Count);
        foreach (var p in worldPoints)
            localPoints.Add(p - cellCenter);

        collection.AddPolygonTileAtCell(col, row, Polygon.FromPoints(localPoints));
    }

    private static void ThrowIfRotated(TilemapObject obj)
    {
        if (obj.Rotation != 0f)
            throw new InvalidOperationException(
                $"Tiled object '{obj.Name}' (Class '{obj.Class}') has Rotation={obj.Rotation}, " +
                "which is not supported for collision generation. Remove rotation in Tiled.");
    }

    private static void AddMatchingTiles(
        Tilemap tilemap,
        TilemapTileLayer layer,
        Func<TilemapTileData, bool> predicate,
        TileShapes collection)
    {
        for (int row = 0; row < layer.Height; row++)
        {
            for (int col = 0; col < layer.Width; col++)
            {
                TilemapTile? tileNullable = layer.GetTile(col, row);
                if (!tileNullable.HasValue || tileNullable.Value.GlobalId == 0)
                    continue;

                TilemapTile tile = tileNullable.Value;

                TilemapTileData? tileData = tile.GetTileData(tilemap.Tilesets);
                if (tileData == null || !predicate(tileData))
                    continue;

                // Tiled is Y-down; TileShapes is Y-up. Flip the row.
                int flippedRow = layer.Height - 1 - row;

                BuildCollisionShapes(tileData, collection.GridSize, tile.FlipFlags,
                    out var polygons, out var rects);

                if (polygons == null && rects == null)
                {
                    collection.AddTileAtCell(col, flippedRow);
                    continue;
                }

                if (polygons != null)
                    foreach (var proto in polygons)
                        collection.AddPolygonTileAtCell(col, flippedRow, proto);

                if (rects != null)
                    foreach (var r in rects)
                        collection.AddRectangleTileAtCell(col, flippedRow, r.cx, r.cy, r.w, r.h);
            }
        }
    }

    // Converts polygon and rectangle collision objects on the tile into local-space shapes
    // centered on (0, 0) with Y-up. Applies Tiled flip flags (diagonal, then horizontal, then
    // vertical) per Tiled's rendering semantics. A tile with any collision object emits those
    // custom shapes instead of the default full-cell rect. Ellipse and polyline collision
    // objects are ignored — see TODOS.md.
    private static void BuildCollisionShapes(
        TilemapTileData tileData,
        float gridSize,
        TilemapTileFlipFlags flipFlags,
        out List<Polygon>? polygons,
        out List<(float cx, float cy, float w, float h)>? rects)
    {
        polygons = null;
        rects = null;
        if (tileData.CollisionObjects == null || tileData.CollisionObjects.Count == 0)
            return;

        float half = gridSize / 2f;
        bool flipD = (flipFlags & TilemapTileFlipFlags.FlipDiagonally) != 0;
        bool flipH = (flipFlags & TilemapTileFlipFlags.FlipHorizontally) != 0;
        bool flipV = (flipFlags & TilemapTileFlipFlags.FlipVertically) != 0;

        foreach (var obj in tileData.CollisionObjects)
        {
            if (obj is TilemapPolygonObject polyObj && polyObj.Points != null && polyObj.Points.Length >= 3)
            {
                var localPoints = new List<Vector2>(polyObj.Points.Length);
                foreach (var p in polyObj.Points)
                {
                    // Tiled pixel (Y-down, origin at tile top-left) → FRB2 local (Y-up, centered).
                    XnaVec2 tiled = polyObj.Position + p;
                    float x = tiled.X - half;
                    float y = half - tiled.Y;
                    ApplyFlips(ref x, ref y, flipD, flipH, flipV);
                    localPoints.Add(new Vector2(x, y));
                }

                polygons ??= new List<Polygon>();
                polygons.Add(Polygon.FromPoints(localPoints));
            }
            else if (obj is TilemapRectangleObject rectObj)
            {
                // Tiled rect: top-left (Position.X, Position.Y), size (Size.X, Size.Y), Y-down.
                // Convert center to FRB2 local (Y-up, centered on cell).
                float w = rectObj.Size.X;
                float h = rectObj.Size.Y;
                float cx = rectObj.Position.X + w / 2f - half;
                float cy = half - (rectObj.Position.Y + h / 2f);

                // Diagonal flip transposes across the tile's main diagonal — swap center and size.
                if (flipD)
                {
                    (cx, cy) = (-cy, -cx);
                    (w, h) = (h, w);
                }
                if (flipH) cx = -cx;
                if (flipV) cy = -cy;

                rects ??= new List<(float, float, float, float)>();
                rects.Add((cx, cy, w, h));
            }
        }
    }

    // Applies Tiled flip flags in declared D → H → V order in centered-Y-up local space.
    // Winding may reverse under odd flip counts; callers that produce polygons rely on
    // Polygon.FromPoints / SAT to normalize winding internally.
    private static void ApplyFlips(ref float x, ref float y, bool flipD, bool flipH, bool flipV)
    {
        if (flipD) (x, y) = (-y, -x);
        if (flipH) x = -x;
        if (flipV) y = -y;
    }
}
