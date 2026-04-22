using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace MatinPower.Infrastructure
{
    public class DataDictionary : Dictionary<string, string>, IXmlSerializable
    {
        public string TypeName { get; set; }

        public DataDictionary()
        {

        }
        public static DataDictionary Create(object item, Type type)
        {
            var properties = new DataDictionary { TypeName = type.Name };
            type.GetProperties().Where(p => !Attribute.IsDefined(p, typeof(XmlIgnoreAttribute))).ForEach(p => properties[p.Name] = Convert.ToString(item.GetProperty(p.Name)));
            return properties;
        }
        public static DataDictionary Create(object item, string entityName, string[] propertiesNames)
        {
            var properties = new DataDictionary { TypeName = entityName };
            propertiesNames.ForEach(p => properties[p] = Convert.ToString(item.GetProperty(p)));
            return properties;
        }

        public string SerializeToString()
        {
            using (var stream = new MemoryStream())
            using (var xmlWriter = new XmlTextWriter(stream, Encoding.UTF8))
            {
                var serializer = new XmlSerializer(typeof(DataDictionary), new XmlRootAttribute(TypeName));
                serializer.Serialize(xmlWriter, this);
                return Encoding.UTF8.GetString(stream.GetBuffer());
            }
        }
        public static DataDictionary DeserializeFromString(string typeName, string xmlValue)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xmlValue)))
            using (TextReader reader = new StreamReader(stream))
            {
                var serializer = new XmlSerializer(typeof(DataDictionary), new XmlRootAttribute(typeName));
                var result = (DataDictionary)serializer.Deserialize(reader);
                result.TypeName = typeName;
                return result;
            }
        }

        public XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            if (reader.HasAttributes)
            {
                reader.MoveToNextAttribute();
                do
                    this[reader.Name] = reader.Value;
                while (reader.MoveToNextAttribute());
            }
        }

        public void WriteXml(XmlWriter writer)
        {
            this.ForEach(i => writer.WriteAttributeString(i.Key, i.Value)); ;
        }
    }
}