using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


/// <summary>
/// <code>EmlFile</code> contains methods for decoding and reading data from
/// an <code>.EML</code> encoded file.
/// </summary>
/// <remarks>
/// An <code>.EML</code> file is an email message saved by an email application, such as Gmail.
/// It contains the content of the message, along with the subject, sender, recipient(s), and date of the message.
/// EML files may also store one or more email attachments, which are files sent with the message.
///
/// Further work needed to make more compatible with <code>System.Net.Mail.MailMessage</code>:
/// https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.mailmessage?view=net-6.0
/// </remarks>
public class EmlFile : IDisposable {


    /// <summary>
    /// Filename of the EML file, if opened from a file.
    /// </summary>
    public String Filename { get; private set; } = null;


    /// <summary>
    /// Filesize of the EML file, if opened from a file.
    /// Can be used to calculate percent loaded.
    /// Currently still being tested.
    /// </summary>
    public long Filesize { get; private set; } = 0;


    /// <summary>
    /// Headers of the email.
    /// </summary>
    public SortedDictionary<String, List<String>> Headers { get; private set; } = null;


    /// <summary>
    /// The mime-parts of this EML file.
    /// If MIME is not used, then one Part will represent the message of the email.
    /// </summary>
    public List<EmlFile.Part> Parts { get; private set; } = null;


    /// <summary>
    /// The top-level unique boundary of this EML file.
    /// </summary>
    public String UniqueBoundary { get; private set; } = null;


    /// <summary>
    /// Lock used in the <code>Decode()</code> method.
    /// </summary>
    private Object DecodeLock = new object();


    /// <summary>
    /// Used to force-stop reading if necessary.
    /// Before and after decoding, <code>ReadFromStream</code> is set to <code>null</code>.
    /// Set in method <code>EmlFile.DecodeTo()</code>.
    /// Used in method <code>Stop()</code>.
    /// </summary>
    private StreamReader ReadFromStream = null;


    /// <summary>
    /// Tells if this <code>EmlFile</code> has been decoded or not.
    /// </summary>
    public bool DecodedFile { get; private set; } = false;


    /// <summary>
    /// Tells if the decoding process returned okay or not.
    /// </summary>
    public bool DecodedOkay { get; private set; } = false;


    /// <summary>
    /// This is an estimate of characters read.  It assumes <code>CRLF</code> line endings,
    /// and it counts characters, not bytes.
    /// 
    /// It can be compared to <code>Filesize</code>, but as noted,
    /// the character encoding of this <code>EmlFile</code> will make a difference.
    /// </summary>
    public long DecodedSize { get; private set; } = 0;




    /// <summary>
    /// Represents a part of an email file.
    /// </summary>
    /// <remarks>
    /// Parts in EML files are separated by (possibly recursive) unique boundaries.
    /// If the EML file does not use MIME parts or unique boundaries,
    /// a single top-level <code>EmlFile.Part</code> will be created to represent the message.
    /// </remarks>
    public class Part : IDisposable {

        /// <summary>
        /// The headers of this <code>EmlFile.Part</code>.
        /// </summary>
        public SortedDictionary<String, List<String>> Headers { get; internal set; } = null;

        /// <summary>
        /// The sub-parts of this <code>EmlFile.Part</code>.  Forms a tree structure.
        /// </summary>
        public List<EmlFile.Part> Subparts { get; internal set; } = null; // RECURSIVE SUB-PARTS (IF APPLICABLE)

        /// <summary>
        /// The content of this <code>EmlFile.Part</code>.  Only found in leafs of the tree.
        /// </summary>
        public String Content { get; internal set; } = null;

        /// <summary>
        /// Unique boundary for this <code>EmlFile.Part</code>.  Sub-parts have their own.
        /// </summary>
        public String UniqueBoundary { get; internal set; } = null; // BOUNDARY OF SUB-PARTS (IF APPLICABLE)

        //public Part ContainingPart { get; internal set; } = null; // IF PART IS SUB-PART OF ANOTHER PART IN THE FILE

        /// <summary>
        /// Charset of this <code>EmlFile.Part</code> specified in "Content-type" header.
        /// </summary>
        public String Charset { get; private set; } = null;


        /// <summary>
        /// Name of this <code>EmlFile.Part</code> specified in "Content-type" header.
        /// </summary>
        public String Name { get; private set; } = null;

        /// <summary>
        /// <code>EmlFile</code> containing this <code>EmlFile.Part</code>.
        /// </summary>
        public EmlFile Eml { get; internal set; } = null; // TOPMOST CONTAINING EML FILE

        /// <summary>
        /// This is initialized during decoding.
        /// </summary>
        private byte[] DecodedBytes = null;
        private String DecodedContent = null;


        /// <summary>
        /// Constructs a new <code>EmlFile.Part</code>.
        /// </summary>
        public Part() {
            this.Headers = null;
            this.Content = null;
            this.Eml = null;
        }


        /// <summary>
        /// Returns the first value for the specified header name.
        /// </summary>
        /// <param name="param">The header name, E.G.: "content-type", "subject" or "from".</param>
        /// <returns>The header value, or the first header value if there are more than one.</returns>
        public String GetHeaderValue(String param) {
            param = param.ToLower();
            if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
                return this.Headers[param].ElementAt(0);
            }
            return this.Eml.GetHeaderValue(param);
        }


        /// <summary>
        /// Returns the "Content-type" header value of this <code>EmlFile.Part</code>.
        /// </summary>
        /// <returns>Returns the content type.</returns>
        public String GetContentType() {
            return this.GetHeaderValue("content-type");
        }



        /// <summary>
        /// Returns the "Content-transfer-encoding" header value (like "base64" or "quoted-printable") for this <code>EmlFile.Part</code>.
        /// </summary>
        /// <returns>The "Content-transfer-encoding" header value.</returns>
        public String GetContentTransferEncoding() {
            return this.GetHeaderValue("content-transfer-encoding");
        }




        /// <summary>
        /// Returns if this <code>EmlFile.Part</code> has message content or not.
        /// </summary>
        /// <remarks>
        /// Calling this method will not decode the content to working memory.
        /// </remarks>
        /// <returns><code>true</code> if there is content after the headers, <code>false</code> otherwise.</returns>
        public bool HasContent() {
            return this.Content != null && this.Content.Length > 0;
        }



