// See https://aka.ms/new-console-template for more information

using System.Text.RegularExpressions;
using X.Common.Helper;
using System.Runtime.InteropServices;

//bool compress = args.Length > 0 && args[0] == "compress";
bool compress = false;
string imgTag = RuntimeInformation.ProcessArchitecture == Architecture.X64 ? ":amd64" : string.Empty;
Console.WriteLine($"s: save images {Environment.NewLine}l: load images");
string? option = Console.ReadLine();

if (option == "s")
{
    var output = CommandHelper.Execute("docker", "image ls");
    List<Image> images = output.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries).Select(x =>
    {
        var split = x.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        return new Image(split[0], split[1], split[2]);
    }).ToList();
    int count = 0;
    foreach (var image in images)
        Console.WriteLine($"{count++,-2} : {image.Repo,-35}{image.Tag,-20}\t{image.Id,-20}");


    Console.WriteLine("是否采用7z进行压缩(默认为否)请输入 y、n");
    string? isCompress = Console.ReadLine();
    if (isCompress == "y" && !string.IsNullOrWhiteSpace(isCompress))
    {
        compress = true;
    }

    Console.WriteLine("Select images to export, use space to seperate");

    string? input = Console.ReadLine();

    //if (string.IsNullOrWhiteSpace(input))
    //    input = string.Join(" ", Enumerable.Range(1, images.Count - 1).Select(x => x.ToString()));

    if (!string.IsNullOrWhiteSpace(input))
    {
        bool isAllDigits = Regex.IsMatch(input, @"^\d+$");

        if (isAllDigits)
        {
            // 根据数字进行ID的精确匹配
            //var selectedImages = images.Where(image => image.Id == number.ToString()).ToArray();

            // 输出选中的图像信息
            Console.WriteLine("Selected images:");

            int[] selected = Regex.Split(input, @"\s+").Select(x => int.Parse(x)).ToArray();
            exportImages(selected, images);
        }
        else
        {
            // 根据repo进行模糊匹配
            var filteredImages = images.Where(image => image.Repo.Contains(input));
            var matchingIndexes = images
                                    .Select((image, index) => new { Image = image, Index = index })
                                    .Where(item => item.Image.Repo.Contains(input))
                                    .Select(item => item.Index)
                                    .ToList();
            int count2 = 0;
            foreach (var image in filteredImages)
                Console.WriteLine($"{matchingIndexes[count2++],-2} : {image.Repo,-35}{image.Tag,-20}\t{image.Id,-20}");

            string? subInput = "";
            while (true)
            {
                // 输出选中的图像信息
                Console.WriteLine("请输入选择的id(不支持全选):");
                subInput = Console.ReadLine();

                if (!string.IsNullOrWhiteSpace(subInput))
                {
                    break;
                }
            }

            var selected = Regex.Split(subInput, @"\s+").Select(x => int.Parse(x)).ToArray();
            exportImages(selected, images);
        }
    }
    else
    {
        Console.WriteLine("No input entered. Showing all images:");
        // 使用所有图像进行后续处理，与上述代码类似
        input = string.Join(" ", Enumerable.Range(1, images.Count - 1).Select(x => x.ToString()));
        var selected = Regex.Split(input, @"\s+").Select(x => int.Parse(x)).ToArray();
        exportImages(selected, images);
    }

}
else if (option == "l")
{
    DirectoryInfo dir = new(Environment.CurrentDirectory);
    var szs = dir.GetFiles("*.7z"); //, SearchOption.AllDirectories);
    Console.WriteLine("Extracting 7z files...");
    foreach (var sz in szs)
    {
        string output = CommandHelper.Execute("docker", $"run --rm -v {Environment.CurrentDirectory}:/app 7z{imgTag} x -bsp1 {sz.Name} -y");
        Console.WriteLine(output);
    }

    Console.WriteLine("Loading images...");
    var tars = dir.GetFiles("*.tar");
    foreach (var tar in tars)
    {
        string output = CommandHelper.Execute("docker", $"load -i {tar.Name}");
        Console.WriteLine(output);
    }
    Console.WriteLine("Delete tar files? Y/n");
    string? deleteTarInput = Console.ReadLine();
    if (deleteTarInput != "n" || deleteTarInput != "N")
        foreach (var tar in tars)
            tar.Delete();
}


void exportImages(int[] selected, List<Image> images)
{
    Parallel.ForEach(selected, index =>
    // foreach (var index in selected)
    {
        var image = images[index];
        string name = $"{image.Repo}:{image.Tag}";
        string fileName = $"{Path.GetFileName(image.Repo)}-{image.Tag}";
        Console.WriteLine($"Exporting {name}");
        CommandHelper.Execute("docker", $"save {name} -o {fileName}.tar");
        if (compress)
        {
            Console.WriteLine($"Exported {name}. Compressing...");
            string compressResult = CommandHelper.Execute("docker", $"run --rm -v {Environment.CurrentDirectory}:/app 7z{imgTag} a -mx9 -bsp1 -sdel {fileName}.7z {fileName}.tar");
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine(compressResult);
            Console.WriteLine("Compressed");
        }
        else
            Console.WriteLine($"Exported {name}. ");
    });
}
//Console.WriteLine("Hello, World!");

public class Image
{
    public Image(string repo, string tag, string id)
    {
        Repo = repo;
        Tag = tag;
        Id = id;
    }

    public string Repo { get; set; }
    public string Tag { get; set; }
    public string Id { get; set; }
}