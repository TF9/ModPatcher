using BytePatternMatch;
using ModPatcher.Properties;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace ModPatcher
{
	public class Form1 : Form
	{
		private Stopwatch sw = new Stopwatch();
		private PatchAspect mp;
		private int nmbFiles;
		private IContainer components;
		private SplitContainer splitContainer1;
		private FolderBrowserDialog folderBrowserDialog1;
		private Button button1;
		private TextBox textBox1;
		private Button button2;
		private TreeView treeView1;
		private Label label1;
		private TextBox textBox2;
		private Label label2;

		public Form1()
		{
			InitializeComponent();
			textBox1.Text = Settings.Default.LastPath;
			textBox2.Text = Settings.Default.Extension;
			folderBrowserDialog1.SelectedPath = textBox1.Text;
			mp = new PatchAspect();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			folderBrowserDialog1.ShowDialog();
			textBox1.Text = folderBrowserDialog1.SelectedPath;
			Settings.Default.LastPath = textBox1.Text;
			Settings.Default.Save();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			nmbFiles = 0;
			sw.Reset();
			sw.Start();
			treeView1.Nodes.Clear();
			DirectoryInfo directoryInfo = new DirectoryInfo(textBox1.Text);
			BuildTree(directoryInfo, treeView1.Nodes);
			sw.Stop();
			label1.Text = $"{nmbFiles} files modified in {sw.Elapsed.TotalSeconds} seconds";
			MessageBox.Show(this, "Finished.", "Mod-Patcher", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
		}

		private void BuildTree(DirectoryInfo directoryInfo, TreeNodeCollection addInMe)
		{
			TreeNode treeNode = addInMe.Add(directoryInfo.Name);
			FileInfo[] files = directoryInfo.GetFiles();
			foreach (FileInfo fileInfo in files)
			{
				string b = Path.GetExtension(fileInfo.Name).ToLower();
				string str = "";
				if ("." + Settings.Default.Extension.ToLower() == b)
				{
					int num = 0;
					FileStream fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
					num = mp.PatchAspectRatio16_9(fileStream);
					fileStream.Close();
					str = $" -->> {num} tags modified";
					nmbFiles++;
				}
				TreeNode treeNode2 = new TreeNode(fileInfo.Name + str);
				treeNode.Nodes.Add(treeNode2);
				treeView1.ExpandAll();
				treeView1.SelectedNode = treeNode2;
				treeView1.SelectedNode.EnsureVisible();
				Application.DoEvents();
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			foreach (DirectoryInfo directoryInfo2 in directories)
			{
				BuildTree(directoryInfo2, treeNode.Nodes);
			}
		}

		private void textBox2_TextChanged(object sender, EventArgs e)
		{
			Settings.Default.Extension = textBox2.Text;
			Settings.Default.Save();
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null)
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			splitContainer1 = new System.Windows.Forms.SplitContainer();
			label2 = new System.Windows.Forms.Label();
			textBox2 = new System.Windows.Forms.TextBox();
			label1 = new System.Windows.Forms.Label();
			button2 = new System.Windows.Forms.Button();
			button1 = new System.Windows.Forms.Button();
			textBox1 = new System.Windows.Forms.TextBox();
			treeView1 = new System.Windows.Forms.TreeView();
			folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
			((System.ComponentModel.ISupportInitialize)splitContainer1).BeginInit();
			splitContainer1.Panel1.SuspendLayout();
			splitContainer1.Panel2.SuspendLayout();
			splitContainer1.SuspendLayout();
			SuspendLayout();
			splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			splitContainer1.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			splitContainer1.Location = new System.Drawing.Point(0, 0);
			splitContainer1.Name = "splitContainer1";
			splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			splitContainer1.Panel1.Controls.Add(label2);
			splitContainer1.Panel1.Controls.Add(textBox2);
			splitContainer1.Panel1.Controls.Add(label1);
			splitContainer1.Panel1.Controls.Add(button2);
			splitContainer1.Panel1.Controls.Add(button1);
			splitContainer1.Panel1.Controls.Add(textBox1);
			splitContainer1.Panel2.Controls.Add(treeView1);
			splitContainer1.Size = new System.Drawing.Size(532, 440);
			splitContainer1.SplitterDistance = 57;
			splitContainer1.TabIndex = 1;
			label2.AutoSize = true;
			label2.Location = new System.Drawing.Point(344, 16);
			label2.Name = "label2";
			label2.Size = new System.Drawing.Size(87, 13);
			label2.TabIndex = 5;
			label2.Text = "Dateierweiterung";
			textBox2.Location = new System.Drawing.Point(295, 13);
			textBox2.Name = "textBox2";
			textBox2.Size = new System.Drawing.Size(43, 20);
			textBox2.TabIndex = 4;
			textBox2.Text = "MOD";
			textBox2.TextChanged += new System.EventHandler(textBox2_TextChanged);
			label1.AutoSize = true;
			label1.Location = new System.Drawing.Point(14, 38);
			label1.Name = "label1";
			label1.Size = new System.Drawing.Size(16, 13);
			label1.TabIndex = 3;
			label1.Text = "...";
			button2.Location = new System.Drawing.Point(438, 10);
			button2.Name = "button2";
			button2.Size = new System.Drawing.Size(75, 23);
			button2.TabIndex = 2;
			button2.Text = "Start";
			button2.UseVisualStyleBackColor = true;
			button2.Click += new System.EventHandler(button2_Click);
			button1.Location = new System.Drawing.Point(259, 11);
			button1.Name = "button1";
			button1.Size = new System.Drawing.Size(24, 21);
			button1.TabIndex = 1;
			button1.Text = "...";
			button1.UseVisualStyleBackColor = true;
			button1.Click += new System.EventHandler(button1_Click);
			textBox1.Location = new System.Drawing.Point(12, 12);
			textBox1.Name = "textBox1";
			textBox1.Size = new System.Drawing.Size(247, 20);
			textBox1.TabIndex = 0;
			treeView1.Dock = System.Windows.Forms.DockStyle.Fill;
			treeView1.Location = new System.Drawing.Point(0, 0);
			treeView1.Name = "treeView1";
			treeView1.Size = new System.Drawing.Size(532, 379);
			treeView1.TabIndex = 0;
			base.AutoScaleDimensions = new System.Drawing.SizeF(6f, 13f);
			base.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			base.ClientSize = new System.Drawing.Size(532, 440);
			base.Controls.Add(splitContainer1);
			base.Name = "Form1";
			Text = "MOD 16/9 Patcher";
			splitContainer1.Panel1.ResumeLayout(false);
			splitContainer1.Panel1.PerformLayout();
			splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer1).EndInit();
			splitContainer1.ResumeLayout(false);
			ResumeLayout(false);
		}
	}
}
