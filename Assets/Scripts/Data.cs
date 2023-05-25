using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Com.Ajinkya.FpsGame
{
    public class Data : MonoBehaviour
    {
        public static void SaveProfile(ProfileData t_profile)
        {
            try
            {
                string path = Application.persistentDataPath + "/profile.dt";

                if (File.Exists(path)) File.Delete(path);

                FileStream file = File.Create(path);

                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(file, t_profile);
                file.Close();

                Debug.Log("Saved successfuly!");
            }
            catch
            {
                Debug.Log("Something went terribly wrong!");
            }
        }

        public static ProfileData LoadProfile()
        {
            ProfileData ret = new ProfileData();

            try
            {
                string path = Application.persistentDataPath + "/profile.dt";

                if (File.Exists(path))
                {
                    FileStream file = File.Open(path, FileMode.Open);
                    BinaryFormatter bf = new BinaryFormatter();
                    ret = (ProfileData)bf.Deserialize(file);
                }

                Debug.Log("Loaded successfuly!");
            }
            catch
            {
                Debug.Log("File was not found");
            }

            return ret;
        }
    }
}