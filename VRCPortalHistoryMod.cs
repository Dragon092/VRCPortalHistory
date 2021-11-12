using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using MelonLoader;
using UnityEngine;
using VRC;
using VRC.Core;
using UIExpansionKit.API;
using VRChatUtilityKit.Utilities;
using UnhollowerRuntimeLib.XrefScans;

namespace VRCPortalHistory
{
    public class VRCPortalHistoryMod : MelonMod
    {
        private static List<PortalHistoryEntry> portalHistoryList = new List<PortalHistoryEntry>();

        public override void OnApplicationStart()
        {
            HarmonyInstance.Patch(typeof(PortalInternal).GetMethods().Where(mb => mb.Name.StartsWith("Method_Public_Void_") && mb.Name.Length <= 21 && XrefUtils.CheckUsedBy(mb, "OnTriggerEnter")).First(), prefix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalEnter", BindingFlags.Static | BindingFlags.Public)));
            HarmonyInstance.Patch(typeof(PortalInternal).GetMethod("ConfigurePortal"), postfix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalDropped", BindingFlags.Static | BindingFlags.Public)));
            HarmonyInstance.Patch(typeof(PortalInternal).GetMethod("OnDestroy"), prefix: new HarmonyMethod(typeof(VRCPortalHistoryMod).GetMethod("OnPortalDestroyed", BindingFlags.Static | BindingFlags.Public)));

            ExpansionKitApi.GetExpandedMenu(ExpandedMenu.QuickMenu).AddSimpleButton("Respawn last portal", () => respawnLastPortal());

            MelonLogger.Msg("Initialized!");
        }

        public static void OnPortalDropped(MonoBehaviour __instance, Player __3)
        {
            MelonLogger.Msg("OnPortalDropped");
        }

        public static void OnPortalDestroyed(MonoBehaviour __instance)
        {
            //cachedDroppers.Remove(__instance.GetInstanceID());
            MelonLogger.Msg("OnPortalDestroyed");

            MelonLogger.Msg(__instance.name);

            GameObject test_go = __instance.gameObject;

            MelonLogger.Msg(test_go.name);

            PortalInternal portalInternal = test_go.GetComponentInChildren<PortalInternal>();

            if (!portalInternal)
            {
                MelonLogger.Msg("Error: no portalInternal");
                return;
            }

            string roomId = portalInternal.field_Private_String_1;
            MelonLogger.Msg(roomId);

            var world = new ApiWorld { id = portalInternal.field_Private_ApiWorld_0.id };
            MelonLogger.Msg(world.id);

            ApiWorldInstance apiWorldInstance = new ApiWorldInstance(world, roomId);


            PortalHistoryEntry newEntry = new PortalHistoryEntry(world, apiWorldInstance);
            portalHistoryList.Add(newEntry);

            // Remove old entries
            if(portalHistoryList.Count > 8)
            {
                portalHistoryList.RemoveAt(0);
            }

            /*
            string worldId = portalInternal.field_Private_ApiWorld_0.id;
            MelonLogger.Msg(worldId);
            int roomPop = portalInternal.field_Private_Int32_0;
            MelonLogger.Msg(roomPop);
            */
        }

        public static bool OnPortalEnter(PortalInternal __instance)
        {
            MelonLogger.Msg("OnPortalEnter");
            return true;
        }

        private void respawnLastPortal()
        {
            if (portalHistoryList.Count == 0)
            {
                MelonLogger.Msg("Error: no last entry");
                return;
            }

            Transform playerTransform = VRCPlayer.field_Internal_Static_VRCPlayer_0.transform;

            var menu = ExpansionKitApi.CreateCustomQuickMenuPage(LayoutDescription.QuickMenu4Columns);

            foreach (PortalHistoryEntry portalHistoryEntry in portalHistoryList)
            {
                MelonLogger.Msg("Found portal in list: " + portalHistoryEntry.apiWorld.name);

                menu.AddSimpleButton(portalHistoryEntry.apiWorld.name, () => {
                    Utilities.CreatePortal(portalHistoryEntry.apiWorld, portalHistoryEntry.apiWorldInstance, playerTransform.position, playerTransform.forward, true);
                });
            }

            menu.Show();
        }
    }

    class PortalHistoryEntry
    {
        public ApiWorld apiWorld = null;
        public ApiWorldInstance apiWorldInstance = null;

        public PortalHistoryEntry(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance)
        {
            this.apiWorld = apiWorld;
            this.apiWorldInstance = apiWorldInstance;
        }
    }

    public static class Utilities
    {
        private static CreatePortalDelegate ourCreatePortalDelegate;

        private delegate bool CreatePortalDelegate(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool withUIErrors);

        private static CreatePortalDelegate GetCreatePortalDelegate
        {
            get
            {
                if (ourCreatePortalDelegate != null) return ourCreatePortalDelegate;
                MethodInfo portalMethod = typeof(PortalInternal).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly).First(
                    m => m.ReturnType == typeof(bool)
                         && m.HasParameters(typeof(ApiWorld), typeof(ApiWorldInstance), typeof(Vector3), typeof(Vector3), typeof(bool))
                         && m.XRefScanFor("admin_dont_allow_portal"));
                ourCreatePortalDelegate = (CreatePortalDelegate)Delegate.CreateDelegate(typeof(CreatePortalDelegate), portalMethod);
                return ourCreatePortalDelegate;
            }
        }

        public static bool CreatePortal(ApiWorld apiWorld, ApiWorldInstance apiWorldInstance, Vector3 position, Vector3 forward, bool showAlerts)
        {
            return GetCreatePortalDelegate(apiWorld, apiWorldInstance, position, forward, showAlerts);
        }

        private static bool HasParameters(this MethodBase methodBase, params Type[] types)
        {
            ParameterInfo[] parameters = methodBase.GetParameters();
            int typesLength = types.Length;
            if (parameters.Length < typesLength) return false;

            for (var i = 0; i < typesLength; ++i)
                if (parameters[i].ParameterType != types[i])
                    return false;

            return true;
        }

        public static bool XRefScanFor(this MethodBase methodBase, string searchTerm)
        {
            return XrefScanner.XrefScan(methodBase).Any(
                xref => xref.Type == XrefType.Global && xref.ReadAsObject()?.ToString().IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0);
        }
    }
}
