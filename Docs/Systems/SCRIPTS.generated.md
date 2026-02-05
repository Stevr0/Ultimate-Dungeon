# SCRIPTS.generated.md — Ultimate Dungeon (Auto-Generated)

Generated: 2026-02-02 17:17:30

This file is auto-generated. Do not hand-edit unless you plan to overwrite changes next export.

---

## Actors / Players

| Script | Path | Status | Notes |
|---|---|---|---|
| `ActorComponent` | `Assets/_Scripts/Actors/ActorComponent.cs` | Active |  |
| `FactionService` | `Assets/_Scripts/Actors/FactionService.cs` | Active |  |
| `MonsterAiController` | `Assets/_Scripts/Actors/Monsters/MonsterAiController.cs` | Active |  |
| `MonsterNavMeshMotor` | `Assets/_Scripts/Actors/Monsters/MonsterNavMeshMotor.cs` | Active |  |
| `ClickToMoveInput` | `Assets/_Scripts/Actors/Players/ClickToMoveInput.cs` | Active |  |
| `LeftClickTargetPicker` | `Assets/_Scripts/Actors/Players/LeftClickTargetPicker.cs` | Active |  |
| `PlayerCore` | `Assets/_Scripts/Actors/Players/PlayerCore.cs` | Active |  |
| `PlayerDefinition` | `Assets/_Scripts/Actors/Players/PlayerDefinition.cs` | Active |  |
| `PlayerInteractor` | `Assets/_Scripts/Actors/Players/PlayerInteractor.cs` | Active |  |
| `PlayerStats` | `Assets/_Scripts/Actors/Players/PlayerStats.cs` | Active |  |
| `PlayerTargeting` | `Assets/_Scripts/Actors/Players/PlayerTargeting.cs` | Active |  |
| `PlayerVitalsRegenServer` | `Assets/_Scripts/Actors/Players/PlayerVitalsRegenServer.cs` | Active |  |
| `ServerClickMoveMotor` | `Assets/_Scripts/Actors/Players/ServerClickMoveMotor.cs` | Active |  |
| `TargetIndicatorFollower` | `Assets/_Scripts/Actors/Players/TargetIndicatorFollower.cs` | Active |  |
| `PlayerSkillBook` | `Assets/_Scripts/Skills/PlayerSkillBook.cs` | Active |  |

## Combat

| Script | Path | Status | Notes |
|---|---|---|---|
| `ActorVitals` | `Assets/_Scripts/Combat/Actors/ActorVitals.cs` | Active |  |
| `CombatActorFacade` | `Assets/_Scripts/Combat/Actors/CombatActorFacade.cs` | Active |  |
| `CombatEngageIntent` | `Assets/_Scripts/Combat/Actors/CombatEngageIntent.cs` | Active |  |
| `DoubleClickAttackInput` | `Assets/_Scripts/Combat/Actors/DoubleClickAttackInput.cs` | Active |  |
| `FactionTag.Runtime` | `Assets/_Scripts/Combat/Actors/FactionTag.Runtime.cs` | Active |  |
| `ICombatActor` | `Assets/_Scripts/Combat/Actors/ICombatActor.cs` | Active |  |
| `AttackLoop` | `Assets/_Scripts/Combat/Core/AttackLoop.cs` | Active |  |
| `CombatResolver` | `Assets/_Scripts/Combat/Core/CombatResolver.cs` | Active |  |
| `CombatStateTracker` | `Assets/_Scripts/Combat/Core/CombatStateTracker.cs` | Active |  |
| `DamagePacket` | `Assets/_Scripts/Combat/Core/DamagePacket.cs` | Active |  |
| `PlayerCombatController` | `Assets/_Scripts/Combat/Core/PlayerCombatController.cs` | Active |  |
| `AutoAttackEngageOnTarget` | `Assets/_Scripts/Combat/Net/AutoAttackEngageOnTarget.cs` | Active |  |

## Debug / Tests

| Script | Path | Status | Notes |
|---|---|---|---|
| `CombatDummyTarget` | `Assets/_Scripts/Combat/Debug/CombatDummyTarget.cs` | Active |  |
| `DamageFeedbackReceiver` | `Assets/_Scripts/Debug/DamageFeedbackReceiver.cs` | Active |  |
| `FactionDebugTest` | `Assets/_Scripts/Debug/FactionDebugTest.cs` | Active |  |
| `FloatingDamageText` | `Assets/_Scripts/Debug/FloatingDamageText.cs` | Active |  |
| `HitFlash` | `Assets/_Scripts/Debug/HitFlash.cs` | Active |  |
| `MinimalDebugHud` | `Assets/_Scripts/Debug/MinimalDebugHud.cs` | Active |  |
| `InventoryDebugSeeder` | `Assets/_Scripts/Inventory/Debug/InventoryDebugSeeder.cs` | Active |  |

## Editor

