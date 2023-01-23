/* ----------------------------------------------------------------------------
MIT License

Copyright (c) 2018-2023 Christopher Whitley

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
---------------------------------------------------------------------------- */

using Microsoft.Xna.Framework.Graphics;
using MonoGame.Aseprite.AsepriteTypes;

namespace MonoGame.Aseprite.Processors;

/// <summary>
///     Defines a processor that processes <see cref="AsepriteTilemapLayer"/>, <see cref="AsepriteTilemapCel"/>, and
///     <see cref="AsepriteTileset"/> elements from a single <see cref="AsepriteFrame"/> in an
///     <see cref="AsepriteFile"/> as a <see cref="Tilemap"/>.
/// </summary>
public static class TilemapProcessor
{

    /// <summary>
    ///     Processes <see cref="AsepriteTilemapLayer"/>, <see cref="AsepriteTilemapCel"/>, and
    ///     <see cref="AsepriteTileset"/> elements from a single <see cref="AsepriteFrame"/> in an
    ///     <see cref="AsepriteFile"/> as a <see cref="Tilemap"/>
    /// </summary>
    /// <param name="device">
    ///     The instance of the <see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/> class used to create the
    ///     graphical resources.
    /// </param>
    /// <param name="file">
    ///     The instance of the <see cref="AsepriteFile"/> to process the elements from.
    /// </param>
    /// <param name="configuration">
    ///     An instance of the <see cref="TilemapProcessorConfiguration"/> class that defines configurations for
    ///     this process.
    /// </param>
    /// <returns>
    ///     The instance of the <see cref="Tilemap"/> class that is created by this method.
    /// </returns>
    public static Tilemap Process(GraphicsDevice device, AsepriteFile file, TilemapProcessorConfiguration configuration)
    {
        RawTilemap rawTilemap = CreateRawTilemap(file, configuration);
        return CreateTilemap(device, rawTilemap);
    }

    /// <summary>
    ///     Processes <see cref="AsepriteTilemapLayer"/>, <see cref="AsepriteTilemapCel"/>, and
    ///     <see cref="AsepriteTileset"/> elements from a single <see cref="AsepriteFrame"/> in an
    ///     <see cref="AsepriteFile"/> as a <see cref="Tilemap"/>
    /// </summary>
    /// <param name="device">
    ///     The instance of the <see cref="Microsoft.Xna.Framework.Graphics.GraphicsDevice"/> class used to create the
    ///     graphical resources.
    /// </param>
    /// <param name="file">
    ///     The instance of the <see cref="AsepriteFile"/> to process the elements from.
    /// </param>
    /// <param name="configure">
    ///     An action method used to build the instance of the <see cref="TilemapProcessorConfiguration"/> class that
    ///     defines the configurations for this process.
    /// </param>
    /// <returns>
    ///     The instance of the <see cref="Tilemap"/> class that is created by this method.
    /// </returns>
    public static Tilemap Process(GraphicsDevice device, AsepriteFile file, Action<TilemapProcessorConfiguration> configure)
    {
        TilemapProcessorConfiguration configuration = new();
        configure(configuration);
        return Process(device, file, configuration);
    }

    internal static Tilemap CreateTilemap(GraphicsDevice device, RawTilemap rawTilemap)
    {
        Tilemap tilemap = new(rawTilemap.Name);

        Dictionary<int, Tileset> tilesetLookup = new();
        foreach (RawTileset rawTileset in rawTilemap.Tilesets)
        {
            Tileset tileset = TilesetProcessor.CreateTileset(device, rawTileset);
            tilesetLookup.Add(rawTileset.ID, tileset);
        }

        for (int i = 0; i < rawTilemap.Layers.Length; i++)
        {
            RawTilemapLayer layer = rawTilemap.Layers[i];
            tilemap.CreateLayer(layer.Name, tilesetLookup[layer.TilesetID], layer.Columns, layer.Rows, layer.Offset.ToVector2());
        }

        return tilemap;
    }

    internal static RawTilemap CreateRawTilemap(AsepriteFile file, TilemapProcessorConfiguration configuration)
    {
        AsepriteFrame frame = file.Frames[configuration.FrameIndex];
        RawTilemapLayer[] layers = GenerateLayers(frame.Cels, configuration);
        RawTileset[] tilesets = GetTilesetsForLayers(file, layers);
        return new(file.Name, layers, tilesets);

    }

    private static RawTilemapLayer[] GenerateLayers(ReadOnlySpan<AsepriteCel> cels, TilemapProcessorConfiguration configuration)
    {
        List<RawTilemapLayer> layers = new();

        for (int i = 0; i < cels.Length; i++)
        {
            if (cels[i] is AsepriteTilemapCel cel && (cel.Layer.IsVisible || !configuration.OnlyVisibleLayers))
            {
                //  TODO: Can the TilesetID be added to the Tileset itself instead of
                //        having to get the layer as then pulling it from the layer
                //        since the cel already has access to the Tileset???
                int tilesetID = cel.Tileset.ID;

                RawTile[] tiles = new RawTile[cel.TileCount];
                CopyAsepriteTileToRawTile(cel.Tiles, tiles);
                RawTilemapLayer layer = new(cel.Layer.Name, tilesetID, cel.Columns, cel.Rows, tiles, cel.Position);
                layers.Add(layer);
            }
        }

        return layers.ToArray();
    }

    private static void CopyAsepriteTileToRawTile(ReadOnlySpan<AsepriteTile> from, Span<RawTile> to)
    {
        for (int i = 0; i < from.Length; i++)
        {
            AsepriteTile tile = from[i];
            bool xFlip = tile.XFlip != 0;
            bool yFlip = tile.YFlip != 0;
            to[i] = new RawTile(tile.TilesetTileID, xFlip, yFlip, tile.Rotation);
        }
    }

    private static RawTileset[] GetTilesetsForLayers(AsepriteFile file, ReadOnlySpan<RawTilemapLayer> layers)
    {
        List<RawTileset> tilesets = new();
        HashSet<int> ids = new();

        for (int i = 0; i < layers.Length; i++)
        {
            RawTilemapLayer layer = layers[i];

            if (ids.Contains(layer.TilesetID))
            {
                continue;
            }

            RawTileset tileset = TilesetProcessor.GetRawTileset(file.Tilesets[layer.TilesetID]);
            tilesets.Add(tileset);
        }

        return tilesets.ToArray();
    }
}
