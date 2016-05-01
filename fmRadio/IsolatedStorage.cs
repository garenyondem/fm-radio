using System;
using System.IO.IsolatedStorage;
using System.Xml.Serialization;

namespace fmRadio
{
    public class IsolatedStorage
    {
        public static T DeserializeXml<T>(IsolatedStorageFileStream stream)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            return (T)serializer.Deserialize(stream);
        }

        public static void SerializeXml<T>(IsolatedStorageFileStream stream, T state)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            serializer.Serialize(stream, state);
        }

        public static T LoadFromIsolatedStorage<T>(string FileName)
        {
            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                if (file.FileExists(FileName))
                {
                    using (IsolatedStorageFileStream stream = file.OpenFile(FileName, System.IO.FileMode.Open))
                    {
                        return DeserializeXml<T>(stream);
                    }
                }
                return Activator.CreateInstance<T>();
            }
        }

        public static void SaveToIsolatedStorage<T>(T state, string FileName)
        {
            using (IsolatedStorageFile file = IsolatedStorageFile.GetUserStoreForApplication())
            {
                using (IsolatedStorageFileStream stream = file.OpenFile(FileName, System.IO.FileMode.Create))
                {
                    SerializeXml<T>(stream, state);
                }
            }
        }
    }
}