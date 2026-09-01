#if UNITY_INCLUDE_TESTS
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class GameSaveDeletionTests
{
    string testRoot;
    GameObject owner;
    GameSaveManager manager;

    [SetUp]
    public void SetUp()
    {
        testRoot = Path.Combine(
            Path.GetTempPath(), "DungeonSim-SaveDeletion-" +
            System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(testRoot);
        owner = new GameObject("Save Deletion Test");
        manager = owner.AddComponent<GameSaveManager>();
        typeof(GameSaveManager).GetField(
            "persistenceRootOverride",
            BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(
                manager, testRoot);
    }

    [TearDown]
    public void TearDown()
    {
        if (owner != null)
            Object.DestroyImmediate(owner);
        if (Directory.Exists(testRoot))
            Directory.Delete(testRoot, true);
    }

    [Test]
    public void DeleteSaveRemovesOnlyRecognizedSelectedSlot()
    {
        string firstPath = WriteSave("First", "first.json");
        string secondPath = WriteSave("Second", "second.json");
        SaveSlotInfo first = manager.GetSaveSlots().Find(
            slot => slot.SaveName == "First");

        Assert.That(first, Is.Not.Null);
        Assert.That(manager.DeleteSave(first), Is.True);
        Assert.That(File.Exists(firstPath), Is.False);
        Assert.That(File.Exists(secondPath), Is.True);
        Assert.That(manager.GetSaveSlots(), Has.Count.EqualTo(1));
        Assert.That(manager.GetSaveSlots()[0].SaveName, Is.EqualTo("Second"));
    }

    [Test]
    public void DeleteSaveRejectsUnrecognizedFileWithoutMutation()
    {
        string unrelatedPath = Path.Combine(testRoot, "unrelated.txt");
        File.WriteAllText(unrelatedPath, "keep me");
        var slot = new SaveSlotInfo
        {
            SaveName = "Unrecognized",
            FilePath = unrelatedPath
        };
        LogAssert.Expect(
            LogType.Warning,
            "Could not delete 'Unrecognized': it is no longer a recognized save slot.");

        Assert.That(manager.DeleteSave(slot), Is.False);
        Assert.That(File.Exists(unrelatedPath), Is.True);
    }

    string WriteSave(string saveName, string fileName)
    {
        Directory.CreateDirectory(manager.SavesDirectory);
        string path = Path.Combine(manager.SavesDirectory, fileName);
        File.WriteAllText(path, JsonUtility.ToJson(new DungeonSaveData
        {
            saveName = saveName,
            savedAtUtc = System.DateTime.UtcNow.ToString("O"),
            gridWidth = 32,
            gridHeight = 32
        }, true));
        return path;
    }
}
#endif
