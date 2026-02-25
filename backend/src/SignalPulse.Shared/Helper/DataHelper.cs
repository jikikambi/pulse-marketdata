using Newtonsoft.Json;
using System.Reflection;

namespace SignalPulse.Shared.Helper;

public static class DataHelper
{
    public static string ReadText(string folder, string fileName)
    {
        var strTxt = GetDataFromDiskAsTextString(folder, fileName);
        return strTxt;
    }    

    private static string GetDataFromDiskAsTextString(string folder, string fileName)
    {
        var binFolderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        var filePath = Path.Combine(binFolderPath!,"Prompts", folder, fileName);

        if (!File.Exists(filePath)) 
        {
            throw new FileNotFoundException($"File {filePath} not found");
        }

        var str = File.ReadAllText(filePath);
        return str;
    }

}