using HarmonyLib;
using JetBrains.Annotations;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnturnedImages.Module.Images;
using UnturnedImages.Module.Workshop;

namespace UnturnedImages.Module.UI
{
    public class UIManager
    {
        private static readonly FieldInfo IconToolsContainerField =
            AccessTools.Field(typeof(MenuWorkshopUI), "iconToolsContainer");

        private bool _isUIAttached;

        private ISleekElement? _iconToolsContainer;

        private readonly List<ISleekElement> _loadedElements;

        private ISleekFloat32Field? _vehicleAnglesXInput;
        private ISleekFloat32Field? _vehicleAnglesYInput;
        private ISleekFloat32Field? _vehicleAnglesZInput;

        public UIManager()
        {
            _loadedElements = new List<ISleekElement>();
        }

        public void Load()
        {
            UnturnedLog.info("UIManager loading");

            OnMenuUIStarted += AttachUI;

            if (IsUnturnedUILoaded())
            {
                AttachUI();
            }
        }

        public void Unload()
        {
            UnturnedLog.info("UIManager unloading");

            OnMenuUIStarted -= AttachUI;

            DetachUI();
        }

        private void AttachUI()
        {
            if (_isUIAttached)
            {
                return;
            }

            _isUIAttached = true;

            UnturnedLog.info("Attaching UI");

            _iconToolsContainer = (ISleekElement?)IconToolsContainerField.GetValue(null);

            if (_iconToolsContainer == null)
            {
                UnturnedLog.error("Could not find MenuWorkshopUI.iconToolsContainer");
            }
            else
            {
                var positionOffsetY = 260;

                void AddElement<TElement>(Func<TElement> constructor, Action<TElement> modifiers)
                    where TElement : ISleekElement
                {
                    var element = constructor();

                    element.sizeOffset_X = 200;
                    element.sizeOffset_Y = 25;

                    element.positionOffset_Y = positionOffsetY;
                    positionOffsetY += 25;

                    modifiers(element);

                    _loadedElements.Add(element);
                    _iconToolsContainer.AddChild(element);
                }

                // Label - UnturnedImages Controls

                AddElement(Glazier.Get().CreateLabel, unturnedImagesVehiclesLabel =>
                {
                    unturnedImagesVehiclesLabel.text = "Unturned 物品载具图片生成器";
                    unturnedImagesVehiclesLabel.fontAlignment = TextAnchor.MiddleCenter;
                });

                // Button - Export All Vehicle Images

                AddElement(Glazier.Get().CreateButton, captureAllVehicleIconsButton =>
                {
                    captureAllVehicleIconsButton.text = "导出所有载具图片";
                    captureAllVehicleIconsButton.onClickedButton += OnClickedCaptureAllVehicleImagesButton;
                });

                // Button - Export All Item Images

                AddElement(Glazier.Get().CreateButton, captureAllVehicleIconsButton =>
                {
                    captureAllVehicleIconsButton.text = "导出所有物品图片";
                    captureAllVehicleIconsButton.onClickedButton += OnClickedCaptureAllItemImagesButton;
                });

                positionOffsetY += 25;

                // Label - Export Certain Mods

                AddElement(Glazier.Get().CreateLabel, exportCertainModsLabel =>
                {
                    exportCertainModsLabel.text = "导出某些MOD";
                });

                // Buttons - Export Certain Mod

                foreach (var mod in WorkshopHelper.GetAllMods())
                {
                    AddElement(Glazier.Get().CreateButton, exportCertainModButton =>
                    {
                        exportCertainModButton.text = mod == 0 ? "Vanilla" : $"Mod {mod}";
                        exportCertainModButton.onClickedButton += x => OnExportModClicked(x, mod);
                    });
                }

                positionOffsetY += 25;

                // Button - Open Extras Folder

                AddElement(Glazier.Get().CreateButton, extrasFolderButton =>
                {
                    extrasFolderButton.text = "打开文件夹";
                    extrasFolderButton.onClickedButton += OnClickedOpenExtrasFolder;
                });

                // Button - Reload Module

                positionOffsetY += 25;

                AddElement(Glazier.Get().CreateButton, reloadModuleButton =>
                {
                    reloadModuleButton.text = "重载mod";
                    reloadModuleButton.onClickedButton += OnClickedReloadModule;
                });

                // Label - Advanced Settings

                positionOffsetY += 25;

                AddElement(Glazier.Get().CreateLabel, advancedSettingsLabel =>
                {
                    advancedSettingsLabel.text = "高级设置";
                });

                // Label - Vehicle Icon Angles

                positionOffsetY += 25;

                AddElement(Glazier.Get().CreateLabel, vehicleIconAnglesLabel =>
                {
                    vehicleIconAnglesLabel.text = "载具图片角度";
                });

                // Vehicle Icon Angles

                AddElement(Glazier.Get().CreateFloat32Field, vehicleAnglesXInput =>
                {
                    vehicleAnglesXInput.addLabel("X", ESleekSide.RIGHT);
                    vehicleAnglesXInput.state = 10;

                    _vehicleAnglesXInput = vehicleAnglesXInput;
                });

                AddElement(Glazier.Get().CreateFloat32Field, vehicleAnglesYInput =>
                {
                    vehicleAnglesYInput.addLabel("Y", ESleekSide.RIGHT);
                    vehicleAnglesYInput.state = 135;

                    _vehicleAnglesYInput = vehicleAnglesYInput;
                });

                AddElement(Glazier.Get().CreateFloat32Field, vehicleAnglesZInput =>
                {
                    vehicleAnglesZInput.addLabel("Z", ESleekSide.RIGHT);
                    vehicleAnglesZInput.state = -10;

                    _vehicleAnglesZInput = vehicleAnglesZInput;
                });

                // Make workshop tools visible by default
                _iconToolsContainer.isVisible = true;
            }
        }

