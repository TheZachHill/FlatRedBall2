using System.Numerics;
using FlatRedBall2.Tiled;
using MonoGame.Extended.Tilemaps;
using Shouldly;
using Xunit;

namespace FlatRedBall2.Tests.Tiled;

public class TileMapIsometricTests
{
    private static TileMap CreateIsometricMap(
        int widthTiles, int heightTiles, int tileWidth, int tileHeight,
        float x = 0f, float y = 0f)
    {
        var tilemap = new Tilemap(
            name: "test", width: widthTiles, height: heightTiles,
            tileWidth: tileWidth, tileHeight: tileHeight,
            orientation: TilemapOrientation.Isometric);
        return new TileMap(tilemap, x, y);
    }

    [Fact]
    public void Orientation_ReflectsTiledOrientation()
    {
        var map = CreateIsometricMap(4, 4, 64, 32);

        map.Orientation.ShouldBe(TilemapOrientation.Isometric);
    }

    [Fact]
    public void Orientation_OrthogonalConstructorLegacyPath_DefaultsToOrthogonal()
    {
        var map = new TileMap(160f, 160f, 16, 16, new System.Collections.Generic.List<TileMapLayer>());

        map.Orientation.ShouldBe(TilemapOrientation.Orthogonal);
    }

    [Fact]
    public void GetCellWorldPosition_MatchesIsometricFormula()
    {
        var map = CreateIsometricMap(4, 4, 64, 32);

        // MonoGame.Extended's TileToWorldIsometric returns the tile's top-left bounding-box
        // corner — (col - row) * (tileWidth / 2), (col + row) * (tileHeight / 2), Y-down — the
        // same anchor role TileToWorldOrthogonal returns for orthogonal tiles. GetCellWorldPosition
        // adds the half-tile offset to reach the center, then flips Y to FRB2's Y-up.
        foreach (var (col, row) in new[] { (0, 0), (2, 3), (3, 1) })
        {
            var expected = new Vector2(
                (col - row) * (64 / 2f) + 32f,
                -((col + row) * (32 / 2f) + 16f));
            map.GetCellWorldPosition(col, row).ShouldBe(expected);
        }
    }

    [Fact]
    public void GetCellWorldPosition_RespectsMapOffset()
    {
        var map = CreateIsometricMap(4, 4, 64, 32, x: 100f, y: 200f);

        map.GetCellWorldPosition(0, 0).ShouldBe(new Vector2(132f, 184f));
        map.GetCellWorldPosition(2, 3).ShouldBe(
            new Vector2(100f + (2 - 3) * 32f + 32f, 200f - ((2 + 3) * 16f + 16f)));
    }

    [Fact]
    public void GetCellWorldPosition_TileZeroZero_IsTileCenterNotBoundingBoxCorner()
    {
        // Regression: an earlier version returned the raw MonoGame.Extended anchor (the tile's
        // top-left bounding-box corner, (0, 0) here) instead of its center, (32, -16).
        var map = CreateIsometricMap(4, 4, 64, 32);

        map.GetCellWorldPosition(0, 0).ShouldBe(new Vector2(32f, -16f));
    }

    [Fact]
    public void GetCellAt_IsInverseOfGetCellWorldPosition()
    {
        var map = CreateIsometricMap(4, 4, 64, 32, x: 100f, y: 200f);

        foreach (var (col, row) in new[] { (0, 0), (2, 3), (3, 1) })
            map.GetCellAt(map.GetCellWorldPosition(col, row)).ShouldBe((col, row));
    }

    [Fact]
    public void Width_UsesIsometricWorldBounds()
    {
        // Isometric world bounds: (mapWidth + mapHeight) * (tileWidth / 2).
        var map = CreateIsometricMap(4, 6, 64, 32);

        map.Width.ShouldBe((4 + 6) * (64 / 2f));
        map.Height.ShouldBe((4 + 6) * (32 / 2f));
    }

    [Fact]
    public void GenerateCollisionFromClass_IsometricMap_Throws()
    {
        var tilemap = new Tilemap(
            name: "test", width: 4, height: 4, tileWidth: 64, tileHeight: 32,
            orientation: TilemapOrientation.Isometric);
        var layer = new TilemapTileLayer("Main", 4, 4, 64, 32);
        tilemap.Layers.Add(layer);
        var map = new TileMap(tilemap);

        Should.Throw<System.NotSupportedException>(() => map.GenerateCollisionFromClass("Solid"));
    }

    private class MarkerEntity : Entity { }
    private class TestScreen : Screen { }

    [Fact]
    public void CreateEntities_IsometricMap_DefaultOriginCenter_SpawnsAtTileCenter()
    {
        // Regression: an earlier version spawned at the tile's bounding-box corner instead of
        // its center for isometric maps, even with the default Origin.Center.
        var tilemap = new Tilemap(
            name: "test", width: 4, height: 4, tileWidth: 64, tileHeight: 32,
            orientation: TilemapOrientation.Isometric);
        var tileset = new MonoGame.Extended.Tilemaps.TilemapTileset(
            name: "ts", texture: null!, tileWidth: 64, tileHeight: 32, tileCount: 1, columns: 1);
        tileset.FirstGlobalId = 1;
        tileset.AddTileData(new MonoGame.Extended.Tilemaps.TilemapTileData(0) { Class = "Coin" });
        tilemap.Tilesets.Add(tileset);
        var layer = new TilemapTileLayer("Main", 4, 4, 64, 32);
        layer.SetTile(0, 0, new MonoGame.Extended.Tilemaps.TilemapTile(globalId: 1));
        tilemap.Layers.Add(layer);

        var map = new TileMap(tilemap);
        var screen = new TestScreen { Engine = new FlatRedBallService() };
        var factory = new Factory<MarkerEntity>(screen);

        var created = map.CreateEntities("Coin", factory);

        created.Count.ShouldBe(1);
        created[0].X.ShouldBe(32f);
        created[0].Y.ShouldBe(-16f);
    }
}
