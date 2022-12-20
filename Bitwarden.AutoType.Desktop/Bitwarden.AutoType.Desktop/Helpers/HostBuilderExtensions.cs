using System;
using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bitwarden.AutoType.Desktop.Helpers;

public static class HostBuilderExtensions
{
    public static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions()
    { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static IHostBuilder ConfigureUserLocalAppDataJsonFile<T>(this IHostBuilder hostBuilder,
        string folderName, string fileName, bool alwaysWriteFileOnLoad = false) where T : class, new()
    {
        T? instance = null;

        // create folder if not exists
        var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName);
        Directory.CreateDirectory(dataPath); // create folder if not exists

        // if not exists create file
        var fullPath = Path.Combine(dataPath, fileName);
        if (!File.Exists(fullPath))
        {
            instance = new T();
            var content = JsonSerializer.Serialize(instance, SerializerOptions);
            File.WriteAllText(fullPath, content, Encoding.UTF8);
            instance = null;
        }

        // load json file if exists
        if (File.Exists(fullPath))
        {
            var json = File.ReadAllText(fullPath, Encoding.UTF8);
            instance = JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }

        if (alwaysWriteFileOnLoad)
        {
            var content = JsonSerializer.Serialize(instance!, SerializerOptions);
            File.WriteAllText(fullPath, content, Encoding.UTF8);
        }

        ArgumentNullException.ThrowIfNull(instance);

        // add to DI
        hostBuilder
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton(instance);
            services.AddSingleton(new Action<T>((t) =>
            {
                var content = JsonSerializer.Serialize(t, SerializerOptions);
                File.WriteAllText(fullPath, content, Encoding.UTF8);
            }));
        });

        return hostBuilder;
    }

    public static IHostBuilder ConfigureUserLocalAppDataJsonFile<T>(this IHostBuilder hostBuilder, string folderName,
        string fileName, out T? singleton, out Action<T>? saveToFile, bool alwaysWriteFileOnLoad = false) where T : class, new()
    {
        saveToFile = null;
        T? instance = singleton = null;

        // create folder if not exists
        var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), folderName);
        Directory.CreateDirectory(dataPath); // create folder if not exists

        // if not exists create file
        var fullPath = Path.Combine(dataPath, fileName);
        if (!File.Exists(fullPath))
        {
            instance = new T();
            var content = JsonSerializer.Serialize(instance, SerializerOptions);
            File.WriteAllText(fullPath, content, Encoding.UTF8);
            instance = null;
        }

        // load json file if exists
        if (File.Exists(fullPath))
        {
            var json = File.ReadAllText(fullPath, Encoding.UTF8);
            instance = singleton = JsonSerializer.Deserialize<T>(json, SerializerOptions);
        }

        ArgumentNullException.ThrowIfNull(instance);

        var saveMethod = saveToFile = new Action<T>((t) =>
        {
            var content = JsonSerializer.Serialize(t, SerializerOptions);
            File.WriteAllText(fullPath, content, Encoding.UTF8);
        });

        if (alwaysWriteFileOnLoad)
        {
            var content = JsonSerializer.Serialize(instance, SerializerOptions);
            File.WriteAllText(fullPath, content, Encoding.UTF8);
        }

        // add to DI
        hostBuilder
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton(instance);
            services.AddSingleton(saveMethod);
        });

        return hostBuilder;
    }
}

////public class JsonFileConfigurationSource : IConfigurationSource
////{
////    public IConfigurationProvider Build(IConfigurationBuilder builder)
////    {
////        //return new JsonFileConfigurationSource();

////    }
////}
//public class SecurityMetadata
//{
//    public string ApiKey { get; set; }
//    public string ApiSecret { get; set; }
//}

//public class JsonFileConfigurationProvider : ConfigurationProvider
//{
//    public override void Set(string key, string value)
//    {
//        base.Set(key, value);

//        ////Get Whole json file and change only passed key with passed value. It requires modification if you need to support change multi level json structure
//        //var fileFullPath = base.Source.FileProvider.GetFileInfo(base.Source.Path).PhysicalPath;
//        //string json = File.ReadAllText(fileFullPath);
//        //dynamic jsonObj = JsonConvert.DeserializeObject(json);
//        //jsonObj[key] = value;
//        //string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
//        //File.WriteAllText(fileFullPath, output);
//    }

//    public override void Load()
//    {
//        var text = File.ReadAllText(@"D:\SecurityMetadata.json");
//        var options = new JsonSerializerOptions
//        { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
//        var content = JsonSerializer.Deserialize<SecurityMetadata>
//       (text, options);
//        if (content != null)
//        {
//            Data = new Dictionary<string, string>
//                {
//                    {"ApiKey", content.ApiKey},
//                    {"ApiSecret", content.ApiSecret}
//                };
//        }

//        //    someObject.GetType()
//        //.GetProperties(BindingFlags.Instance | BindingFlags.Public)
//        //     .ToDictionary(prop => prop.Name, prop => (string)prop.GetValue(someObject, null))
//    }
//}