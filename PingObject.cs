using System;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace REPO_MOD
{
    // Token: 0x02000006 RID: 6
    public class PingObject : MonoBehaviour
    {
        // Token: 0x06000012 RID: 18 RVA: 0x0000260F File Offset: 0x0000080F
        private void Awake()
        {
            this.InitializeCanvas();
        }

        // Token: 0x06000015 RID: 21 RVA: 0x000026A9 File Offset: 0x000008A9
        private void DestroyPingObject()
        {
            this.canvas.gameObject.SetActive(false);
        }

        // Token: 0x06000016 RID: 22 RVA: 0x000026C8 File Offset: 0x000008C8
        private void InitializeCanvas()
        {
            GameObject gameObject = new GameObject("PingCanvas");
            this.canvas = gameObject.AddComponent<Canvas>();
            this.canvas.renderMode = (RenderMode)2;
            this.canvas.transform.SetParent(base.transform);
            this.canvas.transform.localPosition = this.worldOffset;
            RectTransform component = this.canvas.GetComponent<RectTransform>();
            component.sizeDelta = this.size;
            component.localScale = new Vector3(0.03f, 0.03f, 0.03f);
            gameObject.AddComponent<GraphicRaycaster>();
            this.background = this.CreatePingElement(base.transform, this.backgroundColor, "Background");
            this.foreground.type = (Image.Type)3;
            this.foreground.fillMethod = 0;
            this.foreground.fillOrigin = 0;
            PingObject.Billboard billboard = gameObject.AddComponent<PingObject.Billboard>();
            billboard.offset = this.worldOffset;
        }

        private Image CreatePingElement(Transform pos, Color color, string name)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(this.gameObject.transform);
            Texture2D texture2D = new Texture2D(1, 1);
            texture2D.SetPixel(0, 0, Color.white);
            texture2D.Apply();
            Image image = gameObject.AddComponent<Image>();
            image.sprite = Sprite.Create(texture2D, new Rect(0f, 0f, 1f, 1f), Vector2.zero);
            image.color = color;
            RectTransform component = gameObject.GetComponent<RectTransform>();
            component.anchorMin = Vector2.zero;
            component.anchorMax = Vector2.one;
            component.offsetMin = Vector2.zero;
            component.offsetMax = Vector2.zero;
            return image;
        }

        private void OnDestroy()
        {
            bool flag = this.canvas != null;
            if (flag)
            {
                UnityEngine.Object.Destroy(this.canvas.gameObject);
            }
        }

        public Vector3 worldOffset = new Vector3(0f, 2.5f, 0f);
        public Vector2 size = new Vector2(20f, 2.5f);
        public Color backgroundColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        public Color healthColor = Color.red;
        public Canvas canvas;
        private Image foreground;
        private Image background;

        public class Billboard : MonoBehaviour
        {
            private void Start()
            {
                this.mainCamera = Camera.main;
            }

            private void LateUpdate()
            {
                bool flag = this.mainCamera != null;
                if (flag)
                {
                    base.transform.rotation = Quaternion.Euler(this.mainCamera.transform.eulerAngles.x, this.mainCamera.transform.eulerAngles.y, 0f);
                    base.transform.position = base.transform.parent.position + this.offset;
                }
            }

            public Vector3 offset;
            private Camera mainCamera;
        }
    }
}