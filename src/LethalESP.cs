using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LethalESP
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class LethalESP : BaseUnityPlugin
    {
        // Basic plugin information
        public const string pluginGuid = "lethalesp.lethalcompany.mod";
        public const string pluginName = "LethalESP";
        public const string pluginVersion = "1.1.0.0";

        // Singleton instance
        public static LethalESP Instance;

        // Lists to track objects in each round of the game
        public List<GrabbableObject> GrabbableObjects = new List<GrabbableObject>();
        public List<Turret> Turrets = new List<Turret>();
        public List<Landmine> Landmines = new List<Landmine>();

        // ESP toggles
        private bool EnableEnemyAIESP { get; set; } = false;
        private bool EnableMapHazardsESP { get; set; } = false;
        private bool EnableGrabbableObjectsESP { get; set; } = false;

        public void Awake()
        {
            if (Instance != null)
            {
                Logger.LogError($"Another instance of {pluginName} is already loaded");
                Destroy(this);
                return;
            }
            Instance = this;
            Logger.LogInfo($"{pluginName} {pluginVersion} loaded");

            // Enable hooks for tracking game object spawning
            Harmony harmony = new Harmony(Guid.NewGuid().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public void OnGUI()
        {
            // remove the destroyed objects
            GrabbableObjects = GrabbableObjects.Where(obj => obj != null).ToList();
            Turrets = Turrets.Where(obj => obj != null).ToList();
            Landmines = Landmines.Where(obj => obj != null).ToList();

            if (EnableEnemyAIESP)
            {
                Render.DrawString(new Vector2(5, 2), "EnemyAI ESP Enabled", Color.red, false);
                DrawEnemyAIESP();
            }

            if (EnableMapHazardsESP)
            {
                Render.DrawString(new Vector2(5, 20), "MapHazards ESP Enabled", Color.yellow, false);
                DrawMapHazardsESP();
            }

            if (EnableGrabbableObjectsESP)
            {
                Render.DrawString(new Vector2(5, 40), "GrabbableObjects ESP Enabled", Color.green, false);
                DrawGrabbableObjectsESP();
            }
        }

        public void Update()
        {
            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                EnableEnemyAIESP = !EnableEnemyAIESP;
            }

            if (Keyboard.current.f11Key.wasPressedThisFrame)
            {
                EnableMapHazardsESP = !EnableMapHazardsESP;
            }

            if (Keyboard.current.f12Key.wasPressedThisFrame)
            {
                EnableGrabbableObjectsESP = !EnableGrabbableObjectsESP;
            }
        }

        private void DrawEnemyAIESP()
        {
            if (RoundManager.Instance == null || GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.gameplayCamera == null)
            {
                return;
            }

            foreach (EnemyAI enemy in RoundManager.Instance.SpawnedEnemies)
            {
                (Vector3 componentTopPos, Vector3 componentBottomPos) = GetComponentTopBottom(enemy, false, 2f);

                Vector3 worldToScreenComponentTopPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(componentTopPos);
                Vector3 worldToScreenComponentBottomPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(componentBottomPos);

                if (worldToScreenComponentTopPos.z > 0f && worldToScreenComponentBottomPos.z > 0f)
                {
                    DrawBoxESP(worldToScreenComponentBottomPos, worldToScreenComponentTopPos, Color.red, 1f, true, enemy.name);
                }
            }
        }

        private void DrawMapHazardsESP()
        {
            if (GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.gameplayCamera == null)
            {
                return;
            }

            // Combine all the components into a single IEnumerable
            IEnumerable<Component> components = Turrets.Cast<Component>()
                                                       .Concat(Landmines.Cast<Component>());

            // Return if there are no components to draw
            if (components == null || components.Count() == 0)
            {
                return;
            }

            foreach (Component component in components)
            {
                (Vector3 componentTopPos, Vector3 componentBottomPos) = GetComponentTopBottom(component, true, 1f);

                Vector3 worldToScreenComponentTopPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(componentTopPos);
                Vector3 worldToScreenComponentBottomPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(componentBottomPos);

                if (worldToScreenComponentTopPos.z > 0f && worldToScreenComponentBottomPos.z > 0f)
                {
                    DrawBoxESP(worldToScreenComponentBottomPos, worldToScreenComponentTopPos, Color.yellow, 1f, false, component.name);
                }
            }
        }

        public void DrawGrabbableObjectsESP()
        {
            if (GameNetworkManager.Instance.localPlayerController == null || GameNetworkManager.Instance.localPlayerController.gameplayCamera == null)
            {
                return;
            }

            foreach (GrabbableObject grabbableObject in GrabbableObjects)
            {
                if (grabbableObject.isHeld)
                {
                    continue;
                }

                (Vector3 componentTopPos, Vector3 componentBottomPos) = GetComponentTopBottom(grabbableObject, true, 1f);

                Vector3 worldToScreenComponentTopPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(componentTopPos);
                Vector3 worldToScreenComponentBottomPos = GameNetworkManager.Instance.localPlayerController.gameplayCamera.WorldToScreenPoint(componentBottomPos);

                if (worldToScreenComponentTopPos.z > 0f && worldToScreenComponentBottomPos.z > 0f)
                {
                    DrawBoxESP(worldToScreenComponentBottomPos, worldToScreenComponentTopPos, Color.green, 1f, false, grabbableObject.name);
                }
            }
        }

        private (Vector3, Vector3) GetComponentTopBottom(Component component, bool pivotPoint, float size)
        {
            Vector3 componentPos = component.transform.position;

            Vector3 componentTopPos;
            Vector3 componentBottomPos;

            componentTopPos.x = componentPos.x;
            componentTopPos.z = componentPos.z;
            componentBottomPos.x = componentPos.x;
            componentBottomPos.z = componentPos.z;

            Collider[] renderers = component.GetComponentsInChildren<Collider>();
            foreach (Collider renderer in renderers)
            {
                size = Math.Max(Math.Max(Math.Max(renderer.bounds.max.y - renderer.bounds.min.y, renderer.bounds.max.x - renderer.bounds.min.x), renderer.bounds.max.z - renderer.bounds.min.z), size);
            }

            if (renderers.Length == 0)
            {
                Logger.LogWarning($"{component.name} HAS NO RENDERER");
            }

            if (pivotPoint)
            {
                componentTopPos.y = componentPos.y + (size / 2);
                componentBottomPos.y = componentPos.y - (size / 2);
            }
            else
            {
                componentTopPos.y = componentPos.y + size;
                componentBottomPos.y = componentPos.y;
            }

            return (componentTopPos, componentBottomPos);
        }

        private void DrawBoxESP(Vector3 bottomPos, Vector3 topPos, Color color, float widthOffset, bool snapLine, string label)
        {
            // Scale the pixel's position on screen
            bottomPos = ScaleVector(bottomPos);
            topPos = ScaleVector(topPos);

            // Check if the object is within the screen
            if (bottomPos.x < 0 || bottomPos.x > Screen.width || bottomPos.y < 0 || bottomPos.y > Screen.height)
            {
                return;
            }

            // Flip the y-coordinates after scaling
            topPos.y = Screen.height - topPos.y;
            bottomPos.y = Screen.height - bottomPos.y;

            // Calculate the width of the box
            float height = topPos.y - bottomPos.y;
            float width = height / widthOffset;

            // Draw the ESP box
            Render.DrawBox(bottomPos.x - (width / 2), bottomPos.y, width, height, color, 2f, label);

            // Snapline
            if (snapLine)
            {
                Render.DrawLine(new Vector2(Screen.width / 2, Screen.height / 2), new Vector2(bottomPos.x, bottomPos.y), color, 2f);
            }
        }

        // Scale the pixel's position on screen according to resolution vs default resolution (860x540)
        public static Vector3 ScaleVector(Vector3 position)
        {
            return new Vector3(
                position.x / GameNetworkManager.Instance.localPlayerController.gameplayCamera.pixelWidth * Screen.width,
                position.y / GameNetworkManager.Instance.localPlayerController.gameplayCamera.pixelHeight * Screen.height,
                position.z
            );
        }
    }
}