        private void OnExportModClicked(ISleekElement button, uint modId)
        {
            var vehicleAngles = new Vector3(
                _vehicleAnglesXInput?.state ?? 0,
                _vehicleAnglesYInput?.state ?? 0,
                _vehicleAnglesZInput?.state ?? 0);

            IconUtils.CreateExtrasDirectory();
            ImageUtils.CaptureModItemImages(modId);
            ImageUtils.CaptureModVehicleImages(modId, vehicleAngles);
        }

        private void DetachUI()
        {
            if (!_isUIAttached)
            {
                return;
            }

            _isUIAttached = false;

            if (_iconToolsContainer != null)
            {
                foreach (var element in _loadedElements)
                {
                    _iconToolsContainer.RemoveChild(element);
                }
            }

            _vehicleAnglesXInput = null;
            _vehicleAnglesYInput = null;
            _vehicleAnglesZInput = null;
        }

        private bool IsUnturnedUILoaded()
        {
            return MenuUI.window != null;
        }

        private void OnClickedCaptureAllVehicleImagesButton(ISleekElement button)
        {
            var vehicleAngles = new Vector3(
                _vehicleAnglesXInput?.state ?? 0,
                _vehicleAnglesYInput?.state ?? 0,
                _vehicleAnglesZInput?.state ?? 0);

            IconUtils.CreateExtrasDirectory();
            ImageUtils.CaptureAllVehicleImages(vehicleAngles);
        }

        private void OnClickedCaptureAllItemImagesButton(ISleekElement button)
        {
            IconUtils.CreateExtrasDirectory();
            ImageUtils.CaptureAllItemImages();
        }

        private void OnClickedOpenExtrasFolder(ISleekElement button)
        {
            var path = Path.Combine(ReadWrite.PATH, "Extras");

            Process.Start("explorer", path);
        }

        private void OnClickedReloadModule(ISleekElement button)
        {
            var bootstrapperAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(d => d.GetName().Name.Equals("UnturnedImages.Module.Bootstrapper"));

            var bootstrapperClass = bootstrapperAssembly!.GetType("UnturnedImages.Module.Bootstrapper.BootstrapperModule");
            var instanceProperty = bootstrapperClass.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            var initializeMethod = bootstrapperClass.GetMethod("initialize", BindingFlags.Public | BindingFlags.Instance);
            var moduleInstance = instanceProperty!.GetValue(null);

            if (moduleInstance == null)
            {
                UnturnedLog.error("Could not find bootstrapper instance. Reload cancelled.");
                return;
            }

            UnturnedImagesModule.Instance!.shutdown();
            initializeMethod!.Invoke(moduleInstance, new object[0]);
        }

        public delegate void MenuUIStarted();

        public static event MenuUIStarted? OnMenuUIStarted;

        [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
        [HarmonyPatch]
        private static class UnturnedPatches
        {
            [HarmonyPatch(typeof(MenuUI), "customStart")]
            [HarmonyPostfix]
            public static void MenuUICustomStart()
            {
                OnMenuUIStarted?.Invoke();
            }
        }
    }
}
