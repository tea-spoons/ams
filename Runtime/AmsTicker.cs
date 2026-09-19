namespace TeaSpoons.AMS
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.LowLevel;
    using UnityEngine.PlayerLoop;
    
    /// <summary>
    /// Manages automatic ticking of AMS proxies using Unity's PlayerLoop.
    /// </summary>
    public class AmsTicker
    {
        private AmsTickMode tickMode = AmsTickMode.Manual;
        private bool isInjected;

        private readonly List<AmsProxy> rootProxies = new();

        private static bool isApplicationQuitting;
        private static AmsTicker instance;

        public static AmsTicker Instance
        {
            get
            {
                if (instance != null || isApplicationQuitting) return instance;
                
                instance = new AmsTicker();
                instance.Initialize();
                return instance;
            }
        }

        /// <summary>
        /// Current tick mode.
        /// Changing this automatically updates the PlayerLoop injection.
        /// </summary>
        public AmsTickMode TickMode
        {
            get => tickMode;
            set
            {
                if (tickMode == value) return;
                
                var wasManual = tickMode == AmsTickMode.Manual;
                var isManual = value == AmsTickMode.Manual;
                
                tickMode = value;

                switch (wasManual)
                {
                    case true when !isManual:
                        InjectIntoPlayerLoop();
                        break;
                    case false when isManual:
                        RemoveFromPlayerLoop();
                        break;
                    default:
                    {
                        if (!isManual)
                        {
                            RemoveFromPlayerLoop();
                            InjectIntoPlayerLoop();
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Number of registered root proxies.
        /// </summary>
        public int ProxyCount => rootProxies.Count;

        private void OnApplicationQuitting()
        {
            isApplicationQuitting = true;
            RemoveFromPlayerLoop();
        }
        
        private void Initialize()
        {
            Application.quitting += OnApplicationQuitting;
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged += (state) =>
            {
                if (state != UnityEditor.PlayModeStateChange.ExitingPlayMode) return;
                
                RemoveFromPlayerLoop();
                instance = null;
            };
#endif
        }

        /// <summary>
        /// Registers a root proxy for automatic ticking.
        /// Only register root proxies - children are ticked automatically.
        /// </summary>
        public void Register(AmsProxy rootProxy)
        {
            if (rootProxy == null) return;

            if (!rootProxy.IsRoot)
            {
                Logs.Warning("Only register root proxies with AmsTicker. Children are ticked automatically.");
                return;
            }

            if (!rootProxies.Contains(rootProxy))
            {
                rootProxies.Add(rootProxy);
            }
        }

        /// <summary>
        /// Unregisters a proxy from automatic ticking.
        /// </summary>
        public void Unregister(AmsProxy proxy)
        {
            if (proxy == null) return;
            
            rootProxies.Remove(proxy);
        }
        
        public void UnregisterAll()
        {
            rootProxies.Clear();
        }
        
        public void Tick()
        {
            for (var i = rootProxies.Count - 1; i >= 0; i--)
            {
                var proxy = rootProxies[i];
                if (proxy == null)
                {
                    rootProxies.RemoveAt(i);
                    continue;
                }

                proxy.Tick();
            }
        }
        
        #region PlayerLoop Integration

        private struct AmsUpdateSystem { }
        private struct AmsLateUpdateSystem { }
        private struct AmsFixedUpdateSystem { }

        private void InjectIntoPlayerLoop()
        {
            if (isInjected) return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            var targetType = GetTargetSystemType();

            if (targetType == null)
            {
                return;
            }

            playerLoop = InsertSystem(playerLoop, targetType, GetSystemForCurrentMode());
            PlayerLoop.SetPlayerLoop(playerLoop);
            isInjected = true;
        }

        private void RemoveFromPlayerLoop()
        {
            if (!isInjected) return;

            var playerLoop = PlayerLoop.GetCurrentPlayerLoop();
            playerLoop = RemoveSystem<AmsUpdateSystem>(playerLoop);
            playerLoop = RemoveSystem<AmsLateUpdateSystem>(playerLoop);
            playerLoop = RemoveSystem<AmsFixedUpdateSystem>(playerLoop);
            PlayerLoop.SetPlayerLoop(playerLoop);
            isInjected = false;
        }

        private Type GetTargetSystemType()
        {
            return tickMode switch
            {
                AmsTickMode.Update => typeof(Update),
                AmsTickMode.LateUpdate => typeof(PostLateUpdate),
                AmsTickMode.FixedUpdate => typeof(FixedUpdate),
                _ => null
            };
        }

        private PlayerLoopSystem GetSystemForCurrentMode()
        {
            return tickMode switch
            {
                AmsTickMode.Update => new PlayerLoopSystem
                {
                    type = typeof(AmsUpdateSystem),
                    updateDelegate = Tick
                },
                AmsTickMode.LateUpdate => new PlayerLoopSystem
                {
                    type = typeof(AmsLateUpdateSystem),
                    updateDelegate = Tick
                },
                AmsTickMode.FixedUpdate => new PlayerLoopSystem
                {
                    type = typeof(AmsFixedUpdateSystem),
                    updateDelegate = Tick
                },
                _ => default
            };
        }

        private static PlayerLoopSystem InsertSystem(PlayerLoopSystem loop, Type targetType, PlayerLoopSystem systemToInsert)
        {
            var subSystems = loop.subSystemList;
            if (subSystems == null) return loop;

            for (var i = 0; i < subSystems.Length; i++)
            {
                if (subSystems[i].type == targetType)
                {
                    var subList = subSystems[i].subSystemList ?? Array.Empty<PlayerLoopSystem>();
                    var newSubList = new PlayerLoopSystem[subList.Length + 1];
                    Array.Copy(subList, newSubList, subList.Length);
                    newSubList[subList.Length] = systemToInsert;
                    subSystems[i].subSystemList = newSubList;
                    break;
                }
                
                subSystems[i] = InsertSystem(subSystems[i], targetType, systemToInsert);
            }

            loop.subSystemList = subSystems;
            return loop;
        }

        private static PlayerLoopSystem RemoveSystem<T>(PlayerLoopSystem loop)
        {
            var subSystems = loop.subSystemList;
            if (subSystems == null)
            {
                return loop;
            }

            var filtered = new List<PlayerLoopSystem>();
            foreach (var sys in subSystems)
            {
                if (sys.type != typeof(T))
                {
                    filtered.Add(RemoveSystem<T>(sys));
                }
            }

            loop.subSystemList = filtered.ToArray();
            return loop;
        }

        #endregion
    }
}