| Script | Path | Status | Notes |
|---|---|---|---|
| `ReadmeEditor` | `Assets/_Misc/TutorialInfo/Scripts/Editor/ReadmeEditor.cs` | Active |  |
| `ScriptInventoryExporter` | `Assets/_Scripts/Editor/ScriptInventoryExporter.cs` | Active |  |

## Items / Inventory

| Script | Path | Status | Notes |
|---|---|---|---|
| `InventoryChangeType` | `Assets/_Scripts/Inventory/InventoryChangeType.cs` | Active |  |
| `InventoryOpResult` | `Assets/_Scripts/Inventory/InventoryOpResult.cs` | Active |  |
| `InventoryRuntimeModel` | `Assets/_Scripts/Inventory/InventoryRuntimeModel.cs` | Active |  |
| `InventorySlotData` | `Assets/_Scripts/Inventory/InventorySlotData.cs` | Active |  |
| `InventoryUIBinder` | `Assets/_Scripts/Inventory/InventoryUIBinder.cs` | Active |  |
| `PlayerInventoryComponent` | `Assets/_Scripts/Inventory/PlayerInventoryComponent.cs` | Active |  |
| `AffixCatalog` | `Assets/_Scripts/Items/AffixCatalog.cs` | Active |  |
| `AffixCatalogValidator_Editor` | `Assets/_Scripts/Items/AffixCatalogValidator_Editor.cs` | Active |  |
| `AffixCountResolver` | `Assets/_Scripts/Items/AffixCountResolver.cs` | Active |  |
| `AffixDef` | `Assets/_Scripts/Items/AffixDef.cs` | Active |  |
| `AffixId` | `Assets/_Scripts/Items/AffixId.cs` | Active |  |
| `AffixInstance` | `Assets/_Scripts/Items/AffixInstance.cs` | Active |  |
| `AffixPicker` | `Assets/_Scripts/Items/AffixPicker.cs` | Active |  |
| `AffixPool` | `Assets/_Scripts/Items/AffixPool.cs` | Active |  |
| `AffixRoller` | `Assets/_Scripts/Items/AffixRoller.cs` | Active |  |
| `ItemDef` | `Assets/_Scripts/Items/ItemDef.cs` | Active |  |
| `ItemDefCatalog` | `Assets/_Scripts/Items/ItemDefCatalog.cs` | Active |  |
| `ItemDefCatalogValidator_Editor` | `Assets/_Scripts/Items/ItemDefCatalogValidator_Editor.cs` | Active |  |
| `ItemIconResolver` | `Assets/_Scripts/Items/ItemIconResolver.cs` | Active |  |
| `ItemInstance` | `Assets/_Scripts/Items/ItemInstance.cs` | Active |  |
| `LootRarity` | `Assets/_Scripts/Items/LootRarity.cs` | Active |  |

## Networking

| Script | Path | Status | Notes |
|---|---|---|---|
| `IInteractable` | `Assets/_Scripts/Actors/Players/Networking/IInteractable.cs` | Active |  |
| `PlayerNetIdentity` | `Assets/_Scripts/Actors/Players/Networking/PlayerNetIdentity.cs` | Active |  |
| `PlayerSkillBookNet` | `Assets/_Scripts/Actors/Players/Networking/PlayerSkillBookNet.cs` | Active |  |
| `PlayerStatsNet` | `Assets/_Scripts/Actors/Players/Networking/PlayerStatsNet.cs` | Active |  |

## Other

| Script | Path | Status | Notes |
|---|---|---|---|
| `Readme` | `Assets/_Misc/TutorialInfo/Scripts/Readme.cs` | Active |  |
| `ICameraFollowTarget` | `Assets/_Scripts/Camera/ICameraFollowTarget.cs` | Active |  |
| `IsometricCameraFollow` | `Assets/_Scripts/Camera/IsometricCameraFollow.cs` | Active |  |
| `LocalCameraBinder` | `Assets/_Scripts/Camera/LocalCameraBinder.cs` | Active |  |
| `DeterministicRng` | `Assets/_Scripts/Progression/DeterministicRng.cs` | Active |  |
| `SkillUseResolver` | `Assets/_Scripts/Progression/SkillUseResolver.cs` | Active |  |
| `StatGainSystem` | `Assets/_Scripts/Progression/StatGainSystem.cs` | Active |  |
| `StatID` | `Assets/_Scripts/Progression/StatID.cs` | Active |  |
| `SkillDef` | `Assets/_Scripts/Skills/SkillDef.cs` | Active |  |
| `SkillGainSystem` | `Assets/_Scripts/Skills/SkillGainSystem.cs` | Active |  |
| `SkillID` | `Assets/_Scripts/Skills/SkillID.cs` | Active |  |
| `ServerTargetValidator` | `Assets/_Scripts/Targeting/ServerTargetValidator.cs` | Active |  |
| `TargetingResolver` | `Assets/_Scripts/Targeting/TargetingResolver.cs` | Active |  |
| `FactionTag` | `Assets/_Scripts/World/FactionTag.cs` | Active |  |
| `SimpleInteractable` | `Assets/_Scripts/World/SimpleInteractable.cs` | Active |  |

