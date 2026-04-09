using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using JungleVoodoo.Core;
using JungleVoodoo.Networking;

namespace JungleVoodoo.Map
{
    /// <summary>
    /// Manages the Cursed Wilds world map.
    /// - Renders tile grid by loading prefabs via Addressables
    /// - Handles pan / pinch-zoom via touch input
    /// - Loads nearby tile data from PlayFab in chunks
    /// - Fires OnTileSelected when the player taps a tile
    ///
    /// Register this with the ServiceLocator via a MonoBehaviour wrapper that
    /// calls ServiceLocator.Instance.Register(GetComponent&lt;WorldMapManager&gt;()).
    /// </summary>
    public class WorldMapManager : MonoBehaviour
    {
        [Header("Grid Settings")]
        [SerializeField] private int   _viewRadius      = 10;  // tiles loaded around camera
        [SerializeField] private float _tileSize        = 1f;

        [Header("Camera Control")]
        [SerializeField] private float _zoomMin         = 2f;
        [SerializeField] private float _zoomMax         = 20f;
        [SerializeField] private float _panSpeed        = 0.02f;

        public event Action<TileData> OnTileSelected;

        private readonly Dictionary<Vector2Int, TileData>     _tileCache    = new();
        private readonly Dictionary<Vector2Int, GameObject>   _tileObjects  = new();

        private Camera _cam;
        private Vector2Int _lastLoadedCenter;
        private PlayFabManager _playFab;

        private void Awake()
        {
            _cam     = Camera.main;
            _playFab = ServiceLocator.Instance.Get<PlayFabManager>();

            ServiceLocator.Instance.Register(this);
        }

        private void Start()
        {
            LoadChunkAround(Vector2Int.zero);
        }

        // ── Input ─────────────────────────────────────────────────────────────

        private Vector2 _lastTouchPos;
        private bool    _wasPinching;
        private float   _pinchStartDist;
        private float   _pinchStartSize;

        private void Update()
        {
            HandlePanAndZoom();
            CheckChunkReload();
        }

        private void HandlePanAndZoom()
        {
            if (Input.touchCount == 1)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    var delta = touch.deltaPosition * _panSpeed;
                    _cam.transform.Translate(-delta.x, -delta.y, 0);
                }
                if (touch.phase == TouchPhase.Ended)
                    TrySelectTile(touch.position);
            }
            else if (Input.touchCount == 2)
            {
                var t0 = Input.GetTouch(0);
                var t1 = Input.GetTouch(1);
                float currentDist = Vector2.Distance(t0.position, t1.position);

                if (!_wasPinching)
                {
                    _pinchStartDist = currentDist;
                    _pinchStartSize = _cam.orthographicSize;
                    _wasPinching    = true;
                }
                else
                {
                    float scale = _pinchStartDist / currentDist;
                    _cam.orthographicSize = Mathf.Clamp(_pinchStartSize * scale, _zoomMin, _zoomMax);
                }
            }
            else
            {
                _wasPinching = false;
            }

#if UNITY_EDITOR
            // Mouse pan in editor
            if (Input.GetMouseButton(1))
            {
                var delta = new Vector3(-Input.GetAxis("Mouse X"), -Input.GetAxis("Mouse Y"), 0) * 0.1f;
                _cam.transform.Translate(delta);
            }
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - Input.GetAxis("Mouse ScrollWheel") * 2f,
                _zoomMin, _zoomMax);

            if (Input.GetMouseButtonDown(0))
                TrySelectTile(Input.mousePosition);
#endif
        }

        private void TrySelectTile(Vector2 screenPos)
        {
            var worldPos = _cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 0));
            var grid     = new Vector2Int(
                Mathf.RoundToInt(worldPos.x / _tileSize),
                Mathf.RoundToInt(worldPos.y / _tileSize));

            if (_tileCache.TryGetValue(grid, out var tile))
                OnTileSelected?.Invoke(tile);
        }

        // ── Chunk Loading ─────────────────────────────────────────────────────

        private void CheckChunkReload()
        {
            var camPos = _cam.transform.position;
            var center = new Vector2Int(
                Mathf.RoundToInt(camPos.x / _tileSize),
                Mathf.RoundToInt(camPos.y / _tileSize));

            if (center != _lastLoadedCenter)
                LoadChunkAround(center);
        }

        private void LoadChunkAround(Vector2Int center)
        {
            _lastLoadedCenter = center;
            // TODO: query PlayFab GetUserData / GetTitleData with tile chunk key
            // For now, generate placeholder tiles
            for (int x = center.x - _viewRadius; x <= center.x + _viewRadius; x++)
            {
                for (int y = center.y - _viewRadius; y <= center.y + _viewRadius; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (_tileCache.ContainsKey(pos)) continue;

                    var tile = new TileData { GridX = x, GridY = y, Type = TileType.Jungle };
                    _tileCache[pos] = tile;

                    SpawnTileObject(tile);
                }
            }
        }

        private void SpawnTileObject(TileData tile)
        {
            // Tile prefabs are loaded by Addressable key based on tile type
            var addressKey = $"Prefabs/Tiles/{tile.Type}";
            Addressables.InstantiateAsync(addressKey).Completed += handle =>
            {
                if (handle.Result == null) return;
                var go = handle.Result;
                go.transform.position = new Vector3(tile.GridX * _tileSize, tile.GridY * _tileSize, 0);
                go.transform.SetParent(transform);
                _tileObjects[tile.GridPosition] = go;
            };
        }

        public TileData GetTile(Vector2Int gridPos) =>
            _tileCache.TryGetValue(gridPos, out var t) ? t : null;
    }
}
