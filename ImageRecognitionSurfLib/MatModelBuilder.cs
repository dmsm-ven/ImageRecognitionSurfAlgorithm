using OpenCvSharp;
using System.Collections.Concurrent;
using System.Data;

namespace ImageRecognitionSurfLib;

public partial class OpenCvSharpProcessor
{
    internal class MatModelBuilder
    {
        public static readonly int ULTIMATE_ABILITIES_COUNT = 12;
        public static readonly int REGULAR_ABILITIES_COUNT = 36;
        public static readonly Size ICON_SIZE = new(55, 55);
        public static readonly Rect DeskSpellsPanelBounds = new(new Point(670, 140), new Size(590, 700));
        public static readonly Rect UltimateSpellsPanelBounds = new(new Point(670, 140), new Size(590, 200));

        private readonly OpenCvSharpProcessor parent;

        public MatModelBuilder(OpenCvSharpProcessor parent)
        {
            this.parent = parent;
        }

        public Mat? BuildMat(byte[] data, AbilityPanel? panel)
        {
            var mat = Cv2.ImDecode(data, parent.ImreadMode);

            if (!panel.HasValue)
            {
                mat = mat.Resize(ICON_SIZE);
            }

            if (parent.UseBlurOptions)
            {
                mat = mat.Blur(new Size(parent.BlurSize, parent.BlurSize));
            }

            if (parent.UseCannyOptions)
            {
                mat.ConvertTo(mat, parent.PREDEFINED_MatType);
                mat = mat.Canny(parent.Canny_Treshold1, parent.Canny_Treshold2, parent.Canny_AppertureSize, parent.Canny_L2Gradient);
            }

            if (parent.UseThresholdOptions)
            {
                mat.ConvertTo(mat, parent.PREDEFINED_MatType);
                mat = mat.Threshold(parent.Threshold_Thresh, parent.Threshold_MaxVal, parent.Threshold_Type);
            }

            if (panel.HasValue && panel.Value != AbilityPanel.Unchanged)
            {
                Rect panelRect = panel.Value switch
                {
                    AbilityPanel.Ultimates => UltimateSpellsPanelBounds,
                    AbilityPanel.FullDesk => DeskSpellsPanelBounds,
                    _ => throw new ArgumentOutOfRangeException()
                };
                mat = new Mat(mat, panelRect);
            }
            return mat;
        }

        public async Task<AbilityMatWrapper[]> GetPredefinedItems(IEnumerable<string> iconFiles)
        {
            ConcurrentDictionary<string, byte[]> bag = new();
            var iconFilesDataTasks = iconFiles.Select(async (i) => await Task.Run(async () =>
            {
                var data = await File.ReadAllBytesAsync(i);
                bag[Path.GetFileNameWithoutExtension(i)] = data;
            }));

            await Task.WhenAll(iconFilesDataTasks);

            return await GetPredefinedItems(bag);
        }

        public async Task<AbilityMatWrapper[]> GetPredefinedItems(IDictionary<string, byte[]> iconFiles)
        {
            var list = new List<AbilityMatWrapper>();

            foreach (var file in iconFiles)
            {
                Mat? mat = BuildMat(file.Value, panel: AbilityPanel.Unchanged);

                list.Add(new AbilityMatWrapper()
                {
                    ImageName = Path.GetFileNameWithoutExtension(file.Key),
                    MatValue = mat
                });
            }
            return list.ToArray();
        }

        public int AbilityPanelToMaxItemsCount(AbilityPanel panel)
        {
            return panel switch
            {
                AbilityPanel.FullDesk => REGULAR_ABILITIES_COUNT + ULTIMATE_ABILITIES_COUNT,
                AbilityPanel.RegularAbilities => REGULAR_ABILITIES_COUNT,
                AbilityPanel.Ultimates => ULTIMATE_ABILITIES_COUNT,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}

