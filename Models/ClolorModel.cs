using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace ShotgunMetagenome.Models
{

    /// <summary>
    /// 色と色名を保持するクラス
    /// </summary>
    public class ColorModel
    {
        public Color Color { get; set; }
        public string Name { get; set; }

    }

    public static class ColorList
    {
        public static List<ColorModel> GetList()
            => typeof(Colors).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(i => new ColorModel() { Color = (Color)i.GetValue(null), Name = i.Name }).ToList();

    }

}
