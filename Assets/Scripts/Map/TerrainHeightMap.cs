using UnityEngine;

public class TerrainHeightMap : MonoBehaviour
{
    [Header("Terrain Settings")]
    [SerializeField] private int heightmapResolution = 513; // Must be 2^n + 1
    [SerializeField] private float heightScale = 100f;
    [SerializeField] private float baseHeight = 50f;
    
    [Header("Noise Settings")]
    [SerializeField] private float noiseScale = 50f;
    [SerializeField] private float lacunarity = 2f;
    [SerializeField] private float persistence = 0.5f;
    [SerializeField] private int octaves = 6;
    [SerializeField] private int seed = 12345;
    
    [Header("Terrain Features")]
    [SerializeField] private float mountainPeakHeight = 200f;
    [SerializeField] private float valleyDepth = -50f;
    [SerializeField] private AnimationCurve heightFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    private TerrainData terrainData;
    private Terrain terrain;

    void Start()
    {
        GenerateTerrainMap();
    }

    public void GenerateTerrainMap()
    {
        terrainData = new TerrainData();
        terrainData.heightmapResolution = heightmapResolution;
        terrainData.size = new Vector3(1000f, heightScale, 1000f);

        // Generate heightmap using Perlin noise
        float[,] heights = GenerateHeightmap();
        terrainData.SetHeights(0, 0, heights);

        // Create terrain object
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);
        terrainObject.name = "FantasyTerrain";
        terrainObject.transform.SetParent(transform);
        terrainObject.transform.localPosition = Vector3.zero;

        terrain = terrainObject.GetComponent<Terrain>();
        
        // Apply monochrome material
        ApplyMonochromeMaterial();
    }

    private float[,] GenerateHeightmap()
    {
        float[,] heightmap = new float[heightmapResolution, heightmapResolution];
        
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000);
            float offsetY = prng.Next(-100000, 100000);
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }

        float maxHeight = 0f;
        float minHeight = float.MaxValue;
        
        // Generate height values
        for (int y = 0; y < heightmapResolution; y++)
        {
            for (int x = 0; x < heightmapResolution; x++)
            {
                float amplitude = 1f;
                float frequency = 1f;
                float height = 0f;
                float maxAmplitude = 0f;

                // Perlin noise octaves
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x / (float)heightmapResolution - 0.5f) * noiseScale * frequency + octaveOffsets[i].x;
                    float sampleY = (y / (float)heightmapResolution - 0.5f) * noiseScale * frequency + octaveOffsets[i].y;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    height += perlinValue * amplitude;

                    maxAmplitude += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                height /= maxAmplitude;
                
                // Apply height falloff at edges
                float distFromEdge = Mathf.Min(
                    x / (float)heightmapResolution,
                    y / (float)heightmapResolution,
                    1 - x / (float)heightmapResolution,
                    1 - y / (float)heightmapResolution
                ) * 2;
                distFromEdge = Mathf.Clamp01(distFromEdge);
                
                float falloff = heightFalloff.Evaluate(distFromEdge);
                height *= falloff;

                // Remap height to desired range
                height = Mathf.Lerp(valleyDepth, mountainPeakHeight, height);
                height = (height + baseHeight) / (baseHeight + heightScale);
                height = Mathf.Clamp01(height);

                heightmap[y, x] = height;

                if (height > maxHeight) maxHeight = height;
                if (height < minHeight) minHeight = height;
            }
        }

        // Normalize heightmap
        if (maxHeight > minHeight)
        {
            for (int y = 0; y < heightmapResolution; y++)
            {
                for (int x = 0; x < heightmapResolution; x++)
                {
                    heightmap[y, x] = (heightmap[y, x] - minHeight) / (maxHeight - minHeight);
                }
            }
        }

        return heightmap;
    }

    private void ApplyMonochromeMaterial()
    {
        if (terrain != null)
        {
            // Create a new monochrome material
            Material monochromeMat = new Material(Shader.Find("Nature/Terrain/Standard"));
            monochromeMat.name = "MonochromeTerrain";
            
            terrain.materialTemplate = monochromeMat;
        }
    }

    public Terrain GetTerrain() => terrain;
    public TerrainData GetTerrainData() => terrainData;
}
