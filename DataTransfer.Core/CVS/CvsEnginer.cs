using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Concurrent;
using System.Reflection;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace DataTransfer.Core.CVS
{
    /// <summary>
    /// CVS文件驱动器
    /// </summary>
    public class CvsEnginer
    {
        private static ConcurrentDictionary<Type, PropertyResolver> PoolResolves = new ConcurrentDictionary<Type, PropertyResolver>();
        /// <summary>
        /// I don't want to constructor function.
        /// </summary>
        private CvsEnginer() { }
        public static bool TurnCollectionToCsv<T>(ICollection<T> collections,string fileName) 
            where T:class
        {
            bool success = true;
            try
            {
                //go into check the file path and name.
                if (!CheckFileName(fileName))
                    return success = false;
                //go into turn cvs file. 
                var type = typeof(T);
                if (!PoolResolves.Keys.Contains(type))
                    PoolResolves.TryAdd(type, new PropertyResolver(type));
                PropertyResolver propertyResolver=null;
                PoolResolves.TryGetValue(type, out propertyResolver);
                if (propertyResolver == null)
                    return success = false;
                //create file 
                CreateDirectory(fileName);
                //every time will do create an new csv file
                using (StreamWriter sw = new StreamWriter(fileName,false,Encoding.Default))
                {
                    StringBuilder columnNames = new StringBuilder();
                    foreach (PropertyInfo item in propertyResolver.Properties)
                    {
                        columnNames.Append(item.Name);
                        columnNames.Append(",");
                    }
                    sw.WriteLine(columnNames.ToString());
                    StringBuilder columnValues = new StringBuilder();
                    foreach (var item in collections)
                    {
                        columnValues.Clear();
                        foreach (var getValueFunction in propertyResolver.PropertyGetValueFunctions)
                        {
                            columnValues.Append(getValueFunction.Value(item));
                            columnValues.Append(",");
                        }
                        sw.WriteLine(columnValues.ToString());
                    }
                }

            }catch(Exception ex)
            {
                success = false;
            }
            return success;
        }
        /// <summary>
        /// 检查CVS文件名是否有效
        /// </summary>
        /// <param name="fileName">完整路径的文件名称</param>
        /// <returns>文件存在返回false,文件不存在返回true</returns>
        private static bool CheckFileName(string fileName)
        {
            if (fileName.Trim() == "")
                return false;
            if (File.Exists(fileName))
                return false;
            int extensionSplit= fileName.LastIndexOf('.');
            string extensionType = fileName.Substring(extensionSplit);
            return extensionType == ".csv";
        }
        private static void CreateDirectory(string fileName)
        {
            string directory = fileName.Substring(0,fileName.LastIndexOf('\\'));
            Directory.CreateDirectory(directory);
        }
    }
}
