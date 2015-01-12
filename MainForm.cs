using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Collections;
using Ionic.Zip;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace URLSchemeViewer
{
    public partial class MainForm : Form
    {
        private string _fileName = "";
        private string unzip_path = "";
        private string plist_path = "";
        private string plutil_path = "";
        private bool _needDecode;

        public MainForm()
        {
            InitializeComponent();
            deleteTempFile();
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            DirectoryInfo info = new DirectoryInfo(temp);
            unzip_path = info.FullName + "\\urlschemeviewer\\";
            plutil_path = unzip_path + "plutil";
            Directory.CreateDirectory(plutil_path);
        }

        private void menuQuit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void menuOpen_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "iOS安装包|*.ipa;*.zip|所有文件|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _fileName = dlg.FileName;
                unzipIPA(_fileName);
            }
        }

        private void menuOpenPlist_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Info文件|*.plist|所有文件|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _fileName = dlg.FileName;
                releasePlutil();
                decodeAndReadPlist(_fileName);
            }
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
                if (_needDecode)
                {
                    releasePlutil();
                    decodeAndReadPlist(plist_path);
                }
                else
                {
                    ArrayList list = readPlist(plist_path);
                    showURLScheme(list);
                }
            }
        }

        /// <summary>
        /// info.plist需要转码。所以这里将转码工具释放到系统临时目录
        /// </summary>
        void releasePlutil()
        {
            byte[] ASL = global::URLSchemeViewer.Properties.Resources.ASL;
            byte[] CFNetwork = global::URLSchemeViewer.Properties.Resources.CFNetwork;
            byte[] CoreFoundation = global::URLSchemeViewer.Properties.Resources.CoreFoundation;
            byte[] Foundation = global::URLSchemeViewer.Properties.Resources.Foundation;
            byte[] icudt46 = global::URLSchemeViewer.Properties.Resources.icudt46;

            byte[] libdispatch = global::URLSchemeViewer.Properties.Resources.libdispatch;
            byte[] libicuin = global::URLSchemeViewer.Properties.Resources.libicuin;
            byte[] libicuuc = global::URLSchemeViewer.Properties.Resources.libicuuc;
            byte[] libtidy = global::URLSchemeViewer.Properties.Resources.libtidy;
            byte[] libxml2 = global::URLSchemeViewer.Properties.Resources.libxml2;
            
            byte[] objc = global::URLSchemeViewer.Properties.Resources.objc;
            byte[] plutil = global::URLSchemeViewer.Properties.Resources.plutil;
            byte[] pthreadVC2 = global::URLSchemeViewer.Properties.Resources.pthreadVC2;
            byte[] SQLite3 = global::URLSchemeViewer.Properties.Resources.SQLite3;

            Dictionary<string, byte[]> list = new Dictionary<string, byte[]>();
            list.Add("ASL", ASL);
            list.Add("CFNetwork", CFNetwork);
            list.Add("CoreFoundation", CoreFoundation);
            list.Add("Foundation", Foundation);
            list.Add("icudt46", icudt46);

            list.Add("libdispatch", libdispatch);
            list.Add("libicuin", libicuin);
            list.Add("libicuuc", libicuuc);
            list.Add("libtidy", libtidy);
            list.Add("libxml2", libxml2);

            list.Add("objc", objc);
            list.Add("pthreadVC2", pthreadVC2);
            list.Add("SQLite3", SQLite3);

            foreach (string name in list.Keys)
            {
                string path = plutil_path + "\\" + name + ".dll";
                if (!File.Exists(path))
                {
                    FileStream fsObj = new FileStream(path, FileMode.CreateNew);
                    fsObj.Write(list[name], 0, list[name].Length);
                    fsObj.Close();
                }
            }
            if (!File.Exists(plutil_path + "\\plutil.exe"))
            {
                FileStream plutilStream = new FileStream(plutil_path + "\\plutil.exe", FileMode.CreateNew);
                plutilStream.Write(plutil, 0, plutil.Length);
                plutilStream.Close();
            }
        }

        /// <summary>
        /// 执行转码
        /// </summary>
        /// <param name="fileName"></param>
        private void decodeAndReadPlist(string fileName)
        {
            Process process = new Process();//创建进程对象  
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = plutil_path;
            startInfo.FileName = plutil_path + "\\plutil.exe";//设定需要执行的命令  
            startInfo.Arguments = "-convert xml1 " + fileName;
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
                    process.Kill();
                    process.Close();
                }
                ArrayList list = readPlist(fileName);
                showURLScheme(list);
            }

        }

        /// <summary>
        /// 以xml的方式读取info.plist并且提取url scheme节点的信息。
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private ArrayList readPlist(string fileName)
        {
            ArrayList result = new ArrayList();
            try
            {
                string buf = File.ReadAllText(fileName);

                // begin 删除xml中第二个节点，否则未联网的情况下无法解析xml
                int index = buf.IndexOf("<!DOCTYPE plist");
                if (index > 0)
                {
                    string node1 = buf.Substring(0, index - 1);
                    string others = buf.Substring(index);
                    int index2 = others.IndexOf(">");
                    if (index2 > 0)
                    {
                        others = others.Substring(index2+1);
                        buf = node1 + others;
                    }
                }
                // end ====================================================

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(buf);
                XmlElement root = doc.DocumentElement;
                XmlNodeList nodes = root.SelectNodes("//key[text()='CFBundleURLSchemes']");
                if (nodes.Count > 0)
                {
                    XmlNode node = nodes[0].NextSibling;
                    for (int i = 0; i < node.ChildNodes.Count; i++)
                    {
                        result.Add(node.ChildNodes[i].InnerText);
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

        /// <summary>
        /// 将节点信息显示在界面上。
        /// </summary>
        /// <param name="list"></param>
        private void showURLScheme(ArrayList list)
        {
            textBox.Text = "";
            for (int i = 0; i < list.Count; i++)
            {
                textBox.Text += list[i] + "\r\n";
            }
        }

        /// <summary>
        /// 删除临时文件
        /// </summary>
        private void deleteTempFile()
        {
            try
            {
                if (unzip_path.Length < 2)
                {
                    return;
                }

                string path = unzip_path;
                if (path.Substring(path.Length - 2).Equals("\\\\"))
                {
                    path = path.Substring(0, path.Length - 2);
                }

                System.IO.Directory.Delete(path, true);
            }
            catch
            {

            }
            finally
            {

            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            deleteTempFile();
        }

        
    }
}
