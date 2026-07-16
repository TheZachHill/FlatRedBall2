using System.Collections.Generic;
using System.Numerics;
using FlatRedBall2.Tiled;
using MonoGame.Extended.Tilemaps;
using Shouldly;
using Xunit;
using XnaVec2 = Microsoft.Xna.Framework.Vector2;

namespace FlatRedBall2.Tests.Tiled;

public class TileMapGetObjectLayerDataTests
{
    private static Tilemap BuildObjectOnlyTilemap(
        int widthTiles, int heightTiles, int tileSize, TilemapObjectLayer objectLayer, TilemapTileset? tileset = null)
    {
        var tilemap = new Tilemap(
            name: "test",
            width: widthTiles,
            height: heightTiles,
            tileWidth: tileSize,
            tileHeight: tileSize,
            orientation: TilemapOrientation.Orthogonal);
        if (tileset != null)
            tilemap.Tilesets.Add(tileset);
        tilemap.Layers.Add(objectLayer);
        return tilemap;
    }

    [Fact]
    public void GetObjectLayerData_LayerNameCaseInsensitive_FindsLayer()
    {
        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(new TilemapRectangleObject(id: 1, position: new XnaVec2(0, 0), size: new XnaVec2(16, 16)));
        var tileMap = new TileMap(BuildObjectOnlyTilemap(2, 2, 16, layer));

        var entries = tileMap.GetObjectLayerData("WATER");

        entries.Count.ShouldBe(1);
    }

    [Fact]
    public void GetObjectLayerData_NonExistentLayer_ReturnsEmptyList()
    {
        var layer = new TilemapObjectLayer("Water");
        var tileMap = new TileMap(BuildObjectOnlyTilemap(2, 2, 16, layer));

        var entries = tileMap.GetObjectLayerData("DoesNotExist");

        entries.ShouldBeEmpty();
    }

    [Fact]
    public void GetObjectLayerData_PolygonObject_ReturnsWorldSpacePoints()
    {
        // Local triangle (0,0),(16,16),(0,16) at Tiled position (16,0), map at origin.
        // World point = (mapX + Position.X + localX, mapY - (Position.Y + localY)) — no
        // rotation applied, consistent with this method ignoring Rotation for every object type.
        var polyObj = new TilemapPolygonObject(
            id: 1, position: new XnaVec2(16, 0),
            points: new[] { new XnaVec2(0, 0), new XnaVec2(16, 16), new XnaVec2(0, 16) }) { Class = "Zone" };
        polyObj.Properties.SetString("Terrain", "reef");
        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(polyObj);
        var tileMap = new TileMap(BuildObjectOnlyTilemap(2, 2, 16, layer));

        var entries = tileMap.GetObjectLayerData("Water");

        entries.Count.ShouldBe(1);
        entries[0].Points.ShouldNotBeNull();
        entries[0].Points!.Count.ShouldBe(3);
        entries[0].Points![0].ShouldBe(new Vector2(16f, 0f));
        entries[0].Points![1].ShouldBe(new Vector2(32f, -16f));
        entries[0].Points![2].ShouldBe(new Vector2(16f, -16f));
        entries[0].Width.ShouldBe(0f);
        entries[0].Height.ShouldBe(0f);
        entries[0].GlobalId.ShouldBe(0);
        entries[0].Class.ShouldBe("Zone");
        entries[0].Properties["Terrain"].ShouldBe("reef");
    }

    [Fact]
    public void GetObjectLayerData_RectangleObject_PointsIsNull()
    {
        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(new TilemapRectangleObject(id: 1, position: new XnaVec2(0, 0), size: new XnaVec2(16, 16)));
        var tileMap = new TileMap(BuildObjectOnlyTilemap(2, 2, 16, layer));

        var entries = tileMap.GetObjectLayerData("Water");

        entries[0].Points.ShouldBeNull();
    }

