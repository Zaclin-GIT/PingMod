using System;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ES3;
using UnityEngine.UIElements;

namespace REPO_MOD
{
    public class PingObject : MonoBehaviour
    {
        public Vector2 imageSize = new Vector2(100, 100);

        private void Awake()
        {
            this.InitializeCanvas();
        }

        private void InitializeCanvas()
        {
            // Create a new Canvas
            GameObject canvasObj = new GameObject("WorldSpaceCanvas");
            canvasObj.transform.SetParent(this.gameObject.transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Set the canvas position and rotation
            canvasObj.transform.position = this.gameObject.transform.position;

            // Create an Image object as a child of the canvas
            GameObject imageObj = new GameObject("Image");
            imageObj.transform.SetParent(canvasObj.transform);

            // Add the Image component and set its sprite
            UnityEngine.UI.Image image = imageObj.AddComponent<UnityEngine.UI.Image>();
            image.sprite = CreateWhiteSprite(1000, 1000);
            image.rectTransform.sizeDelta = imageSize;

            // Adjust the image position and size within the canvas
            RectTransform rectTransform = image.GetComponent<RectTransform>();
            rectTransform.localPosition = Vector3.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one * 0.01f;  // Scale down for world space
        }

        public Sprite CreateWhiteSprite(int width, int height)
        {
            // Create a new blank texture
            Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);

            // Create a new sprite from the texture
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        }
    }
}