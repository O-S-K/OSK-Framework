using System.Collections.Generic;
using UnityEngine;

namespace OSK
{
    public class TextureUtils : MonoBehaviour
    {
        #region Example Usage

        // Split texture:
        // List<Texture2D> parts = TextureUtils.SplitTexture(sourceTexture, 2, 2);
        //
        // Make readable copy:
        // Texture2D readable = TextureUtils.CopyToReadable(sourceTexture);
        //
        // Create sprite:
        // Sprite sprite = TextureUtils.ToSprite(texture, 100f);
        //
        // Resize or fill:
        // Texture2D resized = TextureUtils.Resize(texture, 256, 256);
        // Texture2D solid = TextureUtils.CreateSolidTexture(32, 32, Color.red);
        //
        // Encode:
        // byte[] pngBytes = TextureUtils.EncodeToPNG(texture);

        #endregion

        #region Split

        // Splits a texture into a 2x2 grid.
        public List<Texture2D> SplitTexture(Texture2D sourceTexture)
        {
            return SplitTexture(sourceTexture, 2, 2);
        }

        // Splits a texture into a grid.
        public static List<Texture2D> SplitTexture(Texture2D sourceTexture, int columns, int rows)
        {
            List<Texture2D> textures = new List<Texture2D>();
            if (sourceTexture == null || columns <= 0 || rows <= 0)
            {
                return textures;
            }

            Texture2D readableSource = EnsureReadable(sourceTexture);
            int width = readableSource.width / columns;
            int height = readableSource.height / rows;
            if (width <= 0 || height <= 0)
            {
                return textures;
            }

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < columns; x++)
                {
                    RectInt rect = new RectInt(x * width, y * height, width, height);
                    textures.Add(Crop(readableSource, rect));
                }
            }

            return textures;
        }

        // Crops a texture by pixel rect.
        public static Texture2D Crop(Texture2D sourceTexture, RectInt rect)
        {
            if (sourceTexture == null || rect.width <= 0 || rect.height <= 0)
            {
                return null;
            }

            Texture2D readableSource = EnsureReadable(sourceTexture);
            rect.x = Mathf.Clamp(rect.x, 0, readableSource.width - 1);
            rect.y = Mathf.Clamp(rect.y, 0, readableSource.height - 1);
            rect.width = Mathf.Clamp(rect.width, 1, readableSource.width - rect.x);
            rect.height = Mathf.Clamp(rect.height, 1, readableSource.height - rect.y);

            Texture2D texture = new Texture2D(rect.width, rect.height, TextureFormat.RGBA32, false);
            Color[] pixels = readableSource.GetPixels(rect.x, rect.y, rect.width, rect.height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        #endregion

        #region Readable Copy

        // Returns source texture when readable, otherwise creates a readable copy.
        public static Texture2D EnsureReadable(Texture2D sourceTexture)
        {
            if (sourceTexture == null)
            {
                return null;
            }

            if (IsReadable(sourceTexture))
            {
                return sourceTexture;
            }

            return CopyToReadable(sourceTexture);
        }

        // Checks if a Texture2D can be read with GetPixels.
        public static bool IsReadable(Texture2D texture)
        {
            if (texture == null)
            {
                return false;
            }

            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Creates a readable copy of a texture through a temporary RenderTexture.
        public static Texture2D CopyToReadable(Texture sourceTexture, TextureFormat format = TextureFormat.RGBA32, bool mipChain = false)
        {
            if (sourceTexture == null)
            {
                return null;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(
                sourceTexture.width,
                sourceTexture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);

            Graphics.Blit(sourceTexture, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D readableTexture = new Texture2D(sourceTexture.width, sourceTexture.height, format, mipChain);
            readableTexture.ReadPixels(new Rect(0, 0, sourceTexture.width, sourceTexture.height), 0, 0);
            readableTexture.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return readableTexture;
        }

        #endregion

        #region Create

        // Creates a solid color texture.
        public static Texture2D CreateSolidTexture(int width, int height, Color color)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] colors = new Color[width * height];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = color;
            }

            texture.SetPixels(colors);
            texture.Apply();
            return texture;
        }

        // Creates a Texture2D from raw bytes.
        public static Texture2D FromImageBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }

            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }

        #endregion

        #region Resize

        // Resizes a texture to target width and height.
        public static Texture2D Resize(Texture sourceTexture, int width, int height, TextureFormat format = TextureFormat.RGBA32)
        {
            if (sourceTexture == null)
            {
                return null;
            }

            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);

            RenderTexture previous = RenderTexture.active;
            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.Default);
            Graphics.Blit(sourceTexture, renderTexture);
            RenderTexture.active = renderTexture;

            Texture2D resized = new Texture2D(width, height, format, false);
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);
            return resized;
        }

        #endregion

        #region Sprite

        // Creates a sprite from the full texture.
        public static Sprite ToSprite(Texture2D texture, float pixelsPerUnit = 100f)
        {
            if (texture == null)
            {
                return null;
            }

            return ToSprite(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        // Creates a sprite from texture rect, pivot, and pixels per unit.
        public static Sprite ToSprite(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit = 100f)
        {
            if (texture == null)
            {
                return null;
            }

            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
        }

        #endregion

        #region Encode

        // Encodes a texture to PNG bytes.
        public static byte[] EncodeToPNG(Texture2D texture)
        {
            Texture2D readableTexture = EnsureReadable(texture);
            return readableTexture != null ? readableTexture.EncodeToPNG() : null;
        }

        // Encodes a texture to JPG bytes.
        public static byte[] EncodeToJPG(Texture2D texture, int quality = 75)
        {
            Texture2D readableTexture = EnsureReadable(texture);
            quality = Mathf.Clamp(quality, 1, 100);
            return readableTexture != null ? readableTexture.EncodeToJPG(quality) : null;
        }

        #endregion
    }
}