    [Fact]
    public void GetObjectLayerData_RectangleObject_ReturnsWorldSpaceTopLeftAndSize()
    {
        // TileMap positioned away from world origin to prove the map offset is folded in —
        // raw Tiled-space coords would be wrong here.
        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(new TilemapRectangleObject(
            id: 1, position: new XnaVec2(10, 20), size: new XnaVec2(30, 40)) { Class = "Zone" });
        var tileMap = new TileMap(BuildObjectOnlyTilemap(4, 4, 16, layer), x: 100f, y: 200f);

        var entries = tileMap.GetObjectLayerData("Water");

        entries.Count.ShouldBe(1);
        entries[0].X.ShouldBe(110f);
        entries[0].Y.ShouldBe(180f);
        entries[0].Width.ShouldBe(30f);
        entries[0].Height.ShouldBe(40f);
        entries[0].Class.ShouldBe("Zone");
        entries[0].GlobalId.ShouldBe(0);
    }

    [Fact]
    public void GetObjectLayerData_RectangleObject_UsesInstancePropertiesOnly()
    {
        var rectObj = new TilemapRectangleObject(id: 1, position: new XnaVec2(0, 0), size: new XnaVec2(16, 16));
        rectObj.Properties.SetString("Zone", "Water");
        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(rectObj);
        var tileMap = new TileMap(BuildObjectOnlyTilemap(2, 2, 16, layer));

        var entries = tileMap.GetObjectLayerData("Water");

        entries[0].Properties["Zone"].ShouldBe("Water");
    }

    [Fact]
    public void GetObjectLayerData_TileObject_ConvertsBottomLeftAnchorToTopLeftAndSetsGlobalId()
    {
        // Tile object at Tiled position (16, 48), size 16x16 — same fixture geometry as
        // TileMapCreateEntitiesTests' Origin.TopLeft case, which expects world (16, -32).
        var tileData = new TilemapTileData(0) { Class = "Coast" };
        var tileset = new TilemapTileset(name: "ts", texture: null!, tileWidth: 16, tileHeight: 16, tileCount: 1, columns: 1);
        tileset.FirstGlobalId = 1;
        tileset.AddTileData(tileData);

        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(new TilemapTileObject(
            id: 1, position: new XnaVec2(16, 48), tile: new TilemapTile(globalId: 1), size: new XnaVec2(16, 16)));
        var tileMap = new TileMap(BuildObjectOnlyTilemap(4, 4, 16, layer, tileset));

        var entries = tileMap.GetObjectLayerData("Water");

        entries[0].X.ShouldBe(16f);
        entries[0].Y.ShouldBe(-32f);
        entries[0].Width.ShouldBe(16f);
        entries[0].Height.ShouldBe(16f);
        entries[0].GlobalId.ShouldBe(1);
    }

    [Fact]
    public void GetObjectLayerData_TileObject_MergesClassAndInstanceProperties()
    {
        var tileData = new TilemapTileData(0) { Class = "Coast" };
        tileData.Properties.SetString("Terrain", "grass");
        tileData.Properties.SetString("Side", "top");
        var tileset = new TilemapTileset(name: "ts", texture: null!, tileWidth: 16, tileHeight: 16, tileCount: 1, columns: 1);
        tileset.FirstGlobalId = 1;
        tileset.AddTileData(tileData);

        var tileObj = new TilemapTileObject(
            id: 1, position: new XnaVec2(0, 16), tile: new TilemapTile(globalId: 1), size: new XnaVec2(16, 16));
        tileObj.Properties.SetString("Terrain", "sand"); // instance overrides class
        var layer = new TilemapObjectLayer("Water");
        layer.AddObject(tileObj);
        var tileMap = new TileMap(BuildObjectOnlyTilemap(4, 4, 16, layer, tileset));

        var entries = tileMap.GetObjectLayerData("Water");

        entries[0].Properties["Terrain"].ShouldBe("sand");
        entries[0].Properties["Side"].ShouldBe("top");
    }
}
