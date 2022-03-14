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

        public int Rank { get; set; }

        public int RankingBattleBlockIndex { get; set; }

        static readonly string path = $"{Application.StartupPath}setting";
        public void Save(string avatarAddress)
        {
            var str = this.ToJson();

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            File.WriteAllText($"{path}/{avatarAddress}.json", str);
            
        }
        public static Setting LoadSetting(string avatarAddress)
        {
            try
            {
                var js = File.ReadAllText($"{path}/{avatarAddress}.json");
                if (!string.IsNullOrEmpty(js))
                {
                    return js.JosnToObj<Setting>();
                }
            }
            catch (DirectoryNotFoundException)
            {
            }
            catch (FileNotFoundException)
            {
            }

            return new Setting();
        }
        
    }
    public enum SlashMode
    {
        Progress=0,
        Bootstrap=1        
    }
}
