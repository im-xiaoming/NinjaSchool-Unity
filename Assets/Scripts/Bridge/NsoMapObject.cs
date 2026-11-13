using UnityEngine;

namespace NsoBridge
{
    // A Unity object placed at a logical map position. While playing, NsoMapOverlayBridge moves it with the game's
    // camera and zoom and shows it only on its map / zone. Visual only: no collision, no gameplay state.
    // Outside Play mode it sits in the bridge's edit layout (Scene View), so it can be placed without running the game.
    [DisallowMultipleComponent]
    public class NsoMapObject : MonoBehaviour
    {
        [Header("Map placement — logical pixels, Y down")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("mapId")]
        private int _mapId = -1;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("zone")]
        private int _zone = -1;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("position")]
        private Vector2 _position;
        [Header("Presentation")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("depthOffset")]
        private float _depthOffset = -0.01f;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("sortingOrder")]
        private int _sortingOrder = 1;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("scaleWithZoom")]
        private bool _scaleWithZoom = true;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("spritePixelsPerUnit"), Min(0.0001f)]
        private float _spritePixelsPerUnit = 100f;
        [Tooltip("A visual child, never this object or an ancestor.")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("visual")]
        private GameObject _visual;
        public int mapId
        {
            get
            {
                return _mapId;
            }

            set
            {
                _mapId = value;
                appliedVersion = -1;
            }
        }

        public int zone
        {
            get
            {
                return _zone;
            }

            set
            {
                _zone = value;
                appliedVersion = -1;
            }
        }

        public Vector2 position
        {
            get
            {
                return _position;
            }

            set
            {
                _position = value;
                appliedVersion = -1;
            }
        }

        public float depthOffset
        {
            get
            {
                return _depthOffset;
            }

            set
            {
                _depthOffset = value;
                appliedVersion = -1;
            }
        }

        public int sortingOrder
        {
            get
            {
                return _sortingOrder;
            }

            set
            {
                _sortingOrder = value;
                ApplySorting();
            }
        }

        public bool scaleWithZoom
        {
            get
            {
                return _scaleWithZoom;
            }

            set
            {
                _scaleWithZoom = value;
                appliedVersion = -1;
            }
        }

        public float spritePixelsPerUnit
        {
            get
            {
                return _spritePixelsPerUnit;
            }

            set
            {
                _spritePixelsPerUnit = Mathf.Max(0.0001f, value);
                appliedVersion = -1;
            }
        }

        public GameObject visual
        {
            get
            {
                return _visual;
            }

            set
            {
                _visual = value;
            }
        }

        private Renderer[] renderers;
        private Vector3 baseScale = Vector3.one;
        private bool shown = true;
        [System.NonSerialized]
        public int appliedVersion = -1;
        private void OnEnable()
        {
            baseScale = transform.localScale;
            renderers = GetComponentsInChildren<Renderer>(true);
            ApplySorting();
            appliedVersion = -1;
            NsoMapOverlayBridge.Register(this);
        }

        private void OnDisable()
        {
            NsoMapOverlayBridge.Unregister(this);
            if (Application.isPlaying)
                transform.localScale = baseScale;
        }

        private void OnValidate()
        {
            _spritePixelsPerUnit = Mathf.Max(0.0001f, _spritePixelsPerUnit);
            _mapId = Mathf.Max(-1, _mapId);
            _zone = Mathf.Max(-1, _zone);
            appliedVersion = -1;
        }

        public void ApplySorting()
        {
            if (renderers == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].sortingOrder = sortingOrder;
        }

        public bool Matches(NsoMapSnapshot s)
        {
            return s.Ready && (mapId < 0 || mapId == s.MapId) && (zone < 0 || zone == s.Zone);
        }

        // Shows / hides the visual without disabling this GameObject (that would drop its registration).
        public void SetShown(bool value)
        {
            if (shown == value)
                return;
            shown = value;
            if (visual != null && visual != gameObject)
            {
                visual.SetActive(value);
                return;
            }

            if (renderers == null)
                return;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null)
                    renderers[i].enabled = value;
        }

        public void Place(Vector3 world, float worldPerMapPixel)
        {
            ApplySorting();
            transform.position = world;
            if (!scaleWithZoom)
                transform.localScale = baseScale;
            if (scaleWithZoom && worldPerMapPixel > 0f)
            {
                float k = worldPerMapPixel * spritePixelsPerUnit;
                transform.localScale = new Vector3(baseScale.x * k, baseScale.y * k, baseScale.z);
            }
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.9f);
            Vector3 p = transform.position;
            float s = 0.15f;
            Gizmos.DrawLine(p - Vector3.right * s, p + Vector3.right * s);
            Gizmos.DrawLine(p - Vector3.up * s, p + Vector3.up * s);
        }
    }
}
