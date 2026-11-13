using System.Collections.Generic;
using UnityEngine;

namespace NsoBridge
{
    // Hierarchy object "NSO Map Overlay": keeps every NsoMapObject on its map position while the game runs.
    // Uses the snapshot of the game's last paint (the same frame the game layer texture shows), so overlays scroll
    // and zoom with the map. Disable this component to go back to the plain game view.
    // Outside Play mode this object is the root of the edit layout: map pixel (x, y) sits at (x, -y) / Edit Pixels Per Unit.
    [DefaultExecutionOrder(1000)] // after NsoGameLayer.LateUpdate
    public class NsoMapOverlayBridge : MonoBehaviour
    {
        public static NsoMapOverlayBridge Active;
        private static readonly List<NsoMapObject> objects = new List<NsoMapObject>();
        [Header("Overlay references")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("targetCamera")]
        private Camera _targetCamera;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("gameLayer")]
        private NsoGameLayer _gameLayer;
        [Header("Edit layout — outside Play")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("editPixelsPerUnit"), Min(0.0001f)]
        private float _editPixelsPerUnit = 24f;
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("previewMapSize")]
        private Vector2Int _previewMapSize = new Vector2Int(960, 288);
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("drawGrid")]
        private bool _drawGrid = true;
        public Camera targetCamera
        {
            get
            {
                return _targetCamera;
            }

            set
            {
                _targetCamera = value;
                lastVersion = -1;
            }
        }

        public NsoGameLayer gameLayer
        {
            get
            {
                return _gameLayer;
            }

            set
            {
                _gameLayer = value;
                lastVersion = -1;
            }
        }

        public float editPixelsPerUnit
        {
            get
            {
                return _editPixelsPerUnit;
            }

            set
            {
                _editPixelsPerUnit = Mathf.Max(0.0001f, value);
            }
        }

        public Vector2Int previewMapSize
        {
            get
            {
                return _previewMapSize;
            }

            set
            {
                _previewMapSize = value;
            }
        }

        public bool drawGrid
        {
            get
            {
                return _drawGrid;
            }

            set
            {
                _drawGrid = value;
            }
        }

        private void OnValidate()
        {
            _editPixelsPerUnit = Mathf.Max(0.0001f, _editPixelsPerUnit);
            _previewMapSize.x = Mathf.Max(1, _previewMapSize.x);
            _previewMapSize.y = Mathf.Max(1, _previewMapSize.y);
            lastVersion = -1;
        }

        [Header("Runtime (read-only)")]
        [SerializeField]
        private bool ready;
        [SerializeField]
        private int currentMapId = -1;
        [SerializeField]
        private int currentZone = -1;
        [SerializeField]
        private Vector2 cameraScroll;
        [SerializeField]
        private int zoom;
        private int lastVersion = -1;
        public static IList<NsoMapObject> Objects
        {
            get
            {
                return objects;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            objects.Clear();
            Active = null;
        }

        public static void Register(NsoMapObject o)
        {
            if (o != null && !objects.Contains(o))
                objects.Add(o);
        }

        public static void Unregister(NsoMapObject o)
        {
            objects.Remove(o);
        }

        private void OnEnable()
        {
            Active = this;
            lastVersion = -1;
        }

        private void OnDisable()
        {
            if (Active == this)
                Active = null;
            for (int i = 0; i < objects.Count; i++)
                if (objects[i] != null)
                    objects[i].SetShown(true);
        }

        public Camera ResolveCamera()
        {
            if (targetCamera != null)
                return targetCamera;
            NsoGameLayer layer = ResolveLayer();
            if (layer != null && layer.targetCamera != null)
                return layer.targetCamera;
            return Camera.main;
        }

        public NsoGameLayer ResolveLayer()
        {
            return gameLayer != null ? gameLayer : NsoGameLayer.Active;
        }

        private void LateUpdate()
        {
            NsoMapSnapshot s = NsoMapSnapshot.Current;
            ready = s.Ready;
            currentMapId = s.MapId;
            currentZone = s.Zone;
            cameraScroll = new Vector2(s.CameraX, s.CameraY);
            zoom = s.Zoom;
            Camera cam = ResolveCamera();
            NsoGameLayer layer = ResolveLayer();
            if (cam == null)
                return;
            bool changed = s.Version != lastVersion;
            lastVersion = s.Version;
            float distance = layer != null ? layer.distance : 10f;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                NsoMapObject o = objects[i];
                if (o == null)
                {
                    objects.RemoveAt(i);
                    continue;
                }

                bool show = o.Matches(s);
                o.SetShown(show);
                if (!show || (!changed && o.appliedVersion == s.Version))
                    continue;
                float d = distance + o.depthOffset;
                Vector3 world;
                if (!NsoMapCoordinates.MapToWorld(cam, s, o.position, d, out world))
                    continue;
                o.Place(world, NsoMapCoordinates.WorldPerMapPixel(cam, s, d));
                o.appliedVersion = s.Version;
            }
        }

        public Vector3 EditPosition(Vector2 map)
        {
            return NsoMapCoordinates.MapToEdit(transform, map, editPixelsPerUnit);
        }

        public Vector2 EditToMap(Vector3 world)
        {
            return NsoMapCoordinates.EditToMap(transform, world, editPixelsPerUnit);
        }

        private void OnDrawGizmos()
        {
            if (Application.isPlaying)
                return;
            int w = Mathf.Max(1, previewMapSize.x), h = Mathf.Max(1, previewMapSize.y);
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.9f);
            Vector3 a = EditPosition(Vector2.zero), b = EditPosition(new Vector2(w, 0)), c = EditPosition(new Vector2(w, h)), d = EditPosition(new Vector2(0, h));
            Gizmos.DrawLine(a, b);
            Gizmos.DrawLine(b, c);
            Gizmos.DrawLine(c, d);
            Gizmos.DrawLine(d, a);
            if (!drawGrid)
                return;
            const int tile = 24, maxLines = 200; // keeps large maps cheap to draw
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.2f);
            int step = tile * Mathf.Max(1, Mathf.CeilToInt((w / tile + h / tile) / (float)maxLines));
            for (int x = step; x < w; x += step)
                Gizmos.DrawLine(EditPosition(new Vector2(x, 0)), EditPosition(new Vector2(x, h)));
            for (int y = step; y < h; y += step)
                Gizmos.DrawLine(EditPosition(new Vector2(0, y)), EditPosition(new Vector2(w, y)));
        }
    }
}
