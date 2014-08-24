﻿using UnityEngine;
using System.Collections;

public enum MapObjectDepth
{
    Tile = 0,
    Shadow = 1,
    Feature = 2,
}

public abstract class MapBuilder : MonoBehaviour 
{
    public int Width = 16;
    public int Height = 16;
    public float TileWidth = 1.0f;
    public float TileHeight = 0.8f;
    public float NoiseFrequency = 6.0f;

    public int[] HeightField { get; private set; }

    private AssetInfo shadowEastAssetInfo;
    private AssetInfo shadowNorthAssetInfo;
    private AssetInfo shadowNorthEastAssetInfo;
    private AssetInfo shadowNorthWestAssetInfo;
    private AssetInfo shadowSouthAssetInfo;
    private AssetInfo shadowSouthEastAssetInfo;
    private AssetInfo shadowSouthWestAssetInfo;
    private AssetInfo shadowWestAssetInfo;

    protected ResourceManager resourceManager;
    protected GameController gameController;

    private void Awake()
    {
        gameController = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        resourceManager = gameController.GetComponent<ResourceManager>();
        gameController.OnFinishedLoading += OnFinishedLoadingHandler;
    }

    private void OnFinishedLoadingHandler()
    {
        shadowEastAssetInfo         = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_EAST");
        shadowNorthAssetInfo        = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_NORTH");
        shadowNorthEastAssetInfo    = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_NORTH_EAST");
        shadowNorthWestAssetInfo    = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_NORTH_WEST");
        shadowSouthAssetInfo        = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_SOUTH");
        shadowSouthEastAssetInfo    = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_SOUTH_EAST");
        shadowSouthWestAssetInfo    = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_SOUTH_WEST");
        shadowWestAssetInfo         = Database.Instance.GetEntry<AssetInfo>("ASSET_SHADOW_WEST");

        InitHeights();
        InitMapObjects();
        InitShadows();
    }

    private void InitHeights()
    {
        HeightField = new int[Width * Height];

        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                // Compute a position in noise space
                float noiseX = (float)x / Width * NoiseFrequency;
                float noiseY = (float)y / Height * NoiseFrequency;

                // Get Perlin noise value
                float noise = Mathf.PerlinNoise(noiseX, noiseY);

                // Convert to height and store
                HeightField[y * Width + x] = Mathf.RoundToInt(noise * 3.0f);
            }
        }
    }

    public Vector2 GetTileLowerLeft(int x, int y)
    {
        return new Vector2(x * TileWidth, y * TileHeight);
    }

    public Vector2 GetTileLowerRight(int x, int y)
    {
        return new Vector2(x * TileWidth + TileWidth, y * TileHeight);
    }

    public Vector2 GetTileUpperLeft(int x, int y)
    {
        return new Vector2(x * TileWidth, y * TileHeight + TileHeight);
    }

    public Vector2 GetTileUpperRight(int x, int y)
    {
        return new Vector2(x * TileWidth + TileWidth, y * TileHeight + TileHeight);
    }

    public Vector2 GetLowerCenter(int x, int y)
    {
        return new Vector2(x * TileWidth + TileWidth * 0.5f, y * TileHeight);
    }

    public Vector2 GetUpperCenter(int x, int y)
    {
        return new Vector2(x * TileWidth + TileWidth * 0.5f, y * TileHeight + TileHeight);
    }

    public Vector2 GetCenter(int x, int y)
    {
        return new Vector2(x * TileWidth + TileWidth * 0.5f, y * TileHeight + TileHeight * 0.5f);
    }

    public bool IsValid(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public int GetHeight(int x, int y)
    {
        if (HeightField == null)
        {
            throw new System.Exception("Height field has not been initialized!");
        }

        if (!IsValid(x, y))
        {
            return -1;
        }

        return HeightField[y * Width + x];
    }

    protected void InstantiateMapPrefab(int x, int y, MapObjectDepth depth, GameObject prefab)
    {
        // Select a tile from the load strings array and instantiate it
        Vector2 offset = new Vector2(0.0f, HeightField[y * Width + x] * 0.4f);
        GameObject go = Instantiate(prefab, GetTileLowerLeft(x, y) + offset, Quaternion.identity) as GameObject;

        // Set the tile sprite's render layer and sorting order
        SpriteRenderer[] renderers = go.GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.sortingLayerName = "Tiles";
            renderer.sortingOrder = (Height - y) * 10 + (int)depth;
        }
    }

    private void InitShadows()
    {
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                int height = GetHeight(x, y);
                int tempX = x;
                int tempY = y;

                bool[] adjacent = 
                {
                    IsValid(x, y + 1) && GetHeight(x, y + 1) > height,              // North
                    IsValid(x + 1, y + 1) && GetHeight(x + 1, y + 1) > height,      // North East
                    IsValid(x + 1, y) && GetHeight(x + 1, y) > height,              // East
                    IsValid(x + 1, y - 1) && GetHeight(x + 1, y - 1) > height,      // South East
                    IsValid(x, y - 1) && GetHeight(x, y - 1) > height,              // South
                    IsValid(x - 1, y - 1) && GetHeight(x - 1, y - 1) > height,      // South West
                    IsValid(x - 1, y) && GetHeight(x - 1, y) > height,              // West
                    IsValid(x - 1, y + 1) && GetHeight(x - 1, y + 1) > height,      // North West
                };

                // Check for shadow to the north
                if (adjacent[0])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowNorthAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the east
                if (adjacent[2])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowEastAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the south
                if (adjacent[4])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowSouthAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the west
                if (adjacent[6])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowWestAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the north east
                if (!adjacent[0] && adjacent[1] && !adjacent[2])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowNorthEastAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the south east
                if (!adjacent[2] && adjacent[3] && !adjacent[4])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowSouthEastAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the south west
                if (!adjacent[4] && adjacent[5] && !adjacent[6])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowSouthWestAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }

                // Check for shadow to the north west
                if (!adjacent[6] && adjacent[7] && !adjacent[0])
                {
                    resourceManager.LoadAssetAsync<GameObject>(shadowNorthWestAssetInfo, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Shadow, prefab);
                    });
                }
            }
        }
    }

    protected abstract void InitMapObjects();
}
