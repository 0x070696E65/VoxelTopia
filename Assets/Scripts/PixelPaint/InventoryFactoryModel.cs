using System.Collections.Generic;
using System.IO;
using System.Linq;

/*
 * var d = new List<(string metalId, List<(string fileName, byte[] bytes)> datas)>();
        Debug.Log(storageVoxelList.Count);
        foreach (var v in storageVoxelList)
        {
            var voxel = v.GetComponent<InventoryVoxel>();
            if (voxel.toggle.isOn)
            {
                var datas = InventoryFactoryModel.GetDataFromStorageForInventory($"{Application.persistentDataPath}/voxels/{voxel.filePath}");
                d.Add((voxel.metalId, (datas)));
            }
        }
        foreach (var valueTuples in d)
        {
            Debug.Log("metalID: "+valueTuples.metalId);
            Debug.Log("==fileName==");
            foreach (var valueTuple in valueTuples.datas)
            {
                Debug.Log(valueTuple.fileName);
            }
        }
 */
public class InventoryFactoryModel
{
    public static List<(string fileName, byte[] data)> GetDataFromStorageForInventory(string folderPath)
    {
        var files = Directory.GetFiles(folderPath, "*.png", SearchOption.TopDirectoryOnly);
        return (from file in files let data = File.ReadAllBytes(file) select (Path.GetFileName(file), data)).ToList();
    }
}