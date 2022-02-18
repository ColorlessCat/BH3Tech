using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;

namespace 对崩坏科研3
{
    
    public class JsonHandler
    {
        const string CONFIG_PATH= "config.json";
        public bool appendConfigToFile(Weapon item)
        {
            List<Weapon>? list = readConfig();
            if (list == null&&!File.Exists(CONFIG_PATH)) return false;
            if(list==null) list= new List<Weapon>();
            list.Add(item);
            string json= JsonConvert.SerializeObject(list);
            try
            {
                StreamWriter sw = new StreamWriter(CONFIG_PATH);
                sw.Write(json);
                sw.Flush();
                sw.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            return true;
        }
        public List<Weapon> readConfig()
        {
            string json;
            try
            {
                StreamReader sr = new StreamReader(CONFIG_PATH);
                json= sr.ReadToEnd();
                sr.Close();
            }catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
            return JsonConvert.DeserializeObject<List<Weapon>>(json);
        }
    }
   
}
