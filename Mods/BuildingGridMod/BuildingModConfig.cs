using Unity.Mathematics;

namespace ApproximatelyUpMod
{
    internal static class BuildingModConfig
    {
        private static readonly float[] AllowedGridSizes = { 1.0f, 0.5f, 0.25f, 0.1f };
        private static int _gridSizeIndex = 2;

        public static float GridSize => AllowedGridSizes[_gridSizeIndex];

        public static bool DisablePlacementCollisions { get; set; }

        public static void StepGridSize(int direction)
        {
            if (direction > 0)
            {
                _gridSizeIndex = (_gridSizeIndex + 1) % AllowedGridSizes.Length;
            }
            else if (direction < 0)
            {
                _gridSizeIndex = (_gridSizeIndex - 1 + AllowedGridSizes.Length) % AllowedGridSizes.Length;
            }
        }

        public static string GridSizeLabel()
        {
            return GridSize.ToString("0.###");
        }

        /// <summary>Restore grid size and collision bypass to game defaults.</summary>
        public static void Reset()
        {
            _gridSizeIndex = 2; // 0.25f — game default
            DisablePlacementCollisions = false;
        }

        public static float3 GetEffectiveSnapping(float3 originalSnapping)
        {
            float custom = GridSize;
            if (custom <= 0f)
            {
                return originalSnapping;
            }

            return new float3(custom, custom, custom);
        }
    }
}
