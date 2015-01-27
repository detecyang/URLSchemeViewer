using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using Ionic.Zip;
using System.Windows.Forms;
using CE.iPhone.PList;
using System.IO;
using System.Xml;
using CE.iPhone.PList.Internal;

namespace URLSchemeViewer.ios
{
    class PListSchemeViewer
    {
        EventHandler<ExtractProgressEventArgs> eventHandler;
        Action<List<DataGridCell>> unzipCallback;
        string unzip_path;
        string plist_path;
        bool _needDecode;

        public PListSchemeViewer(string basePath)
        {
            unzip_path = basePath + "ios\\";
            eventHandler = new EventHandler<ExtractProgressEventArgs>(zip_ExtractProgress);
        }

        public void getURLScheme(string fileName, Action<List<DataGridCell>> callback)
        {
            unzipCallback = callback;
            unzipIPA(fileName);
        }

        /// <summary>
        /// 将ipa中的info.plist解压到系统临时目录
        /// </summary>
        /// <param name="fileName"></param>
        private void unzipIPA(string fileName)
        {
            using (ZipFile zip = new ZipFile(fileName))
            {
                zip.ExtractProgress += new EventHandler<ExtractProgressEventArgs>(zip_ExtractProgress);
                foreach (ZipEntry e in zip.Entries)
                {
                    if (e.FileName.Contains("/Info.plist"))
                    {
                        plist_path = unzip_path + e.FileName;
                        _needDecode = !e.IsText;
                        e.Extract(unzip_path, ExtractExistingFileAction.OverwriteSilently);
                        return;
                    }
                }
                MessageBox.Show("这不是一个标准的安装包。");
            }
        }

        void zip_ExtractProgress(object sender, ExtractProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Extracting_AfterExtractEntry)
            {
                List<DataGridCell> list = null;
                if (_needDecode)
                {
                    list = decodeAndReadPlist(plist_path);
                }
                else
                {
                    list = readPlist(plist_path);
                }
                unzipCallback(list);
            }
        }


#region deprecated
        ///// <summary>
        ///// info.plist需要转码。所以这里将转码工具释放到系统临时目录
        ///// </summary>
        //void releasePlutil()
        //{
        //    byte[] ASL = global::URLSchemeViewer.Properties.Resources.ASL;
        //    byte[] CFNetwork = global::URLSchemeViewer.Properties.Resources.CFNetwork;
        //    byte[] CoreFoundation = global::URLSchemeViewer.Properties.Resources.CoreFoundation;
        //    byte[] Foundation = global::URLSchemeViewer.Properties.Resources.Foundation;
        //    byte[] icudt46 = global::URLSchemeViewer.Properties.Resources.icudt46;

        //    byte[] libdispatch = global::URLSchemeViewer.Properties.Resources.libdispatch;
        //    byte[] libicuin = global::URLSchemeViewer.Properties.Resources.libicuin;
        //    byte[] libicuuc = global::URLSchemeViewer.Properties.Resources.libicuuc;
        //    byte[] libtidy = global::URLSchemeViewer.Properties.Resources.libtidy;
        //    byte[] libxml2 = global::URLSchemeViewer.Properties.Resources.libxml2;

        //    byte[] objc = global::URLSchemeViewer.Properties.Resources.objc;
        //    byte[] plutil = global::URLSchemeViewer.Properties.Resources.plutil;
        //    byte[] pthreadVC2 = global::URLSchemeViewer.Properties.Resources.pthreadVC2;
        //    byte[] SQLite3 = global::URLSchemeViewer.Properties.Resources.SQLite3;

        //    Dictionary<string, byte[]> list = new Dictionary<string, byte[]>();
        //    list.Add("ASL", ASL);
        //    list.Add("CFNetwork", CFNetwork);
        //    list.Add("CoreFoundation", CoreFoundation);
        //    list.Add("Foundation", Foundation);
        //    list.Add("icudt46", icudt46);

        //    list.Add("libdispatch", libdispatch);
        //    list.Add("libicuin", libicuin);
        //    list.Add("libicuuc", libicuuc);
        //    list.Add("libtidy", libtidy);
        //    list.Add("libxml2", libxml2);

        //    list.Add("objc", objc);
        //    list.Add("pthreadVC2", pthreadVC2);
        //    list.Add("SQLite3", SQLite3);

        //    foreach (string name in list.Keys)
        //    {
        //        string path = plutil_path + "\\" + name + ".dll";
        //        if (!File.Exists(path))
        //        {
        //            FileStream fsObj = new FileStream(path, FileMode.CreateNew);
        //            fsObj.Write(list[name], 0, list[name].Length);
        //            fsObj.Close();
        //        }
        //    }
        //    if (!File.Exists(plutil_path + "\\plutil.exe"))
        //    {
        //        FileStream plutilStream = new FileStream(plutil_path + "\\plutil.exe", FileMode.CreateNew);
        //        plutilStream.Write(plutil, 0, plutil.Length);
        //        plutilStream.Close();
        //    }
        ///
#endregion

