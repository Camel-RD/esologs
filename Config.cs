using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace ESOLogs
{
    public class Config
    {
        public string LogFileName { get; set; }
        public static Config ReadXml()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                using (FileStream fs = new FileStream("Config.xml", FileMode.Open))
                {
                    Config config = (Config)serializer.Deserialize(fs);
                    return config;
                }
            }
            catch (Exception)
            {
                return new Config();
            }
        }

        public void WriteXml()
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Config));
                using (StreamWriter writer = new StreamWriter("Config.xml"))
                {
                    serializer.Serialize(writer, this);
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
