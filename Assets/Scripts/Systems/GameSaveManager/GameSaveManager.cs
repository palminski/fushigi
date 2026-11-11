using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class RunData
{
    public List<string> knownTopics = new List<string>();
    public List<string> storyFlags = new List<string>();
    public List<string> convoy = new List<string>();
}

[System.Serializable]
public class PersistentData
{
    public List<string> knownTopics = new List<string>();
    public List<string> storyFlags = new List<string>();
}

public static class GameSaveManager
{
    private static RunData run;
    public static RunData Run => run ??= Load<RunData>("run.json");

    private static PersistentData persistent;
    public static PersistentData Persistent => persistent ??= Load<PersistentData>("persistent.json");
    private static string PathFor(string filename) => Path.Combine(Application.persistentDataPath, filename);

    
    // Save Methods
    public static void SaveRun() => Save(Run, "run.json");
    public static void SavePersistent() => Save(Persistent, "persistent.json");

    //Helper Methods
    public static bool TopicKnown(Topic topic)
    {
        if(topic == null) return false;
        return Run.knownTopics.Contains(topic.name) || Persistent.knownTopics.Contains(topic.name);
    }

    public static bool HasFlag(StoryFlag flag)
    {
        if (flag == null) return false;
        return Run.storyFlags.Contains(flag.name) || Persistent.storyFlags.Contains(flag.name);

    }

    //
    public static void SendToConvoy(Item item)
    {
        if(item == null) return;
        Run.convoy.Add(item.name);
        Debug.Log($"[Convoy is a stub] {item.name} has been sent to the convoy");
    }

    public static void LearnTopic(Topic topic, MemoryScope scope = MemoryScope.Global)
    {
        if(topic == null) return;

        switch (scope)
        {
            case MemoryScope.Run:
            AddOnce(Run.knownTopics, topic.name);
            break;

            case MemoryScope.Global:
            if(AddOnce(Persistent.knownTopics, topic.name)) SavePersistent();
            break;
        }
    }

    public static void SetFlag(StoryFlag flag, MemoryScope scope)
    {
        if(flag == null) return;

        switch (scope)
        {
            case MemoryScope.Run:
            AddOnce(Run.storyFlags, flag.name);
            break;

            case MemoryScope.Global:
            if(AddOnce(Persistent.storyFlags, flag.name)) SavePersistent();
            break;
        }
    }

    public static void WipeRunData()
    {
        run = new RunData();
        DeleteFile("run.json");
    }

    public static void NukeSaveData()
    {
        run = new RunData();
        persistent = new PersistentData();
        DeleteFile("run.json");
        DeleteFile("persistent.json");
    }


    // Private Utility Functions
    private static bool AddOnce(List<string> list, string name)
    {
        if(list.Contains(name)) return false;
        list.Add(name);
        return true;
    }

    private static void DeleteFile(string fileName)
    {
        string path = PathFor(fileName);
        if(File.Exists(path)) File.Delete(path);
    }


    // Private Functions For Performing Data Actions
    private static void Save<T>(T data, string fileName)
    {
        File.WriteAllText(PathFor(fileName), JsonUtility.ToJson(data, true));
    }
    private static T Load<T>(string fileName) where T : new()
    {
        string path = PathFor(fileName);
        if(!File.Exists(path)) return new T();
        return JsonUtility.FromJson<T>(File.ReadAllText(path));
    }
}
