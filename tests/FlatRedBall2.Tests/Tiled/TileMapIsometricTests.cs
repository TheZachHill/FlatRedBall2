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

        // Known isometric formula: worldX = (col - row) * (tileWidth / 2),
        // worldY = -(col + row) * (tileHeight / 2) after the Y-down -> Y-up flip.
        foreach (var (col, row) in new[] { (0, 0), (2, 3), (3, 1) })
        {
            var expected = new Vector2(
                (col - row) * (64 / 2f),
                -(col + row) * (32 / 2f));
            map.GetCellWorldPosition(col, row).ShouldBe(expected);
        }
    }

    [Fact]
    public void GetCellWorldPosition_RespectsMapOffset()
    {
        var map = CreateIsometricMap(4, 4, 64, 32, x: 100f, y: 200f);

        map.GetCellWorldPosition(0, 0).ShouldBe(new Vector2(100f, 200f));
        map.GetCellWorldPosition(2, 3).ShouldBe(new Vector2(100f + (2 - 3) * 32f, 200f - (2 + 3) * 16f));
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
}
