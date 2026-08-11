using UnityEngine;

public static class MiniGameGraphics
{
    public static readonly Color Background = new Color(0.08f, 0.10f, 0.14f, 1f);
    public static readonly Color TileBase = new Color(0.16f, 0.18f, 0.24f, 1f);
    public static readonly Color PipeColor = new Color(0.35f, 0.65f, 1f, 1f);
    public static readonly Color SourceColor = new Color(0.2f, 0.85f, 0.35f, 1f);
    public static readonly Color SinkColor = new Color(0.95f, 0.25f, 0.25f, 1f);
    public static readonly Color HintGlow = new Color(1f, 0.85f, 0.2f, 1f);
    public static readonly Color BackColor = new Color(0.22f, 0.24f, 0.32f, 1f);
    public static readonly Color MatchedTint = new Color(0.55f, 0.9f, 0.55f, 1f);

    public static readonly Color[] ButtonColors =
    {
        new Color(0.85f, 0.2f, 0.2f, 1f),
        new Color(0.25f, 0.4f, 0.9f, 1f),
        new Color(0.25f, 0.75f, 0.3f, 1f),
        new Color(0.95f, 0.85f, 0.2f, 1f),
        new Color(0.55f, 0.3f, 0.85f, 1f),
        new Color(0.95f, 0.55f, 0.15f, 1f)
    };

    public static Sprite PipeSprite(int openMask)
    {
        return MakeSprite(64, mask => PaintPipe(mask, openMask));
    }

    public static Sprite ShapeSprite(int shapeIndex, Color color)
    {
        return MakeSprite(128, mask => PaintShape(mask, shapeIndex, color));
    }

    public static Sprite CardBackSprite()
    {
        return MakeSprite(128, mask =>
        {
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                    mask[y * 128 + x] = TileBase;
            }
        });
    }

    private static void PaintPipe(Color32[] mask, int openMask)
    {
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
                mask[y * 64 + x] = TileBase;
        }

        var color = PipeColor;
        DrawHorizontal(mask, 64, color, 28, 35);
        DrawVertical(mask, 64, color, 28, 35);
        if ((openMask & PipeDirections.Up) != 0) DrawVertical(mask, 64, color, 0, 28);
        if ((openMask & PipeDirections.Down) != 0) DrawVertical(mask, 64, color, 35, 64);
        if ((openMask & PipeDirections.Left) != 0) DrawHorizontal(mask, 64, color, 0, 28);
        if ((openMask & PipeDirections.Right) != 0) DrawHorizontal(mask, 64, color, 35, 64);
    }

    private static void DrawHorizontal(Color32[] mask, int size, Color color, int start, int end)
    {
        for (int x = start; x < end; x++)
        {
            for (int y = 28; y < 36; y++)
                mask[y * size + x] = color;
        }
    }

    private static void DrawVertical(Color32[] mask, int size, Color color, int start, int end)
    {
        for (int y = start; y < end; y++)
        {
            for (int x = 28; x < 36; x++)
                mask[y * size + x] = color;
        }
    }

    private static void PaintShape(Color32[] mask, int shapeIndex, Color color)
    {
        for (int y = 0; y < 128; y++)
        {
            for (int x = 0; x < 128; x++)
                mask[y * 128 + x] = new Color32(0, 0, 0, 0);
        }

        for (int y = 16; y < 112; y++)
        {
            for (int x = 16; x < 112; x++)
            {
                float nx = (x - 64f) / 56f;
                float ny = (y - 64f) / 56f;
                if (InShape(nx, ny, shapeIndex)) mask[y * 128 + x] = color;
            }
        }
    }

    private static bool InShape(float nx, float ny, int shapeIndex)
    {
        switch (shapeIndex)
        {
            case 0:
                return nx * nx + ny * ny < 1f;
            case 1:
                return ny >= -0.35f && ny <= 0.7f && Mathf.Abs(nx) <= 0.6f * (0.7f - ny) / 1.05f;
            case 2:
                return Mathf.Abs(nx) < 0.6f && Mathf.Abs(ny) < 0.6f;
            case 3:
                return Mathf.Abs(nx) + Mathf.Abs(ny) < 0.7f;
            case 4:
                return Star(nx, ny);
            default:
                return Mathf.Abs(nx) < 0.7f && Mathf.Abs(ny) < 0.7f && Mathf.Abs(nx) * 0.577f + Mathf.Abs(ny) < 0.85f;
        }
    }

    private static bool Star(float nx, float ny)
    {
        float angle = Mathf.Atan2(ny, nx);
        float radius = Mathf.Sqrt(nx * nx + ny * ny);
        float spikes = 5;
        float spikeLength = 0.9f;
        float valley = 0.4f;
        float theta = Mathf.Abs(Mathf.Repeat(angle * spikes / (2f * Mathf.PI), 1f) - 0.5f) * 2f;
        float outer = spikeLength - (spikeLength - valley) * theta;
        return radius < outer;
    }

    public static Sprite MakeSprite(int size, System.Action<Color32[]> painter)
    {
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var mask = new Color32[size * size];
        painter(mask);
        texture.SetPixels32(mask);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public static AudioClip SineClip(float frequency)
    {
        const int sampleRate = 44100;
        const float duration = 0.25f;
        int samples = (int)(sampleRate * duration);
        var data = new float[samples];
        for (int i = 0; i < samples; i++)
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * 0.6f;

        var clip = AudioClip.Create("Tone" + frequency, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}