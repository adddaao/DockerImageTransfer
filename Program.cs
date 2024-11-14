更新代码为// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using X.Common.Helper;
using System.Globalization;
using LocalizationMessages;
using System.Runtime.InteropServices;
using System.Diagnostics;

bool compress = false;
bool isChinese = IsSystemLanguageChinese();

Extract7zTool(); // 在程序启动时解压 7zzs 工具

Console.WriteLine(LocalMessages.GetLocalizedMessage("SaveOrLoad", isChinese, Environment.NewLine));
string? option = Console.ReadLine();

if (option == "s")
{
    Console.WriteLine(LocalMessages.GetLocalizedMessage("UseMultithreading", isChinese));
    bool useMultithreading = Console.ReadLine()?.ToLower() == "y";
    SaveImages(useMultithreading);
}
else if (option == "l")
{
    LoadImages();
}

/// <summary>
/// 保存 Docker 镜像，支持多线程导出。
/// </summary>
/// <param name="useMultithreading">是否使用多线程导出</param>
void SaveImages(bool useMultithreading)
{
    var output = CommandHelper.Execute("docker", "image ls");
    List<Image> images = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
    .Select(x => Regex.Split(x, "\\s{2,}"))  // 使用正则表达式匹配两个或以上的空格
    .Where(split => split.Length >= 5)       // 至少要有五列信息，分别是 Repo、Tag、Id、Created、Size
    .Select(split => new Image(split[0], split[1], split[2], split[3], split[4]))
    .Where(image => !image.Repo.Contains("<none>") && !image.Tag.Contains("<none>"))
    .Where(image => !image.Repo.Contains("REPOSITORY") && !image.Tag.Contains("TAG") && !image.Id.Contains("IMAGE"))
    .ToList();

    DisplayImages(images);

    Console.WriteLine(LocalMessages.GetLocalizedMessage("UseCompression", isChinese));
    compress = Console.ReadLine()?.ToLower() == "y";

    Console.WriteLine(LocalMessages.GetLocalizedMessage("SelectImages", isChinese));
    string? input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input))
    {
        input = string.Join(" ", Enumerable.Range(0, images.Count));
    }

    int[] selected = Regex.Split(input, "\\s+").Where(s => int.TryParse(s, out _)).Select(int.Parse).ToArray();
    if (useMultithreading)
    {
        ExportImagesParallel(selected, images);
    }
    else
    {
        ExportImagesSequential(selected, images);
    }
}

/// <summary>
/// 从存档文件加载 Docker 镜像。
/// </summary>
void LoadImages()
{
    DirectoryInfo dir = new(Environment.CurrentDirectory);

    Console.WriteLine(LocalMessages.GetLocalizedMessage("ExtractingFiles", isChinese));
    foreach (var sz in dir.GetFiles("*.7z"))
    {
        string output = CommandHelper.Execute("7zzs", $"x -bsp1 {sz.Name}");
        Console.WriteLine(output);
    }

    Console.WriteLine(LocalMessages.GetLocalizedMessage("LoadingImages", isChinese));
    foreach (var tar in dir.GetFiles("*.tar"))
    {
        string output = CommandHelper.Execute("docker", $"load -i {tar.Name}");
        Console.WriteLine(output);
    }

    Console.WriteLine(LocalMessages.GetLocalizedMessage("DeleteTarFiles", isChinese));
    if (Console.ReadLine()?.ToLower() == "y")
    {
        foreach (var tar in dir.GetFiles("*.tar"))
        {
            tar.Delete();
        }
    }
}

/// <summary>
/// 显示可用的 Docker 镜像列表。
/// </summary>
/// <param name="images">Docker 镜像列表</param>
void DisplayImages(List<Image> images)
{
    if (images == null || images.Count == 0)
    {
        Console.WriteLine(LocalMessages.GetLocalizedMessage("NoImagesFound", isChinese));
        return;
    }

    int count = 0;
    int repoMaxLength = images.Max(img => img.Repo.Length) + 5;
    int tagMaxLength = images.Max(img => img.Tag.Length) + 5;
    int createdMaxLength = images.Max(img => img.Created.Length) + 5;
    int sizeMaxLength = images.Max(img => img.Size.Length) + 5;

    foreach (var image in images)
    {
        Console.WriteLine($"{count++,-2} : {image.Repo.PadRight(repoMaxLength)}{image.Tag.PadRight(tagMaxLength)}{image.Id.PadRight(20)}{image.Created.PadRight(createdMaxLength)}{image.Size.PadRight(sizeMaxLength)}");
    }
}