## Scenes

| Script | Path | Status | Notes |
|---|---|---|---|
| `SceneRuleBootstrapValidator` | `Assets/_Scripts/Scenes/SceneRuleBootstrapValidator.cs` | Active |  |
| `SceneRuleProvider` | `Assets/_Scripts/Scenes/SceneRuleProvider.cs` | Active |  |

## UI

| Script | Path | Status | Notes |
|---|---|---|---|
| `TargetFrameUI` | `Assets/_Scripts/Targeting/UI/TargetFrameUI.cs` | Active |  |
| `TargetRingFactionTint` | `Assets/_Scripts/Targeting/UI/TargetRingFactionTint.cs` | Active |  |
| `TargetRingPresenter` | `Assets/_Scripts/Targeting/UI/TargetRingPresenter.cs` | Active |  |
| `TargetRingPulse` | `Assets/_Scripts/Targeting/UI/TargetRingPulse.cs` | Active |  |
| `LocalPlayerUIBinder` | `Assets/_Scripts/UI/Binding/LocalPlayerUIBinder.cs` | Active |  |
| `HotbarSlotUI` | `Assets/_Scripts/UI/Canvas_HUD/Hotbar/HotbarSlotUI.cs` | Active |  |
| `HotbarUI` | `Assets/_Scripts/UI/Canvas_HUD/Hotbar/HotbarUI.cs` | Active |  |
| `HotbatInputRouter` | `Assets/_Scripts/UI/Canvas_HUD/Hotbar/HotbatInputRouter.cs` | Active |  |
| `HudVitalsUI` | `Assets/_Scripts/UI/Canvas_HUD/HudVitalsUI.cs` | Active |  |
| `NetworkHudController` | `Assets/_Scripts/UI/Canvas_HUD/NetworkHudController.cs` | Active |  |
| `UIModal` | `Assets/_Scripts/UI/Canvas_Modal/UIModal.cs` | Active |  |
| `UIModalManager` | `Assets/_Scripts/UI/Canvas_Modal/UIModalManager.cs` | Active |  |
| `CharacterStatsPanelUI` | `Assets/_Scripts/UI/Canvas_Windows/CharacterStatsPanelUI.cs` | Active |  |
| `EquipmentSlotId` | `Assets/_Scripts/UI/Canvas_Windows/Equipment/EquipmentSlotId.cs` | Active |  |
| `EquipmentSlotViewUI` | `Assets/_Scripts/UI/Canvas_Windows/Equipment/EquipmentSlotViewUI.cs` | Active |  |
| `InventoryEquipmentPanelUI` | `Assets/_Scripts/UI/Canvas_Windows/Equipment/InventoryEquipmentPanelUI.cs` | Active |  |
| `InventoryGridLayoutUI` | `Assets/_Scripts/UI/Canvas_Windows/Imventory/InventoryGridLayoutUI.cs` | Active |  |
| `InventorySlotPlaceholderUI` | `Assets/_Scripts/UI/Canvas_Windows/Imventory/InventorySlotPlaceholderUI.cs` | Active |  |
| `InventoryWindowUI` | `Assets/_Scripts/UI/Canvas_Windows/Imventory/InventoryWindowUI.cs` | Active |  |
| `InventoryWindowUI_GridHook` | `Assets/_Scripts/UI/Canvas_Windows/Imventory/InventoryWindowUI_GridHook.cs` | Active |  |
| `InventoryGridViewUI` | `Assets/_Scripts/UI/Canvas_Windows/Imventory/Slots/InventoryGridViewUI.cs` | Active |  |
| `InventorySlotViewUI` | `Assets/_Scripts/UI/Canvas_Windows/Imventory/Slots/InventorySlotViewUI.cs` | Active |  |
| `UIWindow` | `Assets/_Scripts/UI/Canvas_Windows/UIWindow.cs` | Active |  |
| `UIWindowClampToCanvas` | `Assets/_Scripts/UI/Canvas_Windows/UIWindowClampToCanvas.cs` | Active |  |
| `UIWindowDrag` | `Assets/_Scripts/UI/Canvas_Windows/UIWindowDrag.cs` | Active |  |
| `UIWindowFrame` | `Assets/_Scripts/UI/Canvas_Windows/UIWindowFrame.cs` | Active |  |
| `UIWindowManager` | `Assets/_Scripts/UI/Canvas_Windows/UIWindowManager.cs` | Active |  |
| `InventoryWeightPanelUI` | `Assets/_Scripts/UI/Canvas_Windows/Weight/InventoryWeightPanelUI.cs` | Active |  |
| `UIHotkeyToggler` | `Assets/_Scripts/UI/UI_Debug/UIHotkeyToggler.cs` | Active |  |

