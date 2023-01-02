using System;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace EmlFileViewer {


    public partial class EmlViewer : Form {

        /// <summary>
        /// File currently open
        /// </summary>
        public volatile EmlFile currentFile = null;

        /// <summary>
        /// Currently loading file
        /// </summary>
        private volatile EmlFile loadingFile = null;

        /// <summary>
        /// Currently viewed sub-part of current file.
        /// </summary>
        private volatile EmlFile.Part currentFilePart = null;

        private String pathToPdf2TxtExe = "bin/pdftotext.exe";
        private BackgroundWorker emlLoaderWorker = new BackgroundWorker();


        public EmlViewer() {
            InitializeComponent();
            emlLoaderWorker.DoWork += emlLoaderWorker_DoWork;
            //emlLoaderWorker.ProgressChanged += emlLoaderWorker_ProgressChanged;
        }

        private void EmlViewer_Load(object sender, EventArgs e) {
            //MessageBox.Show("Loading " + sender.ToString() + "\r\n\r\n" + e.ToString());
        }

        public async void EmlViewer_ClearDisplay() {
            HideDefaultHeaders();
            webBrowser1.DocumentText = "";
            if (webBrowser1.Visible) {
                webBrowser1.Visible = false;
            }
            textBox1.Text = "";
            if (textBox1.Visible) {
                textBox1.Visible = false;
            }
            panel1.BackgroundImage = null;
            if (!panel1.Visible) {
                panel1.Visible = true;
            }
            if (progressBar1.Visible) {
                progressBar1.Visible = false;
                progressBar1.Value = 0;
            }
        }

        private void EmlViewer_CloseFile() {
            EmlViewer_ClearDisplay();
            partsTree.BeginUpdate();
            partsTree.Nodes.Clear();
            partsTree.EndUpdate();
            this.Text = "Eml Viewer";

            if (loadingFile != null) { // LOADING FILE AND NOT FINISHED
                loadingFile.Stop(); // STOP LOADING!
                if (!emlLoaderWorker.IsBusy) {
                    emlLoaderWorker.CancelAsync();
                }
                loadingFile = null;
            }
            if (currentFile != null) {
                currentFile.Dispose();
                currentFile = null;
            }
            if (currentFilePart != null) {
                currentFilePart.Dispose();
                currentFilePart = null;
            }
            GC.Collect();
        }



        public async void EmlViewer_OpenFile(String filename) {

            //MessageBox.Show("Opening file: " + filename);
            if (!filename.StartsWith("\\\\?\\")) {
                filename = "\\\\?\\" + Path.GetFullPath(filename);
            }
            if (currentFile != null && String.Equals(currentFile.Filename, filename)) {
                return; // ALREADY LOADED THIS ONE
            }
            if (loadingFile != null && String.Equals(loadingFile.Filename, filename)) {
                return; // CURRENTLY LOADING THIS ONE
            }
            if (loadingFile != null) { // LOADING FILE AND NOT FINISHED
                loadingFile.Stop(); // STOP LOADING!
                if (!emlLoaderWorker.IsBusy) {
                    emlLoaderWorker.CancelAsync();
                }
                loadingFile = null;
            }
            if (currentFile != null) {
                currentFile.Dispose();
                currentFile = null;
            }
            if (currentFilePart != null) {
                currentFilePart.Dispose();
                currentFilePart = null;
            }
            GC.Collect();
            loadingFile = new EmlFile(filename);
            HideDefaultHeaders();
            this.progressBar1.Visible = true;
            this.progressBar1.Value = 0;
            //MessageBox.Show("Thread Count 0: " + Process.GetCurrentProcess().Threads.Count);
            Task t = Task.Run(() => UpdateLoadProgressAsync());

            emlLoaderWorker.RunWorkerAsync();
            await t;
        }


        private async void emlLoaderWorker_DoWork(object sender, DoWorkEventArgs e) {
            if (loadingFile == null) {
                return;
            }
            String filename = loadingFile.Filename;
            Task<bool> decodeEmlTask = loadingFile.DecodeAsync();
            if (decodeEmlTask == null) {
                return;
            }
            bool got = await decodeEmlTask;
            if (!got) {
                MessageBox.Show("Unable to read EML file from task: " + filename);
                return;
            }
            if (loadingFile == null || !loadingFile.DecodedOkay) {
                MessageBox.Show("Unable to read EML file: " + filename);
                return;
            }
            currentFile = loadingFile;
            loadingFile = null;

            if (ControlInvokeRequired(panel1, () => ShowDefaultView())) return;
        }

        private void emlLoaderWorker_ProgressChanged(object sender, System.ComponentModel.ProgressChangedEventArgs e) {
            if (ControlInvokeRequired(progressBar1, () => ShowLoadProgress())) return;
            //MessageBox.Show("Loaded " + currentFile.DecodedSize + "/" + currentFile.Filesize);
        }

        private async Task UpdateLoadProgressAsync() {
            int loopCount = 0;
            while (loadingFile != null) {
                ControlInvokeRequired(progressBar1, () => ShowLoadProgress());
                //Thread.Sleep(100);
                loopCount++;
            }
            if (currentFile != null) {
            //    MessageBox.Show("Decoded " + decodedChars + " of " + fileChars + " = " + percentage);
            } else {
            //    MessageBox.Show("Looped " + loopCount + " times to " + percentage);
            }
        }

        private volatile int percentage;
        private volatile int decodedChars;
        private volatile int fileChars;
        private void ShowLoadProgress() {
            try {
                if (loadingFile != null) {
                    progressBar1.Value = (int)Math.Round((((double)loadingFile.DecodedSize) * 100.0) / ((double)loadingFile.Filesize));
                    percentage = progressBar1.Value;
                    //fileChars = (int)loadingFile.Filesize;
                    //decodedChars = (int)loadingFile.DecodedSize;
                }
            } catch (Exception notImportant) {
            }
        }

        /// <summary>
        /// Helper method to determin if invoke required, if so will rerun method on correct thread.
        /// if not do nothing.
        /// </summary>
        /// <param name="c">Control that might require invoking</param>
        /// <param name="a">action to preform on control thread if so.</param>
        /// <returns>true if invoke required</returns>
        public bool ControlInvokeRequired(Control c, Action a) {
            if (c.InvokeRequired) {
                c.Invoke(new MethodInvoker(delegate { a(); }));
                return true;
            } else {
                return false;
            }
        }


        public void ShowDefaultView() {
            //GC.Collect();
            //MessageBox.Show("Thread Count 3: " + Process.GetCurrentProcess().Threads.Count);
            if (currentFile == null || !currentFile.DecodedOkay) {
                EmlViewer_ClearDisplay();
                return;
            }
            loadingFile = null;
            //MessageBox.Show("Loaded " + currentFile.DecodedSize + "/" + currentFile.Filesize);
            //progressBar1.Value = (int)Math.Round(((double)currentFile.DecodedSize) * 100.0 / ((double)currentFile.Filesize));
            //this.progressBar1.Refresh();
            if (progressBar1.Visible) {
                progressBar1.Visible = false;
                progressBar1.Value = 0;
            }


            FileInfo fi = new FileInfo(currentFile.Filename);

            partsTree.BeginUpdate();
            partsTree.Nodes.Clear();
            partsTree.Nodes.Add(fi.Name);
            partsTree.Nodes[0].Nodes.Add("hg0", "Headers");
            if (currentFile.Headers != null && currentFile.Headers.Count > 0) {
                addPartsTreeHeaders("h0", partsTree.Nodes[0].Nodes[0], currentFile.Headers);
            }
            partsTree.Nodes[0].Nodes.Add("0.0", "Message");
            for (int i = 0; i < currentFile.Parts.Count; i++) {
                String typeLabel = getPartTypeLabel(currentFile.Parts[i]);
                partsTree.Nodes[0].Nodes[1].Nodes.Add("0." + i, "Part " + (i + 1) + (typeLabel != null && typeLabel.Length > 0 ? " (" + typeLabel + ")" : ""));
                populatePartsTreeRecursive("0." + i, partsTree.Nodes[0].Nodes[1].Nodes[i], currentFile.Parts[i]);
            }
            partsTree.Nodes[0].Expand();
            partsTree.Nodes[0].Nodes[1].Expand();
            partsTree.EndUpdate();

            this.Text = fi.Name;

            this.ShowDefaultHeaders();
            this.ShowDefaultMessage();

        }


        private void HideDefaultHeaders() {
            this.subjectHeaderLabel.Visible = false;
            this.subjectHeaderBox.Visible = false;
            this.subjectHeaderBox.Text = "";
            this.fromHeaderLabel.Visible = false;
            this.fromHeaderBox.Visible = false;
            this.fromHeaderBox.Text = "";
            this.dateHeaderLabel.Visible = false;
            this.dateHeaderBox.Visible = false;
            this.dateHeaderBox.Text = "";
            this.toHeaderLabel.Visible = false;
            this.toHeaderBox.Visible = false;
            this.toHeaderBox.Text = "";

        }

        private void ShowDefaultHeaders() {
            if (currentFile == null || !currentFile.DecodedOkay) {
                EmlViewer_CloseFile();
                return;
            }
            this.subjectHeaderLabel.Visible = true;
            this.subjectHeaderBox.Visible = true;
            this.subjectHeaderBox.Text = currentFile.GetHeaderValue("subject");
            this.fromHeaderLabel.Visible = true;
            this.fromHeaderBox.Visible = true;
            this.fromHeaderBox.Text = currentFile.GetHeaderValue("from");
            this.dateHeaderLabel.Visible = true;
            this.dateHeaderBox.Visible = true;
            this.dateHeaderBox.Text = currentFile.GetHeaderValue("date");
            this.toHeaderLabel.Visible = true;
            this.toHeaderBox.Visible = true;
            this.toHeaderBox.Text = currentFile.GetHeaderValue("to");

        }

        private void ShowDefaultMessage() {
            if (currentFile == null || !currentFile.DecodedOkay) {
                EmlViewer_CloseFile();
                return;
            }
            String str = currentFile.ToDebugString();
            panel1.Visible = false;
            textBox1.Text = str;
            textBox1.Visible = true;
            panel1.BackgroundImage = null;
            if (progressBar1.Visible) {
                progressBar1.Visible = false;
                progressBar1.Value = 0;
            }

        }


        #region PopulatePartTree
        private void populatePartsTreeRecursive(String prefix, TreeNode node, EmlFile.Part part) {
            int index = -1;
            node.ToolTipText = "Left click to view content, right click to view source.";
            if (part.Headers != null && part.Headers.Count > 0) {
                index++;
                node.Nodes.Add("hg" + prefix, "Headers");
                addPartsTreeHeaders("h" + prefix, node.Nodes[index], part.Headers);
                node.Nodes[index].ToolTipText = "Click to view headers.";
            }
            if (part.HasContent()) {
                index++;
                node.Nodes.Add(prefix, "Content");
                node.Nodes[index].ToolTipText = "Left click to view content, right click to view source.";
            }
            if (part.Subparts != null && part.Subparts.Count > 0) {
                index++;
                node.Nodes.Add(prefix, "Parts");
                for (int i = 0; i < part.Subparts.Count; i++) {
                    String typeLabel = getPartTypeLabel(part.Subparts[i]);
                    node.Nodes[index].Nodes.Add(prefix + '.' + i, "Part " + (i + 1) + (typeLabel != null && typeLabel.Length > 0 ? " (" + typeLabel + ")" : ""));
                    populatePartsTreeRecursive(prefix + '.' + i, node.Nodes[index].Nodes[i], part.Subparts[i]);
                }
                node.Nodes[index].Expand();
            }
            node.Expand();
        }

        private String getPartTypeLabel(EmlFile.Part part) {
            if (part.StartsWithContentType("text/html")) {
                return "HTML";
            }
            if (part.StartsWithContentType("text/")) {
                return "Text";
            }
            if (part.StartsWithContentType("image/")) {
                return "Image";
            }
            if (part.RegexMatchesHeader("content-type", ".*(?i)(application\\/).*(pdf).*")) {
                return "PDF";
            }
            if (part.StartsWithContentType("multipart/")) {
                return "MIME";
            }
            if (part.StartsWithContentType("message/rfc822")) {
                return "RFC822";
            }
            if (part.RegexMatchesHeader("content-type", ".*(?i)(application\\/).*(mp4)")) {
                return "MP4";
            }
            return null;
        }

        private void addPartsTreeHeaders(String prefix, TreeNode node, SortedDictionary<String, List<String>> Headers) {
            if (Headers == null) {
                return;
            }
            SortedDictionary<String, List<String>>.KeyCollection headerKeys = Headers.Keys;
            for (int i = 0; i < headerKeys.Count; i++) {
                node.Nodes.Add(prefix + "." + i, headerKeys.ElementAt(i));
                List<String> headerValues = Headers[headerKeys.ElementAt(i)];
                for (int j = 0; j < headerValues.Count; j++) {
                    node.Nodes[i].Nodes.Add(prefix + "." + i + "_" + j, headerValues[j]);
                }
            }
        }
        #endregion

        private void treeView1_NodeMouseHover(object sender, TreeNodeMouseHoverEventArgs e) {
        }

        private void treeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) {
            if (e.Button == MouseButtons.Right)
                MessageBox.Show(e.Node.Name);
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e) {
            if (currentFile == null || !currentFile.DecodedOkay) {
                EmlViewer_CloseFile();
                return;
            }
            if (progressBar1.Visible) {
                progressBar1.Visible = false;
                progressBar1.Value = 0;
            }
            String levelString = e.Node.Name;
            bool isHeader = false;
            bool isHeaderGroup = false;
            if (levelString != null && levelString.Length > 1 && levelString[0] == 'h') {
                isHeader = true;
                if (levelString[1] == 'g') {
                    isHeaderGroup = true;
                    //MessageBox.Show("Header Group: " + e.Node.Name);
                } else {
                    //MessageBox.Show("Header: " + e.Node.Name);
                }
            }



            if (isHeaderGroup) {

                String showStr = "";

                if (string.Equals(levelString, "hg0")) {
                    for (int i = 0; i < currentFile.Headers.Count; i++) {
                        String headerName = currentFile.Headers.Keys.ElementAt(i);
                        if (currentFile.Headers.ContainsKey(headerName)) {
                            List<String> headerValues = currentFile.Headers[headerName];
                            if (headerValues != null && headerValues.Count > 0) {
                                for (int k = 0; k < headerValues.Count; k++) {
                                    showStr += headerName + ": " + headerValues[k].ToString() + "\r\n";
                                }
                            } else {
                                for (int k = 0; k < headerValues.Count; k++) {
                                    showStr += headerName + ":\r\n";
                                }
                            }
                        }
                    }

                } else {

                    String[] levels = levelString.Split('.');

                    int first = Int32.Parse(levels[1]);
                    EmlFile.Part part = currentFile.Parts[first];
                    for (int i = 2; i < levels.Length; i++) {
                        int index = Int32.Parse(levels[i]);
                        part = part.Subparts[index];
                        if (part == null) {
                            break;
                        }
                    }
                    if (part != null) {

                        for (int i = 0; i < part.Headers.Count; i++) {
                            String headerName = part.Headers.Keys.ElementAt(i);
                            if (part.Headers.ContainsKey(headerName)) {
                                List<String> headerValues = part.Headers[headerName];
                                if (headerValues != null && headerValues.Count > 0) {
                                    for (int k = 0; k < headerValues.Count; k++) {
                                        showStr += headerName + ": " + headerValues[k].ToString() + "\r\n";
                                    }
                                } else {
                                    for (int k = 0; k < headerValues.Count; k++) {
                                        showStr += headerName + ":\r\n";
                                    }
                                }
                            }
                        }

                    }

                    //////////////////////
                }

                webBrowser1.DocumentText = "";
                webBrowser1.Hide();
                panel1.BackgroundImage = null;
                panel1.Hide();

                textBox1.Text = showStr;
                textBox1.Show();


            } else if (isHeader) {
                // TOP-LEVEL HEADER GROUP
                if (string.Equals(levelString, "h0")) {
                    String showStr = "";
                    for (int i = 0; i < currentFile.Headers.Count; i++) {
                        String headerName = currentFile.Headers.Keys.ElementAt(i);
                        if (currentFile.Headers.ContainsKey(headerName)) {
                            List<String> headerValues = currentFile.Headers[headerName];
                            if (headerValues != null && headerValues.Count > 0) {
                                for (int k = 0; k < headerValues.Count; k++) {
                                    showStr += headerName + ": " + headerValues[k].ToString() + "\r\n";
                                }
                            } else {
                                for (int k = 0; k < headerValues.Count; k++) {
                                    showStr += headerName + ":\r\n";
                                }
                            }
                        }
                    }
                    webBrowser1.DocumentText = "";
                    webBrowser1.Hide();
                    panel1.BackgroundImage = null;
                    panel1.Hide();

                    textBox1.Text = showStr;
                    textBox1.Show();

                } else {
                    String[] levels = levelString.Split('.');
                    if (levels.Length > 1 && levels[levels.Length - 1].IndexOf('_') > 0) {
                        //MessageBox.Show("Show specific header value??");
                    } else if (levels.Length == 2) {
                        // SHOWING ALL VALUES FOR A TOP-LEVEL HEADER
                        int index = Int32.Parse(levels[1]);
                        String headerName = currentFile.Headers.Keys.ElementAt(index);
                        if (currentFile.Headers.ContainsKey(headerName)) {
                            List<String> headerValues = currentFile.Headers[headerName];
                            if (headerValues != null) {
                                webBrowser1.DocumentText = "";
                                webBrowser1.Hide();
                                panel1.BackgroundImage = null;
                                panel1.Hide();


                                String showStr = "";
                                for (int k = 0; k < headerValues.Count; k++) {
                                    showStr += headerName + ": " + headerValues[k].ToString() + "\r\n";
                                }
                                textBox1.Text = showStr;
                                textBox1.Show();
                            }
                        }

                    } else if (levels.Length >= 2) {
                        // SHOWING ALL VALUES FOR SPECIFIC HEADER
                        int first = Int32.Parse(levels[1]);
                        EmlFile.Part part = currentFile.Parts[first];
                        for (int i = 2; i < levels.Length - 1; i++) {
                            int index = Int32.Parse(levels[i]);
                            part = part.Subparts[index];
                            if (part == null) {
                                break;
                            }
                        }
                        if (part != null) {
                            int index = Int32.Parse(levels[levels.Length - 1]);
                            String headerName = part.Headers.Keys.ElementAt(index);
                            List<String> headerValues = part.Headers[headerName];
                            if (headerValues != null) {
                                webBrowser1.DocumentText = "";
                                webBrowser1.Hide();
                                panel1.BackgroundImage = null;
                                panel1.Hide();


                                String showStr = "";
                                for (int k = 0; k < headerValues.Count; k++) {
                                    showStr += headerName + ": " + headerValues[k].ToString() + "\r\n";
                                }
                                textBox1.Text = showStr;
                                textBox1.Show();
                            }
                        }

                    }
                }

            } else if (
                String.Equals(e.Node.Text, "Message", StringComparison.OrdinalIgnoreCase)
                || String.Equals(e.Node.Text, "Content", StringComparison.OrdinalIgnoreCase)
                || String.Equals(e.Node.Text, "Parts", StringComparison.OrdinalIgnoreCase)
                || e.Node.Text.StartsWith("Part ", StringComparison.OrdinalIgnoreCase)
            ) {
                String[] levels = levelString.Split('.');
                if (levels.Length == 1 && levels[0][0] == '0') { // TRIVIAL CASE, MESSAGE BODY OF EMAIL
                    ShowDefaultMessage();
                } else if (levels.Length > 1) {
                    int first = Int32.Parse(levels[1]);
                    EmlFile.Part part = currentFile.Parts[first];
                    for (int i = 2; i < levels.Length; i++) {
                        int index = Int32.Parse(levels[i]);
                        if (index < part.Subparts.Count) {
                            part = part.Subparts[index];
                        } else {
                            part = null;
                        }
                        if (part == null) {
                            break;
                        }
                    }
                    if (part != null) {

                        // GO TO FIRST LEAF
                        while (part.Subparts != null && part.Subparts.Count > 0) {
                            part = part.Subparts[0];
                        }

                        currentFilePart = part;

                        if (part.StartsWithContentType("text/html")) {

                            textBox1.Text = "";
                            textBox1.Hide();
                            panel1.BackgroundImage = null;
                            panel1.Hide();

                            webBrowser1.DocumentText = part.GetContent();
                            webBrowser1.Show();
                        } else if (part.StartsWithContentType("text/") || part.StartsWithContentType("message/")) {

                            webBrowser1.DocumentText = "";
                            webBrowser1.Hide();
                            panel1.BackgroundImage = null;
                            panel1.Hide();

                            String content = part.GetContent();
                            textBox1.Text = content;
                            textBox1.Show();
                        } else if (part.StartsWithContentType("image/")) {
                            webBrowser1.DocumentText = "";
                            webBrowser1.Hide();
                            textBox1.Text = "";
                            textBox1.Hide();

                            byte[] content = part.GetContentBytes();
                            Image im = Image.FromStream(new MemoryStream(content));
                            panel1.BackgroundImageLayout = ImageLayout.Center;
                            panel1.BackgroundImage = im;
                            panel1.Show();

                        } else if (part.RegexMatchesHeader("content-type", ".*(?i)(application\\/).*(pdf).*")) {

                            webBrowser1.DocumentText = "";
                            webBrowser1.Hide();
                            panel1.BackgroundImage = null;
                            panel1.Hide();

                            byte[] content = part.GetContentBytes();
                            DisplayPdfTxt(content);
                            textBox1.Show();

                        } else {
                            webBrowser1.DocumentText = "";
                            webBrowser1.Visible = false;
                            textBox1.Text = part.ToDebugString();
                            textBox1.Visible = true;
                            panel1.BackgroundImage = null;
                            panel1.Visible = false;
                        }
                    } else {
                        //MessageBox.Show("No part");
                    }
                }
                //MessageBox.Show("Part: " + e.Node.Name);
            } else {
                //
                // 2022-04-05
                // LEFT OFF HERE
                // 1) NEED TO MAKE RIGHT CLICKING SHOW SOURCE OF PART
                // 2) NEED TO MAKE CLICKING ON FILENAME SHOW DEFAULT VIEW OF EMAIL (LIKE A NORMAL EMAIL VIEWER)
                // 3) NEED A SEARCH FUNCTION
                // 
                MessageBox.Show("Else: " + e.Node.Name);
                //MessageBox.Show("Part");
            }
        }



        private void EmlViewer_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                e.Effect = DragDropEffects.All;
            } else {
                String[] strGetFormats = e.Data.GetFormats();
                e.Effect = DragDropEffects.None;
            }
        }

        private void EmlViewer_DragDrop(object sender, DragEventArgs e) {
            string[] args = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            for (int i = 0; i < args.Length; i++) {
                if (args[i].EndsWith(".EML", StringComparison.OrdinalIgnoreCase)) {
                    EmlViewer_OpenFile(args[i]);
                    return;
                }
            }
        }


        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e) {
        }

        private void panel1_Paint(object sender, PaintEventArgs e) {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            // JUST A MENU CLICKED EVENT
            //MessageBox.Show(sender == openToolStripMenuItem ? "Open" : "?");
        }

        private void EmlViewer_OpenClicked(object sender, EventArgs e) {
            //MessageBox.Show("Open clicked");
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Browse EML Files";
            ofd.DefaultExt = "eml";
            ofd.Filter = "EML files (*.eml)|*.eml";
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;

            if (ofd.ShowDialog() == DialogResult.OK) {
                EmlViewer_OpenFile(ofd.FileName);
            }

        }

        /// <summary>
        /// Saves current <c>EmlFile.Part</c> as.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmlViewer_SavePartClicked(object sender, EventArgs e) {

            if (currentFilePart == null) {
                return;
            }

            using (SaveFileDialog dialog = new SaveFileDialog()) {

                dialog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
                dialog.FilterIndex = 2;
                dialog.RestoreDirectory = true;
                dialog.FileName = currentFilePart.GetContentName();
                dialog.OverwritePrompt = true;

                if (dialog.ShowDialog() == DialogResult.OK) {
                    // Can use dialog.FileName
                    using (Stream stream = dialog.OpenFile()) {
                        // Save data
                        stream.Write(currentFilePart.GetContentBytes());
                        stream.Close();
                    }

                }
            }
        }

        private void EmlViewer_CloseClicked(object sender, EventArgs e) {
            EmlViewer_CloseFile();
        }
        private void EmlViewer_ExitClicked(object sender, EventArgs e) {
            Environment.Exit(0);
        }

        private void openFileDialog1_FileOk(object sender, System.ComponentModel.CancelEventArgs e) {
        }

        private void textBox1_TextChanged(object sender, EventArgs e) {
        }


        #region Display PDFs
        private void DisplayPdfTxt(byte[] pdf_bytes) {
            String temp_pdf_filename = null;
            String temp_txt_filename = null;
            try {
                temp_pdf_filename = Path.GetTempFileName();
                temp_txt_filename = Path.GetTempFileName();
                if (temp_pdf_filename == null) {
                    return;
                }
                using (FileStream w = new FileStream(temp_pdf_filename, FileMode.Open)) {
                    w.Write(pdf_bytes, 0, pdf_bytes.Length);
                    w.Close();
                    w.Dispose();
                }

                Process process = new Process();
                process.StartInfo.FileName = pathToPdf2TxtExe;
                process.StartInfo.Arguments = " -layout \"" + temp_pdf_filename + "\" \"" + temp_txt_filename + "\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = true;
                process.Start();

                // NEED TO USE TEMP FILE, "con" KEYWORK REDIRECTING TO TESTING CONSOLE, NOT THIS EXECUTABLE
                //String output = process.StandardOutput.ReadToEnd(); // READ STD OUT (NOT WORKING IN TESTING WITH CONSOLE)
                string err = process.StandardError.ReadToEnd(); // READ STD ERR
                process.WaitForExit();

                if (File.Exists(temp_pdf_filename)) {
                    File.Delete(temp_pdf_filename);
                    temp_pdf_filename = null;
                }

                String output = null;
                if (File.Exists(temp_txt_filename)) {
                    output = File.ReadAllText(temp_txt_filename);
                    File.Delete(temp_txt_filename);
                    temp_txt_filename = null;
                }


                //
                // IF THERE ARE ERRORS, LOG THEM.
                // IF THERE ARE ERRORS WITHOUT OUTPUT, SEND EMAIL ALSO
                //
                bool hasOutput = (output != null && output.Trim().Length > 0);


                if (hasOutput) {
                    // NEED TO PROCESS FILES WITHOUT RATES ALSO, BY GIVING ZERO RATE
                    textBox1.Text = output;
                }


            } catch (Exception oops) {
            } finally {
                if (temp_pdf_filename != null) {
                    if (File.Exists(temp_pdf_filename)) {
                        File.Delete(temp_pdf_filename);
                    }
                    temp_pdf_filename = null;
                }
                if (temp_txt_filename != null) {
                    if (File.Exists(temp_txt_filename)) {
                        File.Delete(temp_txt_filename);
                    }
                    temp_txt_filename = null;
                }
            }

        }


        private void DisplayPdf(byte[] pdf_bytes) {
            String temp_pdf_filename = null;
            try {
                temp_pdf_filename = Path.GetTempFileName();
                if (temp_pdf_filename == null) {
                    return;
                }
                using (FileStream w = new FileStream(temp_pdf_filename, FileMode.Open)) {
                    w.Write(pdf_bytes, 0, pdf_bytes.Length);
                    w.Close();
                    w.Dispose();
                }

                webBrowser1.Navigate(@temp_pdf_filename);
                Thread.Sleep(500);

                if (File.Exists(temp_pdf_filename)) {
                    //File.Delete(temp_pdf_filename);
                    temp_pdf_filename = null;
                }


            } catch (Exception oops) {
            } finally {
                if (temp_pdf_filename != null) {
                    if (File.Exists(temp_pdf_filename)) {
                        File.Delete(temp_pdf_filename);
                    }
                    temp_pdf_filename = null;
                }
            }

        }
        #endregion

        private void messageLayoutPanel_Paint(object sender, PaintEventArgs e) {

        }
        private void headerLayoutPanel_Paint(object sender, PaintEventArgs e) {

        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e) {

        }

        private void label1_Click(object sender, EventArgs e) {

        }

        private void label1_Click_1(object sender, EventArgs e) {

        }
    }
}