/// <summary>
/// 并行导出选定的 Docker 镜像。
/// </summary>
/// <param name="selected">选中的镜像索引数组</param>
/// <param name="images">Docker 镜像列表</param>
void ExportImagesParallel(int[] selected, List<Image> images)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    int maxParallelism = Math.Max(1, Environment.ProcessorCount / 2);
    var tasks = new List<Task>();

    foreach (var index in selected)
    {
        tasks.Add(Task.Run(() =>
        {
            if (index < 0 || index >= images.Count)
            {
                Console.WriteLine(LocalMessages.GetLocalizedMessage("InvalidIndex", isChinese, index));
                return;
            }

            var image = images[index];
            string name = $"{image.Repo}:{image.Tag}";
            string fileName = $"{Path.GetFileName(image.Repo)}-{image.Tag}";
            Console.WriteLine(LocalMessages.GetLocalizedMessage("ExportingImage", isChinese, name));
            CommandHelper.Execute("docker", $"save {name} -o {fileName}.tar");

            if (compress)
            {
                Console.WriteLine(LocalMessages.GetLocalizedMessage("CompressingFile", isChinese, fileName));
                CompressFile(fileName);
            }

            Console.WriteLine(LocalMessages.GetLocalizedMessage("ExportedImage", isChinese, name));

            stopwatch.Stop();
            Console.WriteLine(LocalMessages.GetLocalizedMessage("PackagingCompletedTime", isChinese, stopwatch.Elapsed));
        }));
    }

    var limitedConcurrency = new SemaphoreSlim(maxParallelism);

    foreach (var task in tasks)
    {
        limitedConcurrency.Wait();
        task.ContinueWith(t => limitedConcurrency.Release());
    }

    Task.WaitAll(tasks.ToArray());

}
/// <summary>
/// 顺序导出选定的 Docker 镜像。
/// </summary>
/// <param name="selected">选中的镜像索引数组</param>
/// <param name="images">Docker 镜像列表</param>
void ExportImagesSequential(int[] selected, List<Image> images)
{
    foreach (var index in selected)
    {
        if (index < 0 || index >= images.Count)
        {
            Console.WriteLine(LocalMessages.GetLocalizedMessage("InvalidIndex", isChinese, index));
            continue;
        }

        var image = images[index];
        string name = $"{image.Repo}:{image.Tag}";
        string fileName = $"{Path.GetFileName(image.Repo)}-{image.Tag}";
        Console.WriteLine(LocalMessages.GetLocalizedMessage("ExportingImage", isChinese, name));
        CommandHelper.Execute("docker", $"save {name} -o {fileName}.tar");

        if (compress)
        {
            Console.WriteLine(LocalMessages.GetLocalizedMessage("CompressingFile", isChinese, fileName));
            CompressFile(fileName);
        }

        Console.WriteLine(LocalMessages.GetLocalizedMessage("ExportedImage", isChinese, name));
    }
}

/// <summary>
/// 判断系统语言是否为中文。
/// </summary>
/// <returns>如果系统语言为中文，返回 true；否则返回 false</returns>
bool IsSystemLanguageChinese()
{
    // 尝试读取环境变量 LANG
    string? lang = Environment.GetEnvironmentVariable("LANG");

    if (!string.IsNullOrEmpty(lang))
    {
        return lang.StartsWith("zh");
    }

    // 如果没有设置 LANG 环境变量，则使用 InstalledUICulture 作为备用
    CultureInfo cultureInfo = CultureInfo.InstalledUICulture;
    return cultureInfo.TwoLetterISOLanguageName == "zh";
}
/// <summary>
/// 使用 7zzs 压缩指定的文件。
/// </summary>
/// <param name="fileName">要压缩的文件名</param>
void CompressFile(string fileName)
{
    string toolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7zzs");
    string compressResult = CommandHelper.Execute(toolPath, $"a -mx9 -bsp1 {fileName}.7z {fileName}.tar");
    Console.WriteLine(compressResult);
}

/// <summary>
/// 从嵌入资源中提取 7zzs 工具，并设置可执行权限（适用于 Linux/Unix）。
/// </summary>
void Extract7zTool()
{
    string toolName = RuntimeInformation.ProcessArchitecture == Architecture.Arm || RuntimeInformation.ProcessArchitecture == Architecture.Arm64 ? "7zzs-arm" : "7zzs-x64";
    var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "7zzs");
    if (!File.Exists(outputPath))
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        using (var resourceStream = assembly.GetManifestResourceStream($"DockerImgTransfer.Resources.{toolName}")) // 替换为实际命名空间和资源路径
        {
            if (resourceStream == null)
            {
                throw new FileNotFoundException($"{toolName} resource not found");
            }

            using (var fileStream = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                resourceStream.CopyTo(fileStream);
            }
        }
        // 设置可执行权限（仅适用于 Linux / Unix 系统）
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            Process chmodProcess = new Process();
            chmodProcess.StartInfo.FileName = "/bin/chmod";
            chmodProcess.StartInfo.Arguments = $"+x {outputPath}";
            chmodProcess.StartInfo.UseShellExecute = false;
            chmodProcess.StartInfo.CreateNoWindow = true;
            chmodProcess.Start();
            chmodProcess.WaitForExit();
        }
    }
}


public class Image
{
    public Image(string repo, string tag, string id, string created, string size)
    {
        Repo = repo;
        Tag = tag;
        Id = id;
        Created = created;
        Size = size;
    }

    public string Repo { get; set; }
    public string Tag { get; set; }
    public string Id { get; set; }
    public string Created { get; set; }
    public string Size { get; set; }
}