        /// <summary>
        /// 执行转码
        /// </summary>
        /// <param name="fileName"></param>
        private List<DataGridCell> decodeAndReadPlist(string fileName)
        {
#if PLUTIL
            #region deprecated
            Process process = new Process();//创建进程对象  
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = plutil_path;
            startInfo.FileName = plutil_path + "\\plutil.exe";//设定需要执行的命令  
            startInfo.Arguments = "-convert xml1 \"" + fileName + "\"";
            startInfo.UseShellExecute = false;//不使用系统外壳程序启动  
            startInfo.RedirectStandardInput = false;//不重定向输入  
            startInfo.RedirectStandardOutput = true; //重定向输出  
            startInfo.CreateNoWindow = true;//不创建窗口  
            process.StartInfo = startInfo;
            try
            {
                if (process.Start())//开始进程  
                {
                    process.WaitForExit(1000); //等待进程结束，等待时间为指定的毫秒
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "plist转码失败");
            }
            finally
            {
                if (process != null)
                {
                    process.Close();
                }
                ArrayList list = readPlist(fileName);
                showURLScheme(list);
            }
            #endregion
#else
            PListRoot plist = PListRoot.Load(fileName);
            PListDict dic = (PListDict)plist.Root;
            
            List<DataGridCell> result = new List<DataGridCell>();
            DataGridCell cell = new DataGridCell();

            if (dic.ContainsKey("CFBundleExecutable"))
            {
                PListString name = (PListString)dic["CFBundleIdentifier"];
                cell.bundleID = name.Value;
            }

            if (dic.ContainsKey("CFBundleExecutable"))
            {
                PListString name = (PListString)dic["CFBundleExecutable"];
                cell.executeName = name.Value;
            }

            string schemes = "";
            if (dic.ContainsKey("CFBundleURLTypes"))
            {
                PListArray array = (PListArray)dic["CFBundleURLTypes"];
                foreach (Dictionary<string, IPListElement> url in array)
                {
                    if (url.ContainsKey("CFBundleURLSchemes"))
                    {
                        array = (PListArray)url["CFBundleURLSchemes"];
                        foreach (PListString scheme in array)
                        {
                            schemes = schemes + scheme + "; ";
                        }
                    }
                }
            }
            cell.scheme = schemes;
            result.Add(cell);
 
            return result;
#endif
        }

        /// <summary>
        /// 以xml的方式读取info.plist并且提取url scheme节点的信息。
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public List<DataGridCell> readPlist(string fileName)
        {
            PListRoot plist = PListRoot.Load(fileName);
            if (plist.Format == PListFormat.Binary)
            {
                return decodeAndReadPlist(fileName);
            }

            List<DataGridCell> result = new List<DataGridCell>();
            string buf = File.ReadAllText(fileName);
            result = readPlistContent(buf);
            return result;
        }

        public List<DataGridCell> readPlistContent(string xmlText)
        {
            List<DataGridCell> result = new List<DataGridCell>();
            try
            {
                string buf = xmlText;
                // begin 删除xml中第二个节点，否则未联网的情况下无法解析xml
                int index = buf.IndexOf("<!DOCTYPE plist");
                if (index > 0)
                {
                    string node1 = buf.Substring(0, index - 1);
                    string others = buf.Substring(index);
                    int index2 = others.IndexOf(">");
                    if (index2 > 0)
                    {
                        others = others.Substring(index2 + 1);
                        buf = node1 + others;
                    }
                }
                // end ====================================================

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(buf);
                XmlElement root = doc.DocumentElement;

                XmlNode nodeExec = root.SelectSingleNode("//CFBundleExecutable");

                XmlNode nodeID = root.SelectSingleNode("//CFBundleIdentifier");

                XmlNodeList nodes = root.SelectNodes("//key[text()='CFBundleURLSchemes']");
                if (nodes.Count > 0)
                {
                    foreach (XmlNode node in nodes)
                    {
                        XmlNode subNode = node.NextSibling;
                        foreach (XmlNode strNode in subNode.ChildNodes)
                        {

                            result.Add(new DataGridCell(strNode.InnerText, nodeExec.InnerText, nodeID.InnerText));
                        }
                    }
                }
                else
                {
                    MessageBox.Show("文件中并未包含url scheme信息。");
                }

            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "plist解析失败");
            }
            finally
            {

            }
            return result;
        }
    }
}
