using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LieDown.Modles
{
    public class Setting
    {
        public Setting()
        {
            AutoFillAP = true;
        }
        public bool AutoFillAP { get; set; }

        public SlashMode Mode { get; set; }

        public int Stage { get; set; }


        public void Save(string avatarAddress)
        {
            var str = this.ToJson();
            if (Directory.Exists("setting"))
            {
                Directory.CreateDirectory("setting");
            }
            File.WriteAllText($"setting/{avatarAddress}.json", str);
        }
        public static Setting LoadSetting(string avatarAddress)
        {

            return new Setting();
        }
    }
    public enum SlashMode
    {
        Progress=0,
        Bootstrap=1        
    }
}
