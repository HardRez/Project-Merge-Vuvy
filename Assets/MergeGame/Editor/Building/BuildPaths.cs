namespace MergeGame.Editor.Building
{
    /// <summary>Where the scene builder reads its inputs and writes the generated assets.</summary>
    internal static class BuildPaths
    {
        public const string UiSpritesFolder = "Assets/MergeGame/Content/Sprites/UI";
        public const string IconSpritesFolder = ContentSeeder.IconSpritesFolder;
        public const string FontFile = "Assets/MergeGame/Content/Fonts/Baloo2-ExtraBold.ttf";
        public const string FontAsset = "Assets/MergeGame/Content/Fonts/Baloo2-ExtraBold SDF.asset";
        public const string FontOutlineMaterial = "Assets/MergeGame/Content/Fonts/Baloo2-ExtraBold SDF Outline.mat";
        public const string FontTitleMaterial = "Assets/MergeGame/Content/Fonts/Baloo2-ExtraBold SDF Title.mat";

        public const string UiDataFolder = "Assets/MergeGame/Content/UI";
        public const string Theme = UiDataFolder + "/UiTheme.asset";
        public const string NoticeTexts = UiDataFolder + "/NoticeTexts.asset";
        public const string SoundLibrary = UiDataFolder + "/SoundLibrary.asset";

        /// <summary>Sound effects and music; file names are fixed by <c>Tools/PackArt/import_pack_art.py</c>.</summary>
        public const string AudioFolder = "Assets/MergeGame/Content/Audio";
        public const string MusicClip = AudioFolder + "/music_main.ogg";

        public const string PrefabsFolder = "Assets/MergeGame/Content/Prefabs";
        public const string BoardCellPrefab = PrefabsFolder + "/BoardCell.prefab";
        public const string CellCoverPrefab = PrefabsFolder + "/CellCover.prefab";
        public const string SandGrainMaterial = "Assets/MergeGame/Content/Materials/SandGrains.mat";
        public const string SandMaskMaterial = "Assets/MergeGame/Content/Materials/SandMask.mat";
        public const string CustomerPortraitMaterial = "Assets/MergeGame/Content/Materials/CustomerPortrait.mat";
        public const string UiAdditiveMaterial = "Assets/MergeGame/Content/Materials/UiAdditive.mat";
        public const string ItemViewPrefab = PrefabsFolder + "/ItemView.prefab";
        public const string PointsPopupPrefab = PrefabsFolder + "/PointsPopup.prefab";
        public const string RequirementSlotPrefab = PrefabsFolder + "/RequirementSlot.prefab";
        public const string OrderCardPrefab = PrefabsFolder + "/OrderCard.prefab";
        public const string CustomerStagePrefab = PrefabsFolder + "/CustomerStage.prefab";

        /// <summary>Zenject loads the project context from a Resources folder by this exact name.</summary>
        public const string ProjectContextPrefab = PrefabsFolder + "/Resources/ProjectContext.prefab";

        public const string ScenesFolder = "Assets/MergeGame/Scenes";
        public const string MainMenuScene = ScenesFolder + "/MainMenu.unity";
        public const string GameScene = ScenesFolder + "/Game.unity";
    }
}
