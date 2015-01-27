using System;
using System.Collections.Generic;
using System.Text;
using Ionic.Zip;
using System.Collections;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using System.IO;


namespace URLSchemeViewer
{
    class AndroidPackageViewer
    {
        EventHandler<ExtractProgressEventArgs> eventHandler;
        Action<string> unzipCallback;
        string unzip_path;
        string xml_path;

        public AndroidPackageViewer(string basePath)
        {
            unzip_path = basePath + "android\\";
            eventHandler = new EventHandler<ExtractProgressEventArgs>(zip_ExtractProgress);
        }

        public void getPackageName(string fileName, Action<string> callback)
        {
            unzipCallback = callback;
            unzipApk(fileName);
        }

        /// <summary>
        /// 将apk中的AndroidManifest.xml解压到系统临时目录
        /// </summary>
        /// <param name="fileName"></param>
        private void unzipApk(string fileName)
        {
            using (ZipFile zip = new ZipFile(fileName))
            {
                zip.ExtractProgress += eventHandler;
                foreach (ZipEntry e in zip.Entries)
                {
                    if (e.FileName.Contains("AndroidManifest.xml"))
                    {
                        xml_path = unzip_path + e.FileName;
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
                ((ZipFile)sender).ExtractProgress -= eventHandler;
                string packageName = readAndroidManifestXML(xml_path);
                if (packageName == null || packageName.Length == 0)
                {
                    unzipCallback(null);
                }
                else
                {
                    unzipCallback(packageName);
                }
            }
        }

        [DllImport("AXMLParser.dll", CharSet = CharSet.Ansi)]
        public extern static int AxmlToXml(ref IntPtr outbuf, ref uint outsize, byte[] inbuf, uint insize);

        public string readAndroidManifestXML(string fileName)
        {
            IntPtr outbuf = IntPtr.Zero;
            IntPtr outbuf_free = outbuf;
            uint outsize = 0;
            byte[] inbuf = File.ReadAllBytes(fileName);
            uint insize = (uint)inbuf.Length;
            string xml = null;
            try
            {
                AxmlToXml(ref outbuf, ref outsize, inbuf, insize);
                xml = Marshal.PtrToStringAnsi(outbuf);
                if (outsize == 0)
                {
                    throw new Exception("解析结果为空。");
                }
            }
            catch (Exception e)
            {
                xml = null;
                MessageBox.Show(e.Message, "文件解析失败");
            }
            finally
            {
            }
            string name = getPackageNameFromXML(xml);
            return name;
        }

        public string getPackageNameFromXML(string xmlContent)
        {
            if (xmlContent == null)
            {
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlContent);
            XmlElement root = doc.DocumentElement;
            XmlNode node = root.SelectSingleNode("//manifest[@package]");
            if (node == null)
            {
                return null;
            }
            else
            {
                string packageName = node.Attributes[@"package"].Value;
                return packageName;
            }
        }
    }
}
