using System;
using System.Collections.Generic;
using UnityEngine;

namespace WKModKit.Loader
{
    /// <summary>
    /// Oyunun her karesinde kayıtlı modlara event pompalayan, sahne geçişlerinde yok edilmeyen host obje.
    /// </summary>
    internal sealed class WkHost : MonoBehaviour
    {
        public static WkHost Instance { get; private set; }

        private readonly List<IModUpdateReceiver> _receivers = new List<IModUpdateReceiver>();
        private readonly List<IModUpdateReceiver> _pendingAdd = new List<IModUpdateReceiver>();
        private readonly List<IModUpdateReceiver> _pendingRemove = new List<IModUpdateReceiver>();

        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            gameObject.hideFlags = HideFlags.HideInHierarchy;
        }

        internal void Register(IModUpdateReceiver receiver)
        {
            if (receiver != null && !_receivers.Contains(receiver) && !_pendingAdd.Contains(receiver))
                _pendingAdd.Add(receiver);
        }

        internal void Unregister(IModUpdateReceiver receiver)
        {
            if (receiver != null)
                _pendingRemove.Add(receiver);
        }

        private void FlushPending()
        {
            for (int i = 0; i < _pendingAdd.Count; i++)
                if (!_receivers.Contains(_pendingAdd[i]))
                    _receivers.Add(_pendingAdd[i]);
            _pendingAdd.Clear();

            for (int i = 0; i < _pendingRemove.Count; i++)
                _receivers.Remove(_pendingRemove[i]);
            _pendingRemove.Clear();
        }

        private void Update()
        {
            FlushPending();
            for (int i = 0; i < _receivers.Count; i++)
            {
                try { _receivers[i].OnModUpdate(); }
                catch (Exception e) { WkLoaderPlugin.Log.LogError($"[host] OnModUpdate hatası ({_receivers[i].GetType().FullName}): {e}"); }
            }
        }

        private void LateUpdate()
        {
            for (int i = 0; i < _receivers.Count; i++)
            {
                try { _receivers[i].OnModLateUpdate(); }
                catch (Exception e) { WkLoaderPlugin.Log.LogError($"[host] OnModLateUpdate hatası ({_receivers[i].GetType().FullName}): {e}"); }
            }
        }

        private void FixedUpdate()
        {
            for (int i = 0; i < _receivers.Count; i++)
            {
                try { _receivers[i].OnModFixedUpdate(); }
                catch (Exception e) { WkLoaderPlugin.Log.LogError($"[host] OnModFixedUpdate hatası ({_receivers[i].GetType().FullName}): {e}"); }
            }
        }

        private void OnGUI()
        {
            // IMGUI: her receiver kendi giriş işlemini yapar; hotkey/eylem kontrolleri
            // Event.current üzerinden okunur (bkz. SandboxMenu.HandleHotkeys).
            for (int i = 0; i < _receivers.Count; i++)
            {
                try { _receivers[i].OnModGUI(); }
                catch (Exception e) { WkLoaderPlugin.Log.LogError($"[host] OnModGUI hatası ({_receivers[i].GetType().FullName}): {e}"); }
            }
        }
    }
}
