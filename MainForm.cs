using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Xml;
using System.Collections;
using Ionic.Zip;
using System.Diagnostics;
using System.Runtime.InteropServices;
using CE.iPhone.PList;
using CE.iPhone.PList.Internal;
using URLSchemeViewer.ios;

namespace URLSchemeViewer
{
    public partial class MainForm : Form
    {
        private string _fileName = "";
        private string unzip_path = "";

        public MainForm()
        {
            InitializeComponent();
            deleteTempFile();
            string temp = System.Environment.GetEnvironmentVariable("TEMP");
            DirectoryInfo info = new DirectoryInfo(temp);
            unzip_path = info.FullName + "\\urlschemeviewer\\";

            dataGridView.AllowUserToAddRows = false;
            dataGridView.Rows.Clear();
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
                PListSchemeViewer viewer = new PListSchemeViewer(unzip_path);
                viewer.getURLScheme(_fileName, (List<DataGridCell> list) =>
                {
                    if (list == null || list.Count == 0)
                    {
                        MessageBox.Show("文件中没有url Scheme。");
                    }
                    else
                    {
                        addRows(list);
                    }
                });
            }
        }

        private void menuOpenPlist_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Info文件|*.plist|所有文件|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _fileName = dlg.FileName;
                PListSchemeViewer viewer = new PListSchemeViewer(unzip_path);
                List<DataGridCell> list = viewer.readPlist(_fileName);
                if (list == null || list.Count == 0)
                {
                    MessageBox.Show("文件中没有url Scheme。");
                }
                else
                {
                    addRows(list);
                }
            }
        }




        //////////////////////////////////////////////////////////////////////////
        
        private void menuOpenApk_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Android安装包|*.apk;*.zip|所有文件|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _fileName = dlg.FileName;
                AndroidPackageViewer viewer = new AndroidPackageViewer(unzip_path);
                viewer.getPackageName(_fileName, (string name) =>
                {
                    if (name == null)
                    {
                        MessageBox.Show("文件中没有包名。");
                    }
                    else
                    {
                        addRow(new DataGridCell("-", "-", name));
                    }
                });
            }
        }

        private void menuOpenManifestXML_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "AndroidManifest|*.xml|所有文件|*.*";
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _fileName = dlg.FileName;
                AndroidPackageViewer viewer = new AndroidPackageViewer(unzip_path);
                string name = viewer.readAndroidManifestXML(_fileName);
                if (name == null)
                {
                    MessageBox.Show("文件中没有包名。");
                }
                else
                {
                    addRow(new DataGridCell("-", "-", name));
                }
            }
        }




        //////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// 将节点信息显示在界面上。
        /// </summary>
        /// <param name="list"></param>
        private void showURLScheme(ArrayList list)
        {
            if (list == null || list.Count == 0)
            {
                MessageBox.Show("文件中并未包含url scheme信息。");
                return;
            }

            textBox.Text = "";
            foreach (string str in list)
            {
                textBox.Text += str + "\r\n";
            }
        }

        private void addRow(DataGridCell cell)
        {
            int ID = dataGridView.Rows.Count + 1;
            string[] row = new string[] {ID.ToString(), cell.bundleID, cell.executeName, cell.scheme};
            dataGridView.Rows.Add(row);
            dataGridView.Rows[0].ReadOnly = false;
        }

        private void addRows(List<DataGridCell> list)
        {
            foreach (DataGridCell cell in list)
            {
                addRow(cell);
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

        private void labInfo_Click(object sender, EventArgs e)
        {
            MessageBox.Show("这是一个查看iOS和Android软件安装包信息的程序。感谢以下开源项目的作者：\nChristian Ecker(iphone-plist-net)\nClaud Xiao(AxmlParser)", "URLSchemeViewer v"+Application.ProductVersion);
        }

        private void dataGridView_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            this.dataGridView.BeginEdit(false);
        }
    }
}
