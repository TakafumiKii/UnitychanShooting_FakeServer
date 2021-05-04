using Newtonsoft.Json;
using System.IO;
using System;
namespace FakeServer.Utility
{
    public static class FileConverter
    {
        public static bool LoadXML<Type>(out Type obj, string path)
        {
            if (File.Exists(path))
            {
                //  XmlSerializerオブジェクトを作成
                System.Xml.Serialization.XmlSerializer serializer =
                   new System.Xml.Serialization.XmlSerializer(typeof(Type));

                //ファイルストリームを開く
                System.IO.StreamReader sr = new System.IO.StreamReader(
                    path, new System.Text.UTF8Encoding(false));

                //XMLファイルストリームからデシリアライズする
                obj = (Type)serializer.Deserialize(sr);

                //ストリームを閉じる
                sr.Close();
                return true;
            }
            obj = default(Type);
            return false;
        }
        public static bool LoadXML<Type>(out Type obj, byte[] data)
        {
            //XmlSerializerオブジェクトを作成
            System.Xml.Serialization.XmlSerializer serializer =
                new System.Xml.Serialization.XmlSerializer(typeof(Type));

            // メモリストリームを開く
            MemoryStream sr = new MemoryStream(data);

            //メモリストリームからデシリアライズする
            obj = (Type)serializer.Deserialize(sr);

            //ストリームを閉じる
            sr.Close();
            return true;
        }
        public static bool LoadJson<Type>(out Type obj, string path)
        {
            if (File.Exists(path))
            {
                // JSON
                StreamReader sr = File.OpenText(path);
                string text = sr.ReadToEnd();
                obj = JsonConvert.DeserializeObject<Type>(text);
                sr.Close();
                return true;
            }
            obj = default(Type);
            return false;
        }
        public static bool SaveXML(object obj, string path)
        {
            try
            {
                //XmlSerializerオブジェクトを作成
                //オブジェクトの型を指定する
                System.Xml.Serialization.XmlSerializer serializer =
                    new System.Xml.Serialization.XmlSerializer(obj.GetType());
                //書き込むファイルを開く（UTF-8 BOM無し）
                System.IO.StreamWriter sw = new System.IO.StreamWriter(
                    path, false, new System.Text.UTF8Encoding(false));
                //シリアル化し、XMLファイルに保存する
                serializer.Serialize(sw, obj);
                //ファイルを閉じる
                sw.Close();
                return true;
            }
            catch
            {
                Console.WriteLine("セーブデータの更新に失敗");
                return false;
            }
        }

        public static bool SaveJson(object obj, string path)
        {
            try
            {
                string text = JsonConvert.SerializeObject(obj);
                StreamWriter sw = File.CreateText(path);
                sw.Write(text);
                sw.Close();
                return true;
            }
            catch
            {
                Console.WriteLine("セーブデータの更新に失敗");
                return false;
            }
        }
    }
}