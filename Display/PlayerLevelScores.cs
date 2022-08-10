using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using System.Security.Cryptography;
using UnityEngine;
using System.Text;

public static class PlayerLevelScores
{
    public static string levelScoresFilePath = Application.persistentDataPath + "/scores.xml";

    public static void WritePlayerTopScores(Dictionary<int, int> highscores)
    {
        XMLUtility.Serialize( highscores);
    }

    public static Dictionary<int, int> ReadPlayerTopScores()
    {
        Dictionary<int, int> result = new Dictionary<int, int>();

        if (File.Exists(levelScoresFilePath))
        {
            XMLUtility.Deserialize(result);
        }

        Debug.LogWarning("File does not exists at " + levelScoresFilePath);

        return result;
    }
}

public static class XMLUtility
{
    private static string cryptoKey = "Testing1";

    public static void Serialize(IDictionary dictionary)
    {
        List<Entry> entries = new List<Entry>(dictionary.Count);
        foreach (object key in dictionary.Keys)
        {
            entries.Add(new Entry(key, dictionary[key]));
        }
        XmlSerializer serializer = new XmlSerializer(typeof(List<Entry>));

        // Create Encryption stuff.
        DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
        DES.Key = ASCIIEncoding.ASCII.GetBytes(cryptoKey);
        DES.IV = ASCIIEncoding.ASCII.GetBytes(cryptoKey);
        ICryptoTransform desencrypt = DES.CreateEncryptor();

        Stream stream = new FileStream(PlayerLevelScores.levelScoresFilePath, FileMode.OpenOrCreate);
        using (CryptoStream cStream = new CryptoStream(stream, desencrypt, CryptoStreamMode.Write))
        {
            serializer.Serialize(cStream, entries);
        }
        stream.Close();
    }

    public static void Deserialize(IDictionary dictionary)
    {
        dictionary.Clear();
        XmlSerializer serializer = new XmlSerializer(typeof(List<Entry>));
        Stream stream = new FileStream(PlayerLevelScores.levelScoresFilePath, FileMode.Open);

        // Create Decryption stuff.
        DESCryptoServiceProvider DES = new DESCryptoServiceProvider();
        DES.Key = ASCIIEncoding.ASCII.GetBytes(cryptoKey);
        DES.IV = ASCIIEncoding.ASCII.GetBytes(cryptoKey);
        ICryptoTransform desdecrypt = DES.CreateDecryptor();

        using (CryptoStream cStream = new CryptoStream(stream, desdecrypt, CryptoStreamMode.Read))
        {
            List<Entry> list = (List<Entry>)serializer.Deserialize(cStream);
            foreach (Entry entry in list)
            {
                dictionary[entry.Key] = entry.Value;
            }
        }

        stream.Close();
    }

    public class Entry
    {
        public object Key;
        public object Value;
        public Entry()
        {
        }

        public Entry(object key, object value)
        {
            Key = key;
            Value = value;
        }
    }
}