        /// <summary>
        /// Returns the decoded content of this <code>EmlFile.Part</code> in the charset from <code>GetCharset()</code>, or <code>Encoding.UTF8</code>.
        /// </summary>
        /// <returns>The content of this part as a <code>String</code>.</returns>
        public String GetContent() {
            try {
                String charset = this.GetCharset();
                if (charset != null) {
                    Encoding e = EmlFile.GetDotNetEncoding(charset);
                    if (e != null) {
                        return this.GetContent(e);
                    }
                }
            } catch (Exception x) {
            }
            return this.GetContent(Encoding.UTF8);
        }



        /// <summary>
        /// This functions returns the content of this part of the email file
        /// as a string in the specified encoding.
        /// </summary>
        /// <remarks>
        /// NOTE: NOT TESTED WITH VARIOUS INPUT/OUTPUT ENCODING COMBINATIONS YET
        /// </remarks>
        /// <param name="dec"></param>
        /// <returns>The <code>String</code> content of this email part.</returns>
        public String GetContent(Encoding dec) {
            if (this.DecodedContent != null) {
                return this.DecodedContent;
            }
            //if (this.decodedBytes != null) {
            //    this.decodedContent = dec.GetString(this.decodedBytes);
            //    return this.decodedContent;
            //}
            String enc = this.GetContentTransferEncoding();
            //Console.WriteLine("---[" + enc + "]---");
            if (enc == null || enc.Length == 0) {
                this.DecodedContent = this.Content;
            } else if (enc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) >= 0) {
                this.DecodedBytes = Convert.FromBase64String(this.Content);
                this.DecodedContent = dec.GetString(this.DecodedBytes);
            } else if (enc.IndexOf("Quoted-printable", StringComparison.OrdinalIgnoreCase) >= 0) {
                //Attachment attachment = Attachment.CreateAttachmentFromString(this.content);
                //this.decodedContent = attachment.Name;
                this.DecodedContent = EmlFile.DecodeQuotedPrintable(this.Content, this.GetCharset());
            } else {
                this.DecodedContent = this.Content;
            }
            return this.DecodedContent;
        }


        /// <summary>
        /// Returns the binary bytes of the content of this part.
        /// </summary>
        /// <returns>binary content</returns>
        public byte[] GetContentBytes() {
            if (this.DecodedBytes != null) {
                return this.DecodedBytes;
            }
            String enc = this.GetContentTransferEncoding(); // eml part encoding, not character encoding
            //Console.WriteLine("---[" + enc + "]---");
            if (enc == null || enc.Length == 0) {
                // NOT SURE IF THIS SHOULD BE ASCII, LATIN1, OR UTF8
                this.DecodedBytes = Encoding.UTF8.GetBytes(this.Content);
            } else if (enc.IndexOf("base64", StringComparison.OrdinalIgnoreCase) >= 0) {
                this.DecodedBytes = Convert.FromBase64String(this.Content);
            } else if (enc.IndexOf("Quoted-printable", StringComparison.OrdinalIgnoreCase) >= 0) {
                this.DecodedBytes = EmlFile.DecodeQuotedPrintableBytes(this.Content);
            } else {
                this.DecodedBytes = Encoding.UTF8.GetBytes(this.Content);
            }
            return this.DecodedBytes;
        }


        /// <summary>
        /// Returns the charset of this MIME part.
        /// </summary>
        /// <returns>Charset as a <code>String</code>, or <code>null</code> if not found.</returns>
        public String GetCharset() {
            if (this.Charset != null) {
                return this.Charset;
            }
            String contentType = this.GetContentType();
            String charset = null;
            if (contentType != null) {
                int charsetAt = contentType.IndexOf("charset", StringComparison.OrdinalIgnoreCase);
                if (charsetAt >= 0) {
                    charsetAt += "charset".Length;
                    while (charsetAt < contentType.Length && Char.IsWhiteSpace(contentType[charsetAt])) {
                        charsetAt++;
                    }
                    if (charsetAt < contentType.Length && contentType[charsetAt] == '=') {
                        charsetAt++;
                        while (charsetAt < contentType.Length && Char.IsWhiteSpace(contentType[charsetAt])) {
                            charsetAt++;
                        }
                        if (charsetAt < contentType.Length) {
                            int charsetTo = -1;
                            if (contentType[charsetAt] == '"') {
                                charsetAt++;
                                charsetTo = charsetAt + 1;
                                while (charsetTo < contentType.Length && contentType[charsetTo] != '"') {
                                    if (contentType[charsetTo] == '\\') {
                                        charsetTo++;
                                    }
                                    charsetTo++;
                                }
                            } else if (contentType[charsetAt] == '\'') {
                                while (charsetTo < contentType.Length && contentType[charsetTo] != '\'') {
                                    if (contentType[charsetTo] == '\\') {
                                        charsetTo++;
                                    }
                                    charsetTo++;
                                }
                            } else {
                                // ??? go to whitespace
                                charsetTo = charsetAt + 1;
                                while (charsetTo < contentType.Length && !Char.IsWhiteSpace(contentType[charsetTo])) {
                                    charsetTo++;
                                }
                            }
                            if (charsetTo > charsetAt) {
                                if (charsetTo > contentType.Length) {
                                    charsetTo = contentType.Length;
                                }
                                this.Charset = contentType.Substring(charsetAt, charsetTo - charsetAt);
                            }
                        }
                    }
                }
            }
            return this.Charset;
        }


        /// <summary>
        /// Returns the name/filename of this MIME part from the "content-type" header.
        /// </summary>
        /// <returns>Charset as a <code>String</code>, or <code>null</code> if not found.</returns>
        public String GetContentName() {
            if (this.Name != null) {
                return this.Name;
            }
            String contentType = this.GetContentType();
            if (contentType != null) {
                int nameAt = contentType.IndexOf("name", StringComparison.OrdinalIgnoreCase);
                while (nameAt >= 0) {
                    if (nameAt > 0 && nameAt < contentType.Length - 5 && !Char.IsLetterOrDigit(contentType[nameAt - 1])) {
                        int nameEq = nameAt + 4;
                        while (nameEq < contentType.Length && Char.IsWhiteSpace(contentType[nameEq])) {
                            nameEq++;
                        }
                        if (nameEq < contentType.Length && contentType[nameEq] == '=') {
                            int nameStart = nameEq + 1;
                            while (nameStart < contentType.Length && Char.IsWhiteSpace(contentType[nameStart])) {
                                nameStart++;
                            }
                            char quoteChar = contentType[nameStart];
                            if (quoteChar == '\'' || quoteChar == '"') {
                                nameStart++;
                                int nameEnd = nameStart;
                                while (nameEnd < contentType.Length && contentType[nameEnd] != quoteChar) {
                                    if (contentType[nameEnd] == '\\') {
                                        nameEnd++;
                                    }
                                    nameEnd++;
                                }
                                if (contentType[nameEnd] == quoteChar) {
                                    if (nameEnd > nameStart) {
                                        this.Name = contentType.Substring(nameStart, nameEnd - nameStart);
                                        return this.Name;
                                    }
                                }
                            } else {
                                int nameEnd = nameStart;
                                while (nameEnd < contentType.Length && !Char.IsWhiteSpace(contentType[nameEnd]) && contentType[nameEnd] != ';') {
                                    nameEnd++;
                                }
                                if (nameEnd > nameStart) {
                                    this.Name = contentType.Substring(nameStart, nameEnd - nameStart);
                                    return this.Name;
                                }
                            }

                        }
                    }
                    nameAt = contentType.IndexOf("name", nameAt + 4, StringComparison.OrdinalIgnoreCase);

                }
            }
            return this.Name;
        }





        /// <summary>
        /// Returns if the specified header <code>param</code>
        /// matches the specified regular expression <code>pattern</code>.
        /// </summary>
        /// <param name="param"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        public bool RegexMatchesHeader(String param, String pattern) {
            if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
                for (int i = 0; i < this.Headers[param].Count; i++) {
                    String value = this.Headers[param].ElementAt(i);
                    //Console.WriteLine("Check match: " + value + " in \"" + pattern + "\"");
                    if (System.Text.RegularExpressions.Regex.IsMatch(value, pattern)) {
                        //Console.WriteLine("Got match: " + value + " in \"" + pattern + "\"");
                        return true;
                    }
                }
            } else if (this.Eml != null) {
                return this.Eml.RegexMatchesHeader(param, pattern);
            }
            return false;
        }





        public bool StartsWithContentType(String contentType) {
            String param = "content-type";
            if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
                for (int i = 0; i < this.Headers[param].Count; i++) {
                    //Console.WriteLine("-----------------------\r\n" + this.Headers[param].ElementAt(i) + "\r\n----------------------");
                    if (this.Headers[param].ElementAt(i).StartsWith(contentType)) {
                        return true;
                    }
                }
            } else if (this.Eml != null) {
                return this.Eml.StartsWithContentType(contentType);
            }
            return false;
        }


        public bool EndsWithContentType(String contentType) {
            String param = "content-type";
            if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
                for (int i = 0; i < this.Headers[param].Count; i++) {
                    //Console.WriteLine("-----------------------\r\n" + this.Headers[param].ElementAt(i) + "\r\n----------------------");
                    if (this.Headers[param].ElementAt(i).Trim().EndsWith(contentType)) {

                        return true;
                    }
                }
            } else if (this.Eml != null) {
                return this.Eml.EndsWithContentType(contentType);
            }
            return false;
        }


        //
        // NOT SURE IF ONLY LEAFS CONTAIN CERTAIN CONTENT TYPES OR NOT
        //
        public List<EmlFile.Part> GetPartsWithContentType(String contentType) {
            //Console.WriteLine("Part.GetPartsWithContentType(" + contentType + ")");
            List<EmlFile.Part> matches = new List<EmlFile.Part>();
            if (this.Subparts != null) {
                foreach (Part p in this.Subparts) {
                    if (p != null) {
                        if (p.StartsWithContentType(contentType)) {
                            matches.Add(p);
                        }
                        if (p.Subparts != null && p.Subparts.Count > 0) {
                            //List<EmlFile.Part> submatches = p.GetPartsWithContentType(contentType);
                            //foreach (Part sp in submatches) {
                            //    matches.Add(sp);
                            //}
                            if (p.Subparts != null && p.Subparts.Count > 0) {
                                matches.AddRange(p.GetPartsWithContentType(contentType));
                            }
                        }
                    }
                }
            }
            return matches;
        }


        public List<EmlFile.Part> GetPartsMatchingHeaderRegex(String param, String pattern) {
            //Console.WriteLine("EmlFile.GetPartsMatchingHeaderRegex(" + param + "," + pattern + ")");
            List<EmlFile.Part> matches = new List<EmlFile.Part>();
            foreach (Part p in this.Subparts) {
                if (p != null) {
                    if (p.RegexMatchesHeader(param, pattern)) {
                        matches.Add(p);
                    }
                    if (p.Subparts != null && p.Subparts.Count > 0) {
                        matches.AddRange(p.GetPartsMatchingHeaderRegex(param, pattern));
                    }
                }
            }
            return matches;
        }

        public void Dispose() {
            this.Eml = null;
            this.Headers = null;
            this.Content = null;
            this.DecodedContent = null;
            this.DecodedBytes = null;
        }


        public String ToDebugString(String indentStr) {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(indentStr + "_______________________________");

            if (this.Headers != null) {
                foreach (String k in this.Headers.Keys) {
                    if (k != null) {
                        int n = this.Headers[k].Count;
                        for (int i = 0; i < n; i++) {
                            sb.AppendLine(indentStr + k + ": " + this.Headers[k].ElementAt(i)); // DON'T BOTHER FOLDING Headers HERE
                        }
                    }
                }
            }
            if (this.Subparts != null && this.Subparts.Count > 0) {
                foreach (Part p in this.Subparts) {
                    if (p != null) {
                        if (UniqueBoundary != null) {
                            sb.AppendLine("\r\n--" + UniqueBoundary);
                        }
                        sb.Append(p.ToDebugString(indentStr + indentStr));
                    }
                }
                if (UniqueBoundary != null) {
                    sb.AppendLine("\r\n--" + UniqueBoundary);
                }
            }

            sb.AppendLine();
            sb.AppendLine(indentStr + "+-----------------------------+");

            String ct = this.GetContentType();
            if (ct == null || ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) {
                sb.Append(this.GetContent());
            }
            sb.AppendLine(indentStr + "L_____________________________|");

            return sb.ToString();
        }
        public String ToDebugString() {
            return this.ToDebugString("");
        }


    }
    // END OF CLASS EMLFILE.PART
    ///////////////////////////////////////////////////
    //
    //
    //
    //
    //
    //
    //
    ///////////////////////////////////////////////////
    // CONTINUATION OF CLASS EMLFILE



    /// <summary>
    /// Basic constructor for internal use.
    /// Can alternatively use <code>EmlFile.DecodeFile()</code> or <code>EmlFile.DecodeFileAsync()</code>
    /// </summary>
    private EmlFile() {
    }


    /// <summary>
    /// Creates a new <code>EmlFile</code> for specified <code>filename</code>.
    /// File reading and decoding does not begin until <code>Decode()</code> or <code>DecodeAsync</code> is called.
    /// </summary>
    /// <param name="filename"></param>
    /// <exception cref="FileNotFoundException"></exception>
    public EmlFile(string filename) {
        // LONG FILENAMES
        if (!filename.StartsWith("\\\\?\\")) {
            filename = "\\\\?\\" + Path.GetFullPath(filename);
        }
        if (!File.Exists(filename)) {
            throw new FileNotFoundException(filename);
        }
        this.Filename = filename;
        FileInfo fi = new FileInfo(filename);
        this.Filesize = fi.Length;
    }


    /// <summary>
    /// Decodes this <code>EmlFile</code>.
    /// If there is an error, null is returned.
    /// </summary>
    /// <returns>Whether decoding was successful or not</returns>
    public bool Decode() {
        lock (DecodeLock) {
            if (DecodedFile) {
                return DecodedOkay;
            }
            StreamReader r = null;
            try {
                DecodedOkay = false;
                using (r = new StreamReader(this.Filename)) {
                    DecodedOkay = (DecodeTo(r, this, null) == this);
                }
                return DecodedOkay;
            } finally {
                if (r != null) {
                    try {
                        r.Close();
                    } catch (Exception foo) {
                    }
                }
            }
        }
        return DecodedOkay;
    }


    public async Task<bool> DecodeAsync() {
        return this.Decode();
    }


    /// <summary>
    /// Decodes the EML file specified by <code>filename</code>.
    /// </summary>
    /// <param name="filename"></param>
    /// <returns>An <code>EmlFile</code> with Headers, parts and sub-parts... or <code>null</code> on error.</returns>
    public static EmlFile DecodeFile(String filename) {
        EmlFile ef = null;
        ef = new EmlFile(filename);
        ef.Decode();
        return ef;
    }


    public static async Task<EmlFile> DecodeFileAsync(String filename) {
        return DecodeFile(filename);
    }

    /// <summary>
    /// If the file is reading, it stops reading.
    /// </summary>
    public void Stop() {
        try {
            if (ReadFromStream != null) {
                ReadFromStream.Close();
                ReadFromStream.Dispose();
                ReadFromStream = null;
            }
        } catch (Exception x) {
        } finally {
            ReadFromStream = null;
        }
    }

    public virtual void Dispose() {
        this.Stop();
    }

    public String GetHeaderValue(String param) {
        param = param.ToLower();
        if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers.Count > 0) {
            return this.Headers[param].ElementAt(0);
        }
        return null;
    }

    public bool StartsWithContentType(String contentType) {
        String param = "content-type";
        if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
            for (int i = 0; i < this.Headers[param].Count; i++) {
                //Console.WriteLine("-----------------------\r\n" + this.Headers[param].ElementAt(i) + "\r\n----------------------");
                if (this.Headers[param].ElementAt(i).StartsWith(contentType)) {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Returns if the specified header <code>param</code>
    /// matches the specified regular expression <code>pattern</code>.
    /// </summary>
    /// <param name="param"></param>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public bool RegexMatchesHeader(String param, String pattern) {
        if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
            for (int i = 0; i < this.Headers[param].Count; i++) {
                String value = this.Headers[param].ElementAt(i);
                if (System.Text.RegularExpressions.Regex.IsMatch(value, pattern)) {
                    return true;
                }
            }
        }
        return false;
    }


    public bool EndsWithContentType(String contentType) {
        String param = "content-type";
        if (this.Headers != null && this.Headers.ContainsKey(param) && this.Headers[param].Count > 0) {
            for (int i = 0; i < this.Headers[param].Count; i++) {
                //Console.WriteLine("-----------------------\r\n" + this.Headers[param].ElementAt(i) + "\r\n----------------------");
                if (this.Headers[param].ElementAt(i).Trim().EndsWith(contentType)) {
                    return true;
                }
            }
        }
        return false;
    }


    public bool SubjectStartsWith(String prefix) {
        String subject = this.GetHeaderValue("subject");
        return subject != null && subject.Length > 0 && subject.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }


    //
    // NOT SURE IF ONLY LEAFS CONTAIN CERTAIN CONTENT TYPES OR NOT
    //
    public List<EmlFile.Part> GetPartsWithContentType(String contentType) {
        //Console.WriteLine("EmlFile.GetPartsWithContentType(" + contentType + ")");

        List<EmlFile.Part> matches = new List<EmlFile.Part>();
        foreach (Part p in this.Parts) {
            if (p != null) {
                if (p.StartsWithContentType(contentType)) {
                    matches.Add(p);
                }
                if (p.Subparts != null && p.Subparts.Count > 0) {
                    matches.AddRange(p.GetPartsWithContentType(contentType));
                }
            }
        }
        return matches;
    }





    /// <summary>
    /// Uses regular expression specified by <code>regex</code>
    /// to determine if the content type matches.
    ///
    /// Uses <code>System.Text.RegularExpressions.Regex.IsMatch()</code>
    /// </summary>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public List<EmlFile.Part> GetPartsWithContentTypeRegex(String pattern) {
        //Console.WriteLine("EmlFile.GetPartsWithContentTypeRegex(" + pattern + ")");
        List<EmlFile.Part> matches = new List<EmlFile.Part>();
        foreach (Part p in this.Parts) {
            if (p != null) {
                if (p.RegexMatchesHeader("content-type", pattern)) {
                    matches.Add(p);
                }
                if (p.Subparts != null && p.Subparts.Count > 0) {
                    matches.AddRange(p.GetPartsMatchingHeaderRegex("content-type", pattern));
                }
            }
        }
        return matches;
    }

    public EmlFile.EmailAddress[] GetToAddresses() {
        String toStr = this.GetHeaderValue("to");
        return EmlFile.ParseAddresses(toStr);
    }

    public EmlFile.EmailAddress GetFromAddress() {
        String fromStr = this.GetHeaderValue("from");
        return EmlFile.ParseAddress(fromStr);
    }

    public class EmailAddress {
        public String Alias { get; set; } = null;
        public String Address { get; set; } = null;

        public EmailAddress(String alias, String addr) {
            this.Alias = alias;
            this.Address = addr;
        }

        /// <summary>
        /// Parses and returns an array of <code>EmlFile.EmailAddress</code>.
        /// </summary>
        /// <remarks>
        /// Simple wrapper around <code>EmlFile.ParseAddresses()</code>.
        /// </remarks>
        /// <param name="s">The <code>String</code> to parse the <code>EmlFile.EmailAddress</code> array from.</param>
        /// <returns>An array of <code>EmlFile.EmailAddress[]</code>.</returns>
        public static EmlFile.EmailAddress[] Parse(String s) {
            return EmlFile.ParseAddresses(s);
        }

        public override int GetHashCode() {
            if (this.Alias != null && this.Alias.Length > 0) {
                if (this.Address != null && this.Address.Length > 0) {
                    return 33 + this.Alias.GetHashCode() ^ this.Address.GetHashCode();
                } else {
                    return 31 + this.Alias.GetHashCode();
                }
            } else if (this.Address != null && this.Address.Length > 0) {
                return 29 + this.Address.GetHashCode();
            }
            return 27 ^ base.GetHashCode();
        }

        public override String ToString() {
            if (this.Alias != null && this.Alias.Length > 0) {
                if (this.Address != null && this.Address.Length > 0) {
                    return "\"" + this.Alias + "\" <" + this.Address + ">";
                } else {
                    return "\"" + this.Alias + "\" <>";
                }
            } else if (this.Address != null && this.Address.Length > 0) {
                return this.Address;
            }
            return base.ToString();
        }

    }

    #region Email Address Parsing




    /// <summary>
    /// Parses and returns an array of <code>EmlFile.EmailAddress</code>.
    /// </summary>
    /// <param name="s">The <code>String</code> to parse the <code>EmlFile.EmailAddress</code> array from.</param>
    /// <returns>An array of <code>EmlFile.EmailAddress[]</code>.</returns>
    public static EmlFile.EmailAddress[] ParseAddresses(String s) {
        List<EmlFile.EmailAddress> parts = new List<EmlFile.EmailAddress>();
        int i = 0;
        int sep = 0;
        EmlFile.EmailAddress part = null;
        while (i < s.Length) {
            if (s[i] == '"') {
                i++;
                while (i < s.Length) {
                    if (s[i] == '\\') {
                        i++;
                    } else if (s[i] == '"') {
                        break;
                    }
                    i++;
                }
            } else if (s[i] == ',' || s[i] == ';' || s[i] == '/') {
                part = EmlFile.ParseAddress(s.Substring(sep, i - sep).Trim());
                if (part != null) {
                    parts.Add(part);
                }
                sep = i + 1;
            }
            i++;
        }
        part = ParseAddress(s.Substring(sep).Trim());
        if (part != null) {
            parts.Add(part);
        }
        return parts.ToArray();
    }

    /// <summary>
    /// Helper method for <code>ParseAddressList()</code>.
    /// Returns a single <code>EmailAddress</code> from the string.
    /// </summary>
    /// <param name="s">An email address</param>
    /// <returns>An <code>EmlFile.EmailAddress</code> with alias and address separated.</returns>
    private static EmailAddress ParseAddress(String s) {
        int i = 0;
        int addr_start = -1;
        int addr_end = -1;
        while (i < s.Length) {
            if (s[i] == '"') {
                i++;
                while (i < s.Length) {
                    if (s[i] == '\\') {
                        i++;
                    } else if (s[i] == '"') {
                        break;
                    }
                    i++;
                }
            } else if (addr_start == -1 && s[i] == '<') {
                addr_start = i;
            } else if (s[i] == '>') {
                addr_end = i;
            }
            i++;
        }
        String name = null;
        String addr = null;
        if (addr_start >= 0 && addr_end > addr_start) {
            name = s.Substring(0, addr_start).Trim();
            addr = s.Substring(addr_start + 1, (addr_end - addr_start) - 1).Trim();
            if (name.Length > 1 && name[0] == '"' && name[name.Length - 1] == '"') {
                name = name.Substring(1, name.Length - 2).Trim();
            }
        } else {
            addr = s.Trim();
        }
        if (addr.IndexOf('@') <= 0 || addr.Length == 0) {
            return null;
        }

        if (addr != null) {
            return new EmlFile.EmailAddress(name, addr);
        } else {
            return null;
        }

    }


    #endregion


    #region Internal EML Decoding Methods
    private static EmlFile.Part DecodePart(EmlFile eml, StreamReader reader, String uniqueBoundary, String outerUniqueBoundary) {
        //MessageBox.Show("Unique Boundary: " + uniqueBoundary);
        //Console.WriteLine("decodingPart(" + uniqueBoundary + ", " + outerUniqueBoundary + ")");
        String line = null;
        bool gotBlankLine = false;
        String param = null;
        String value = null;
        SortedDictionary<String, List<String>> headerParamValues = new SortedDictionary<String, List<String>>();
        StringBuilder partContent = new StringBuilder();

        try {
            while ((line = reader.ReadLine()) != null) {
                //                Thread.Sleep(500);
                eml.DecodedSize += line.Length + 2;
                if (outerUniqueBoundary != null && line.IndexOf("--" + outerUniqueBoundary) == 0) {
                    //Console.WriteLine("end boundary 1(" + uniqueBoundary + ", " + outerUniqueBoundary + ")");
                    return null;
                }
                if (!gotBlankLine && line.Length == 0) {

                    if (param != null && value != null && param.Length > 0 && value.Length > 0) {
                        param = param.ToLower();
                        if (headerParamValues == null || !headerParamValues.ContainsKey(param)) {
                            headerParamValues[param] = new List<String>();
                        }
                        headerParamValues[param].Add(value);
                        param = null;
                        value = null;
                    }
                    gotBlankLine = true;
                    continue;
                }
                if (outerUniqueBoundary != null && line.IndexOf("--" + outerUniqueBoundary) == 0) {
                    //Console.WriteLine("end boundary 2(" + uniqueBoundary + ", " + outerUniqueBoundary + ")");
                    return null;
                }

                if (gotBlankLine) {

                    //
                    // THEN WE ARE IN THE BODY
                    //
                    EmlFile.Part part = new EmlFile.Part();
                    part.Eml = eml;
                    part.Headers = headerParamValues;

                    if (headerParamValues.ContainsKey("content-type")) {
                        String contentType = headerParamValues["content-type"][0];
                        int boundary_at = contentType.IndexOf("boundary");
                        if (boundary_at > 0) {
                            boundary_at += 8;
                            while (boundary_at < contentType.Length && Char.IsWhiteSpace(contentType[boundary_at])) {
                                boundary_at++;
                            }
                            if (contentType[boundary_at] == '=') {
                                boundary_at++;
                                if (boundary_at < contentType.Length) {
                                    if (contentType[boundary_at] == '"') {
                                        boundary_at++;
                                        int boundary_end = boundary_at + 1;
                                        while (boundary_end < contentType.Length && contentType[boundary_end] != '"') {
                                            boundary_end++;
                                        }
                                        if (boundary_end > boundary_at + 1) {
                                            part.UniqueBoundary = contentType.Substring(boundary_at, boundary_end - boundary_at);
                                            //Console.WriteLine("Boundary of part: " + part.UniqueBoundary);
                                        }
                                    } else {
                                        while (boundary_at < contentType.Length && Char.IsWhiteSpace(contentType[boundary_at])) {
                                            boundary_at++;
                                        }
                                        int boundary_end = boundary_at;
                                        while (boundary_end < contentType.Length && !Char.IsWhiteSpace(contentType[boundary_end])) {
                                            boundary_end++;
                                        }
                                        if (boundary_end > boundary_at) {
                                            part.UniqueBoundary = contentType.Substring(boundary_at, boundary_end - boundary_at);
                                            //Console.WriteLine("Boundary of part: " + part.UniqueBoundary);
                                            //MessageBox.Show("Boundary of part: " + part.UniqueBoundary);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (part.UniqueBoundary != null) {
                        // PART IS MADE OF SUB-PARTS
                        part.Content = "";
                        part.Subparts = new List<Part>();
                        do {
                            if (line.IndexOf("--" + part.UniqueBoundary) == 0) {
                                break;
                            }
                            eml.DecodedSize += line.Length + 2;
                        } while ((line = reader.ReadLine()) != null);

                        EmlFile.Part sub_part = null;
                        while ((sub_part = EmlFile.DecodePart(eml, reader, part.UniqueBoundary, uniqueBoundary)) != null) {
                            part.Subparts.Add(sub_part);
                        }
                        //Console.WriteLine("  part has " + part.subparts.Count + " sub-parts");
                        if (
                            (part.Subparts != null && part.Subparts.Count > 0)
                         || (part.Headers != null && part.Headers.Count > 0)
                        ) {
                            return part;
                        } else {
                            return null;
                        }
                    } else {
                        // ADD PART CONTENTS
                        partContent.AppendLine(line);
                        while ((line = reader.ReadLine()) != null) {
                            eml.DecodedSize += line.Length + 2;
                            if (uniqueBoundary != null && line.IndexOf("--" + uniqueBoundary) == 0) {
                                break;
                            }
                            partContent.AppendLine(line);
                        }
                        part.Content = partContent.ToString();
                        if (
                            (part.Subparts != null && part.Subparts.Count > 0)
                         || (part.Content != null && part.Content.Length > 0)
                         || (part.Headers != null && part.Headers.Count > 0)
                        ) {
                            return part;
                        } else {
                            return null;
                        }
                    }
                } else {
                    // THEN WE ARE STILL IN THE HEADERS
                    if (Char.IsWhiteSpace(line[0])) {
                        line = DecodeHeaderLine(line.TrimStart());
                        if (value != null) {
                            value += line;
                        } else {
                            if (param != null && param.Length > 0) {
                                value = line.Trim();
                            } else {
                                // ERROR!
                                param = null;
                                value = null;
                            }
                        }
                    } else {
                        if (param != null && value != null && param.Length > 0 && value.Length > 0) {
                            param = param.ToLower();
                            if (headerParamValues == null || !headerParamValues.ContainsKey(param)) {
                                headerParamValues[param] = new List<String>();
                            }
                            headerParamValues[param].Add(value);
                            param = null;
                            value = null;
                        }
                        int end_param = line.IndexOf(':');
                        if (end_param > 0) {
                            param = line.Substring(0, end_param).Trim();
                            value = DecodeHeaderLine(line.Substring(end_param + 1).TrimStart());
                        } else {
                            // ERROR
                            param = null;
                            value = null;
                        }
                    }
                }
            }
        } catch (Exception e) {
            try {
                reader.Close();
            } catch (Exception e2) {
                Console.Error.WriteLine(e2.Message);
                Console.Error.WriteLine(e2.StackTrace);
            }
            Console.Error.WriteLine("Exception in DecodePart(" + uniqueBoundary + ", " + outerUniqueBoundary + ")");
            Console.Error.WriteLine(e.Message);
            Console.Error.WriteLine(e.StackTrace);
        }
        return null;
    }

    private static EmlFile DecodeTo(StreamReader reader, EmlFile eml, Dictionary<String, bool> filterParams) {

        eml.ReadFromStream = reader;

        String line = null;
        bool gotBlankLine = false;
        String param = null;
        String value = null;
        String uniqueBoundary = null;
        SortedDictionary<String, List<String>> headerParamValues = new SortedDictionary<String, List<String>>();

        try {
            while ((line = reader.ReadLine()) != null) {
                eml.DecodedSize += line.Length + 2;
                if (line.Length == 0) {

                    if (param != null && value != null && param.Length > 0 && value.Length > 0) {
                        param = param.ToLower();
                        if (headerParamValues == null || !headerParamValues.ContainsKey(param)) {
                            headerParamValues[param] = new List<String>();
                        }
                        headerParamValues[param].Add(value);
                        param = null;
                        value = null;
                    }
                    if (uniqueBoundary == null && headerParamValues.ContainsKey("content-type")) {
                        String contentType = headerParamValues["content-type"][0];
                        int boundary_at = contentType.IndexOf("boundary");
                        if (boundary_at > 0) {
                            boundary_at += 8;
                            while (boundary_at < contentType.Length && Char.IsWhiteSpace(contentType[boundary_at])) {
                                boundary_at++;
                            }
                            if (contentType[boundary_at] == '=') {
                                boundary_at++;
                                if (boundary_at < contentType.Length) {
                                    if (contentType[boundary_at] == '"') {
                                        boundary_at++;
                                        int boundary_end = boundary_at + 1;
                                        while (boundary_end < contentType.Length && contentType[boundary_end] != '"') {
                                            boundary_end++;
                                        }
                                        if (boundary_end > boundary_at + 1) {
                                            uniqueBoundary = contentType.Substring(boundary_at, boundary_end - boundary_at);
                                        }
                                    } else {
                                        while (boundary_at < contentType.Length && Char.IsWhiteSpace(contentType[boundary_at])) {
                                            boundary_at++;
                                        }
                                        int boundary_end = boundary_at;
                                        while (boundary_end < contentType.Length && !Char.IsWhiteSpace(contentType[boundary_end])) {
                                            boundary_end++;
                                        }
                                        if (boundary_end > boundary_at) {
                                            uniqueBoundary = contentType.Substring(boundary_at, boundary_end - boundary_at);
                                            //Console.WriteLine("Boundary of part: " + part.UniqueBoundary);
                                            //MessageBox.Show("Boundary of part: " + part.UniqueBoundary);
                                        }

                                    }
                                }
                            }
                        }
                    }
                    gotBlankLine = true;
                    continue;
                }
                if (gotBlankLine) {
                    //
                    // THEN WE ARE IN THE BODY
                    // FINISH IT
                    //
                    if (eml == null) {
                        eml = new EmlFile();
                        eml.UniqueBoundary = uniqueBoundary;
                    }
                    eml.Headers = headerParamValues;
                    eml.Parts = new List<EmlFile.Part>();
                    if (uniqueBoundary != null && line.IndexOf("--" + uniqueBoundary) == 0) {
                        EmlFile.Part part = null;
                        while ((part = EmlFile.DecodePart(eml, reader, uniqueBoundary, null)) != null) {
                            eml.Parts.Add(part);
                        }
                    } else {
                        // THERE IS ONLY ONE MESSAGE PART
                        EmlFile.Part part = new EmlFile.Part();
                        part.Eml = eml;
                        while ((line = reader.ReadLine()) != null) {
                            eml.DecodedSize += line.Length + 2;
                            part.Content += line + "\r\n";
                        }
                        part.Eml = eml;
                        eml.Parts.Add(part);
                    }
                    return eml;
                } else {
                    // THEN WE ARE STILL IN THE HEADERS
                    if (Char.IsWhiteSpace(line[0])) {
                        line = DecodeHeaderLine(line.TrimStart());
                        if (value != null) {
                            value += line;
                        } else {
                            if (param != null && param.Length > 0) {
                                value = line.Trim();
                            } else {
                                // ERROR!
                                param = null;
                                value = null;
                            }
                        }
                    } else {
                        if (param != null && value != null && param.Length > 0 && value.Length > 0) {
                            param = param.ToLower();
                            if (headerParamValues == null || !headerParamValues.ContainsKey(param)) {
                                headerParamValues[param] = new List<String>();
                            }
                            headerParamValues[param].Add(value);
                            param = null;
                            value = null;
                        }
                        int end_param = line.IndexOf(':');
                        if (end_param > 0) {
                            param = line.Substring(0, end_param).Trim();
                            value = DecodeHeaderLine(line.Substring(end_param + 1).TrimStart());
                            // if (value.StartsWith("=?"))
                        } else {
                            // ERROR
                            param = null;
                            value = null;
                        }
                    }
                }
            }
            reader.Close();
            reader = null;
        } catch (Exception oops) {
            Console.Error.WriteLine(oops.Message + "\r\n" + oops.StackTrace);
        } finally {
            eml.ReadFromStream = null;
            try {
                if (reader != null) {
                    reader.Close();
                    //Console.WriteLine("Closed EML File");
                }
            } catch (Exception e2) {
                Console.Error.WriteLine("This wasn't part of the plan - " + e2.Message); // WTF ?!?!
            }
        }
        return null;
    }
    #endregion



    #region Decode Content-Type Methods


    /// <summary>
    /// Decodes an RFC2047 encoded header.
    /// Other as-of-yet unused header formats may be added later.
    /// </summary>
    /// <param name="line">The value of the first header line, or the value of subsequent header lines.</param>
    /// <returns>The decoded header line</returns>
    public static String DecodeHeaderLine(String line) {
        StringBuilder decodedLine = new StringBuilder();
        int head = 0;
        int tail = 0;
        while (head < line.Length) {
            if (line[head] == '=' && head < line.Length - 7 && line[head + 1] == '?') {
                int charsetEnd = head + 2;
                while (charsetEnd < line.Length && line[charsetEnd] != '?') {
                    charsetEnd++;
                }
                if (charsetEnd < line.Length - 4 && line[charsetEnd] == '?' && line[charsetEnd + 2] == '?') {

                    // FIND END OF TOKEN
                    int tokEnd = charsetEnd + 3;
                    while (tokEnd < line.Length - 1 && !(line[tokEnd] == '?' && line[tokEnd + 1] == '=')) {
                        tokEnd++;
                    }
                    if (tokEnd < line.Length - 1 && line[tokEnd] == '?' && line[tokEnd + 1] == '=') {

                        tokEnd += 2; // THIS WILL BE THE NEXT TAIL

                        char encType = Char.ToUpper(line[charsetEnd + 1]);
                        if (encType == 'Q') {
                            //Console.WriteLine("DECODING QUOTED PRINTABLE HEADER");
                            // SPECIAL QUOTED PRINTABLE
                            String charset = line.Substring(head + 2, charsetEnd - (head + 2));
                            if (head > tail) {
                                decodedLine.Append(line.Substring(tail, head - tail));
                            }
                            String appendMe = line.Substring(charsetEnd + 3, (tokEnd - 2) - (charsetEnd + 3));
                            appendMe = DecodeQuotedPrintable(appendMe, charset);
                            tail = tokEnd;
                            head = tail;
                        } else if (encType == 'B') {
                            // BASE64
                            String charset = line.Substring(head + 2, charsetEnd - (head + 2));
                            Encoding enc = EmlFile.GetDotNetEncoding(charset);
                            if (enc == null) {
                                throw new ArgumentException("Unrecognized charset in quoted-printable header: " + charset);
                            }
                            if (head > tail) {
                                decodedLine.Append(line.Substring(tail, head - tail));
                            }
                            byte[] decodedStuff = Convert.FromBase64String(line.Substring(charsetEnd + 3, (tokEnd - 2) - (charsetEnd + 3)));
                            decodedLine.Append(enc.GetString(decodedStuff));
                            //Console.WriteLine("DECODING BASE64 HEADER: " + charset + ": " + line.Substring(charsetEnd + 3, (tokEnd - 2) - (charsetEnd + 3)));
                            tail = tokEnd;
                            head = tail;
                        } else {
                            //throw new ArgumentException("Unrecognized header encoding format: " + encType);
                            head = tokEnd; // IGNORE INVALID TOKENS WHEN POSSIBLE
                        }
                    } else {
                        head = tokEnd;
                    }

                } else {
                    head = charsetEnd;
                }
            } else {
                head++;
            }
        }
        if (tail == 0) {
            return line;
        } else {
            decodedLine.Append(line.Substring(tail));
            return decodedLine.ToString();
        }
    }



    /// <summary>
    /// Returns the bytes of the quoted-printable "ASCII" string.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public static byte[] DecodeQuotedPrintableBytes(String input) {
        int i = 0;
        byte[] output = new byte[input.Length];
        int o = 0;
        while (i < input.Length) {
            if (input[i] == '=') {
                if (i < input.Length - 3 && input[i + 1] == '\r' && input[i + 2] == '\n') {
                    //Skip
                    i += 3;
                } else {
                    String sHex = input.Substring(i + 1, 2);
                    int hex = Convert.ToInt32(sHex, 16);
                    byte b = Convert.ToByte(hex);
                    output[o++] = b;
                    i += 3;
                }
            } else {
                output[o] = (byte)input[i];
                i++;
                o++;
            }
        }
        byte[] swap = output;
        output = new byte[o];
        Array.Copy(swap, 0, output, 0, output.Length);
        swap = null; // MAYBE GC WILL KICK-IN, BUT WHO CARES
        return output;
    }

    public static String DecodeQuotedPrintable(String input, String encoding) {

        int i = 0;
        byte[] output = new byte[input.Length];
        int o = 0;
        while (i < input.Length) {
            if (input[i] == '=') {
                if (i < input.Length - 3 && input[i + 1] == '\r' && input[i + 2] == '\n') {
                    //Skip
                    i += 3;
                } else {
                    String sHex = input.Substring(i + 1, 2);
                    int hex = Convert.ToInt32(sHex, 16);
                    byte b = Convert.ToByte(hex);
                    output[o++] = b;
                    i += 3;
                }
            } else {
                output[o] = (byte)input[i];
                i++;
                o++;
            }
        }

        if (String.IsNullOrEmpty(encoding)) {
            return Encoding.UTF8.GetString(output, 0, o);
        } else {
            return EmlFile.GetDotNetEncoding(encoding).GetString(output, 0, o);
        }

    }

    /// <summary>
    /// Wrapper method for .NET and email encoding discrepancies.
    /// </summary>
    /// <param name="encoding">The name of the encoding (EG: "ASCII")</param>
    /// <returns>A <code>System.Text.Encoding</code>.</returns>
    public static Encoding GetDotNetEncoding(String encoding) {
        if (encoding == null) {
            return Encoding.UTF8; // GUESS
        }
        encoding = encoding.Trim();
        if (encoding.Length == 0) {
            return Encoding.UTF8; // GUESS
        }
        if (String.Compare(encoding, "ISO-2022-JP", true) == 0) {
            return Encoding.GetEncoding("Shift_JIS");
        } else if (encoding.StartsWith("Windows-", StringComparison.OrdinalIgnoreCase)) {
            try {
                int codepage = Convert.ToInt32(encoding.Substring(8), 16);
                Encoding e = Encoding.GetEncoding(codepage);
                return e;
            } catch (Exception x) {
                return Encoding.GetEncoding("ISO-8859-1");
            }
        } else if (encoding.IndexOf("ASCII", StringComparison.OrdinalIgnoreCase) >= 0) {
            // us-ascii was making .NET crazy
            return Encoding.ASCII;
        } else {
            return Encoding.GetEncoding(encoding);
        }
    }



    #endregion








    public String ToDebugString() {
        return this.ToDebugString("  ");
    }

    public String ToDebugString(String indentStr) {
        StringBuilder sb = new StringBuilder();
        if (this.Headers != null) {
            foreach (String k in this.Headers.Keys) {
                int n = this.Headers[k].Count;
                for (int i = 0; i < n; i++) {
                    sb.AppendLine(indentStr + k + ": " + this.Headers[k].ElementAt(i)); // DON'T BOTHER FOLDING HEADERS HERE
                }
            }
            sb.Append("\r\n");
        }
        foreach (Part p in this.Parts) {
            if (p != null) {
                //String ct = p.GetContentType();
                //if (ct == null || ct.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) {
                if (UniqueBoundary != null) {
                    sb.AppendLine("\r\n--" + UniqueBoundary);
                }
                sb.Append(p.ToDebugString(indentStr + indentStr));
                //}
            }
        }
        if (UniqueBoundary != null) {
            sb.AppendLine("\r\n--" + UniqueBoundary);
        }
        return sb.ToString();
    }
}