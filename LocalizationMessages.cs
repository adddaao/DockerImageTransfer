namespace LocalizationMessages
{
    public static class LocalMessages
    {
        public static string GetLocalizedMessage(string key, bool isChinese, params object[] args)
        {
            string message = key switch
            {
                "SaveOrLoad" => isChinese ? "s: 保存镜像 {0}l: 加载镜像" : "s: save images {0}l: load images",
                "UseMultithreading" => isChinese ? "是否使用多线程导出？(y/n)" : "Use multithreading for export? (y/n)",
                "UseCompression" => isChinese ? "是否使用7z进行压缩（默认否）。请输入y或n。" : "Whether to use 7z for compression (default is no). Please enter y or n.",
                "SelectImages" => isChinese ? "选择要导出的镜像，用空格分隔，或者留空选择所有。" : "Select images to export, use space to separate or leave empty to select all.",
                "ExtractingFiles" => isChinese ? "解压7z文件..." : "Extracting 7z files...",
                "LoadingImages" => isChinese ? "加载镜像..." : "Loading images...",
                "DeleteTarFiles" => isChinese ? "删除tar文件？(y/n)" : "Delete tar files? (y/n)",
                "InvalidIndex" => isChinese ? "无效索引: {0}" : "Invalid index: {0}",
                "ExportingImage" => isChinese ? "正在导出 {0}" : "Exporting {0}",
                "CompressingFile" => isChinese ? "正在压缩 {0}.tar..." : "Compressing {0}.tar...",
                "ExportedImage" => isChinese ? "已导出 {0}" : "Exported {0}",
                "ProcessorCount" => isChinese ? "当前并行度 {0}" : "ProcessorCount {0}",
                "PackagingCompletedTime" => isChinese ? "打包完成，耗时 {0}" : "Packaging completed, elapsed time: {0}",
                _ => key
            };

            return string.Format(message, args);
        }
    }
}
