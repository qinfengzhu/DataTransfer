using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;

namespace DataTransfer.Core.SAS
{
    /// <summary>
    /// StatTransfer 转换控制器
    /// </summary>
    public class StatTransferManager
    {
        private string _exePath;
        private string _stexePath;
        /// <summary>
        /// 是否使用本地的stat/transfer
        /// true:  Path->ST 命令,把stat/transfer 软件的bin 配置到Path
        /// false: 释放本地的嵌入式绿色的stat/transfer
        /// </summary>
        public bool UsingLocal { get; private set; }
        /// <summary>
        /// 执行功能文件是否释放
        /// </summary>
        public bool ResovleSigned { get; private set; }
        /// <summary>
        /// 指定的程序集工作空间
        /// </summary>
        public DirectoryInfo TransferCoreWorkspace { get; private set; }
        /// <summary>
        /// 指定的数据转换文件根目录
        /// </summary>
        public DirectoryInfo DataRootDirectory { get; private set; }
        /// <summary>
        /// 执行Bat文件目录
        /// </summary>
        public DirectoryInfo CommandBatDirectory { get; private set; }
        /// <summary>
        /// StatTransfer Tool 的目录
        /// </summary>
        public DirectoryInfo StatTransferToolDirectory { get; private set; }
        internal StatTransferManager(bool hasSTTool)
        {
            TransferCoreWorkspace = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            DataRootDirectory = TransferCoreWorkspace.CreateSubdirectory(@"DataWorkspace");
            var cmdResourceResovle = new CmdResourceResovle(this);
            ResovleSigned = true;
            _exePath = Path.Combine(CommandBatDirectory.FullName, cmdResourceResovle.CmdBatFileName);
            _stexePath = Path.Combine(StatTransferToolDirectory.FullName, cmdResourceResovle.SToolFileName);
            UsingLocal = hasSTTool;
        }
        public bool Excute(string zipFileName)
        {
            var tempDirectory = CreateRandomTempDirectory();
            return Excute(tempDirectory, zipFileName);
        }
        public bool Excute(DirectoryInfo cvsAndSasDirectory, string zipFileName)
        {
            return Excute(cvsAndSasDirectory, DataRootDirectory, zipFileName);
        }
        public bool Excute(DirectoryInfo cvsAndSasDirectory, DirectoryInfo zipFileDirectory, string zipFileName)
        {
            try
            {
                StringBuilder arguments = new StringBuilder();
                arguments.AppendFormat("{0} ", cvsAndSasDirectory.FullName);
                arguments.AppendFormat("{0} ", zipFileDirectory.FullName);
                arguments.AppendFormat("{0} ", zipFileName);
                if (UsingLocal)
                {
                    arguments.AppendFormat("{0} ", _stexePath);
                }
                using (Process ps = new Process())
                {
                    ProcessStartInfo processStartInfo = new ProcessStartInfo();
                    processStartInfo.FileName = _exePath;
                    processStartInfo.WorkingDirectory = Path.GetDirectoryName(_exePath);
                    processStartInfo.UseShellExecute = true;
                    processStartInfo.CreateNoWindow = true;
                    processStartInfo.Arguments = arguments.ToString();
                    ps.StartInfo = processStartInfo;
                    ps.Start();
                    ps.WaitForExit();
                }
                //zip source file to zip file 
                //file type is : *.sas7bdat  ;  *.xpt
                FastZip fastZip = new FastZip();
                bool recurse = true;
                string filter = @"\.sas7bdat$;\.xpt$";//filter any files at all
                string zipFilePath=Path.Combine(zipFileDirectory.FullName,string.Format("{0}.zip", zipFileName));
                fastZip.CreateZip(zipFilePath, cvsAndSasDirectory.FullName,recurse,filter);
                return true;
            }
            catch
            {
                return false;
            }
        }
        public DirectoryInfo CreateRandomTempDirectory()
        {
            //临时文件夹名,默认我用时间戳
            string randomDirectoryName = DateTime.Now.ToString("yyyyMMddHHmmssffff");
            return DataRootDirectory.CreateSubdirectory(randomDirectoryName);
        }
        public void DelateRandomTempDirectory(DirectoryInfo directoryInfo)
        {
            directoryInfo.Delete(true);
        }
        /// <summary>
        /// 释放CmdResource资源
        /// </summary>
        class CmdResourceResovle
        {
            private const string _cmdTokenPath = "StatTransfer-bat";
            private const string _exeTokenPath = "StatTransfer-9.0.3";
            private readonly string assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
            public string CmdBatFileName
            {
                get
                {
                    return "ProduceSASDataFormCVS.cmd";
                }
            }
            public string SToolFileName
            {
                get
                {
                    return "st.exe";
                }
            }
            public CmdResourceResovle(StatTransferManager statTransferManager)
            {
                statTransferManager.StatTransferToolDirectory = Reslve(_exeTokenPath, statTransferManager.TransferCoreWorkspace);
                statTransferManager.CommandBatDirectory = Reslve(_cmdTokenPath, statTransferManager.TransferCoreWorkspace);
            }
            private DirectoryInfo Reslve(string embeddedToken, DirectoryInfo directoryInfo)
            {
                string embeddedResource = string.Format("{0}.{1}.{2}.{3}", assemblyName,"Resources", embeddedToken, "zip");
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(embeddedResource);
                using (var zipArchive = new ZipArchive(stream))
                {
                    if (!directoryInfo.Exists)
                        directoryInfo.Create();
                    var extractDirectory = directoryInfo.GetDirectories(embeddedToken).FirstOrDefault();
                    if (extractDirectory == null)
                        zipArchive.ExtractToDirectory(directoryInfo.FullName);
                }
                return directoryInfo.GetDirectories(embeddedToken).FirstOrDefault();
            }
        }
    }
}
