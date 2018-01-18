using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using DataTransfer.Core.CVS;
using System.IO;
using DataTransfer.Core.SAS;

namespace DataTransfer.Core.Test
{
    /// <summary>
    /// 数据转换功能测试
    /// </summary>
    [TestFixture]
    public class TransferTest
    {
        private List<Subject> subjectExports;
        [SetUp]
        public void Init()
        {
            subjectExports = new List<Subject>()
            {
                new Subject()
                {
                    UserName = "陈也",
                    Age = 25,
                    Gender = "男",
                    Address = "上海市",
                    Project = "饿了么",
                    Salary = 15000
                },
                new Subject()
                {
                    UserName = "丁丁",
                    Age = 29,
                    Gender = "男",
                    Address = "上海市",
                    Project = "易果生鲜",
                    Salary = 22000
                },
                new Subject()
                {
                    UserName = "小小",
                    Age = 23,
                    Gender = "女",
                    Address = "上海市",
                    Project = "唯品会",
                    Salary = 28000
                },
                new Subject()
                {
                    UserName = "袁一凡",
                    Age = 31,
                    Gender = "男",
                    Address = "北京市",
                    Project = "京东商城",
                    Salary = 45000
                }
            };
        }
        /// <summary>
        /// List 集合数据转换为 CVS 文件
        /// </summary>
        [Test]
        public void TransferListToCVS()
        {
            string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CvsDirectory", string.Format("{0}.csv",DateTime.Now.ToString("yyyyMMDDHHmmssffff")));
            bool turnResult = CvsEnginer.TurnCollectionToCsv<Subject>(subjectExports, fileName);

            Assert.AreEqual(true, turnResult);
        }
        /// <summary>
        /// CVS 文件转换为 sas7bdat 与  xpt 文件
        /// </summary>
        [Test]
        public void TransferCVSToSAS()
        {
            var transferManager = StatTransferManagerFactory.GetAssemblySTM();
            var cvsDataDirectory = transferManager.CreateRandomTempDirectory();
            //数据集合转cvs
            string fileName = Path.Combine(cvsDataDirectory.FullName,string.Format("{0}.csv", DateTime.Now.ToString("yyyyMMDDHHmmssffff")));
            bool turnResult = CvsEnginer.TurnCollectionToCsv<Subject>(subjectExports, fileName);
            //cvs转为sas
            bool turnSasOK=false;
            if (turnResult)
            {                
                turnSasOK= transferManager.Excute(cvsDataDirectory, "TestFirst");
                transferManager.DelateRandomTempDirectory(cvsDataDirectory);
            }
            Assert.AreEqual(true, turnSasOK);
        }
        /// <summary>
        /// 主题
        /// </summary>
        public class Subject
        {
            /// <summary>
            /// 姓名
            /// </summary>
            [ColumnIndex(Index = 1)]
            public string UserName { get; set; }
            /// <summary>
            /// 年龄
            /// </summary>
            [ColumnIndex(Index = 3)]
            public int Age { get; set; }
            /// <summary>
            /// 性别
            /// </summary>
            [ColumnIndex(Index = 2)]
            public string Gender { get; set; }
            /// <summary>
            /// 住址
            /// </summary>
            [ColumnIndex(Index = 4)]
            public string Address { get; set; }
            /// <summary>
            /// 薪水
            /// </summary>
            [ColumnIndex(Index = 5)]
            public int Salary { get; set; }
            /// <summary>
            /// 当前参与的项目
            /// </summary>
            [ColumnIndex(Index = 6)]
            public string Project { get; set; }
        }
    }
    /// <summary>
    /// 其他常用功能测试
    /// </summary>
    [TestFixture]
    public class OtherFunctionTest
    {
        [Test]
        public void ToListMustBeInstanceToReturn()
        {
            string test = "abcdef";
            var list = test.Where(p => p == 'Z').ToList();
            Assert.AreNotEqual(list, null);
        }
    }
}
