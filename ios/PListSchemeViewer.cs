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

        /// <summary>
        /// 执行转码
        /// </summary>
        /// <param name="fileName"></param>
        private List<DataGridCell> decodeAndReadPlist(string fileName)
        {
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
        }

        /// <summary>
        /// 以xml的方式读取info.plist并且提取url scheme节点的信息。
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public List<DataGridCell> readPlist(string fileName)
        {
            PListRoot plist = PListRoot.Load(fileName);
            // 非二进制plist也可以解析。
            //if (plist.Format == PListFormat.Binary)
            //{
                return decodeAndReadPlist(fileName);
            //}

            //List<DataGridCell> result = new List<DataGridCell>();
            //string buf = File.ReadAllText(fileName);
            //result = readPlistContent(buf);
            //return result;
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
                XmlNode nodeDict = root.SelectSingleNode("//dict");
                XmlNode nodeExec = nodeDict.SelectSingleNode("//CFBundleExecutable");

                XmlNode nodeID = nodeDict.SelectSingleNode("//CFBundleIdentifier");

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
