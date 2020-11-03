using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace WClocks
{
    public interface IXmlSerializationCallback
    {
        void OnXmlSerialization(object sender);
        void OnXmlDeserialized(object sender);
    }

    public class Serializer
    {
        public class XtraXmlSerializer : XmlSerializer
        {
            public XtraXmlSerializer(Type t) : base(t) { }

            public new void Serialize(TextWriter textWriter, object o)
            {
                CallSerializationCallback(o, RaiseOnSerialize);
                base.Serialize(textWriter, o);
            }

            public new object Deserialize(TextReader textReader)
            {
                object dObject = base.Deserialize(textReader);
                CallSerializationCallback(dObject, RaiseOnDeserialized);
                return dObject;
            }

            private void CallSerializationCallback(object o, Action<object> callbackMethod)
            {
                var callbackObject = o as IXmlSerializationCallback;
                if (callbackObject != null)
                {
                    callbackMethod(o);
                }

                var fieldInfos = o.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (var fieldInfo in fieldInfos)
                {
                    if (fieldInfo.GetValue(o) is IXmlSerializationCallback)
                    {
                        CallSerializationCallback(fieldInfo.GetValue(o), callbackMethod);
                    }
                }

                var properties = o.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var propertyInfo in properties)
                {
                    if (propertyInfo.GetValue(o) is IXmlSerializationCallback)
                    {
                        CallSerializationCallback(propertyInfo.GetValue(o), callbackMethod);
                    }
                }
            }

            private void RaiseOnDeserialized(object o)
            {
                ((IXmlSerializationCallback)o).OnXmlDeserialized(this);
            }

            private void RaiseOnSerialize(object o)
            {
                ((IXmlSerializationCallback)o).OnXmlSerialization(this);
            }
        }


        public static string SerializeToXml<T>(T serializableObject, XmlWriterSettings xmlSettings = null)
        {
            XtraXmlSerializer serializer = new XtraXmlSerializer(typeof(T));
            using (Stream storage = new MemoryStream())
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(storage, xmlSettings ?? new XmlWriterSettings()))
                {
                    serializer.Serialize(xmlWriter, serializableObject);
                }

                storage.Seek(0, SeekOrigin.Begin);
                using (StreamReader reader = new StreamReader(storage, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }


        public static void SaveToXml<T>(String fileName, T serializableObject, XmlWriterSettings xmlSettings = null)
        {
            if (!Directory.Exists(Path.GetDirectoryName(fileName))) Directory.CreateDirectory(Path.GetDirectoryName(fileName));

            XtraXmlSerializer serializer = new XtraXmlSerializer(typeof(T));
            using (TextWriter writer = new StreamWriter(fileName))
            {
                using (XmlWriter xmlWriter = XmlWriter.Create(writer, xmlSettings ?? new XmlWriterSettings() { Indent = true }))
                {
                    serializer.Serialize(xmlWriter, serializableObject);
                }
            }
        }


        public static T DeserializeFromXml<T>(String xmlText)
        {
            XtraXmlSerializer serializer = new XtraXmlSerializer(typeof(T));
            using (Stream storage = new MemoryStream())
            {
                byte[] encoded = Encoding.UTF8.GetBytes(xmlText);
                storage.Write(encoded, 0, encoded.Length);
                storage.Seek(0, SeekOrigin.Begin);

                using (TextReader reader = new StreamReader(storage))
                {
                    return (T)serializer.Deserialize(reader);
                }
            }
        }


        public static T LoadFromXml<T>(String fileName)
        {
            XtraXmlSerializer serializer = new XtraXmlSerializer(typeof(T));
            using (TextReader reader = new StreamReader(fileName))
            {
                return (T)serializer.Deserialize(reader);
            }
        }

        public static T LoadFromAssemblyXml<T>(String path, String mark, Action<String> warnDelegate = null)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return LoadFromXml<T>(Path.Combine(assemblyPath, path));
        }


        public static T SafeLoadFromXml<T>(String path, String mark, Action<String> warnDelegate = null)
        {
            try
            {
                string settFilePath = path;
                if (!File.Exists(settFilePath))
                {
                    SaveToXml(settFilePath, Activator.CreateInstance<T>());
                    if (!String.IsNullOrEmpty(mark)) mark += " ";
                    string warn = mark + "Settings file not found!\nCreate a new empty settings file.";
                    if (warnDelegate != null) warnDelegate(warn);
                    return default(T);
                }
                else
                {
                    return (T)LoadFromXml<T>(settFilePath);
                }
            }
            catch (Exception ex)
            {
                string error = "Error! Can't load config file : " + ex.Message;
                if (warnDelegate != null) warnDelegate(error);
                return default(T);
            }
        }

        public static T SafeLoadFromAssemblyXml<T>(String path, String mark, Action<String> warnDelegate = null)
        {
            string assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return SafeLoadFromXml<T>(Path.Combine(assemblyPath, path), mark, warnDelegate);
        }
    }
}
