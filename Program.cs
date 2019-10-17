/* The majority of this code was borrowed from David Jeske
 * at https://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C.
 * I restructured it to run in newer versions of C#, and added the
 * IssueTracker class and calls.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;

namespace SimpleHttp
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length<1) { Console.WriteLine("Usage: SimpleHttp.dll {port} [filename]\n       Include '<ISSUES_SECTION>' in the html file for issue tracker placement."); Environment.Exit(0); }
            Console.WriteLine("Hello World!");
            

            var port = 0;
            bool useFile = false;
            string filename = "";

            //port resolve
            if (Int32.TryParse(args[0], out port) && port>1024 && port<=65535) {
                Console.WriteLine("Port Accepted, using "+port+".");
            } else {
                port = 8080;
                Console.WriteLine("Port number could not be resolved, using "+port+".");
            }

            if (args.Length>1 && File.Exists(args[1])) {
                useFile = true;
                filename = args[1];
                Console.WriteLine("Filename accepted: "+filename+", "+useFile+".");
            }

            HttpServer httpServer;
            if(useFile) httpServer = new MyHttpServer(port, filename);
            else        httpServer = new MyHttpServer(port);
           //HttpServer httpServer = new MyHttpServer(port);

            /* if (args.GetLength(0) > 0) {
                Int32.TryParse(args[0], out port);
                if(useFile) httpServer = new MyHttpServer(Convert.ToInt16(args[0]), filename);
                else        httpServer = new MyHttpServer(Convert.ToInt16(args[0]));

            } else {
                if (Int32.TryParse(input, out port)) Console.Write("Accepted, ");
                else { port=8080; Console.Write("Invalid, "); }
                Console.WriteLine("using port "+port);
                if (useFile) httpServer = new MyHttpServer(port, filename);
                else         httpServer = new MyHttpServer(port);
            } */
            Thread thread = new Thread(new ThreadStart(httpServer.listen));
            thread.Start();
        }
    }

    public class HttpProcessor {
        public TcpClient socket;        
        public HttpServer srv;

        private Stream inputStream;
        public StreamWriter outputStream;

        public String http_method;
        public String http_url;
        public String http_protocol_versionstring;
        public Hashtable httpHeaders = new Hashtable();


        private static int MAX_POST_SIZE = 10 * 1024 * 1024; // 10MB

        public HttpProcessor(TcpClient s, HttpServer srv) {
            this.socket = s;
            this.srv = srv;                   
        }
        

        private string streamReadLine(Stream inputStream) {
            int next_char;
            string data = "";
            while (true) {
                next_char = inputStream.ReadByte();
                if (next_char == '\n') { break; }
                if (next_char == '\r') { continue; }
                if (next_char == -1) { Thread.Sleep(1); continue; };
                data += Convert.ToChar(next_char);
            }            
            return data;
        }
        public void process() {                        
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            inputStream = new BufferedStream(socket.GetStream());

            // we probably shouldn't be using a streamwriter for all output from handlers either
            outputStream = new StreamWriter(new BufferedStream(socket.GetStream()));
            try {
                parseRequest();
                readHeaders();
                if (http_method.Equals("GET")) {
                    handleGETRequest();
                } else if (http_method.Equals("POST")) {
                    handlePOSTRequest();
                }
            } catch (Exception e) {
                Console.WriteLine("Exception: " + e.ToString());
                writeFailure();
            }
            outputStream.Flush();
            // bs.Flush(); // flush any remaining output
            inputStream = null; outputStream = null; // bs = null;            
            socket.Close();             
        }

        public void parseRequest() {
            String request = streamReadLine(inputStream);
            string[] tokens = request.Split(' ');
            if (tokens.Length != 3) {
                throw new Exception("invalid http request line");
            }
            http_method = tokens[0].ToUpper();
            http_url = tokens[1];
            http_protocol_versionstring = tokens[2];

            Console.WriteLine("starting: " + request);
        }

        public void readHeaders() {
            Console.WriteLine("readHeaders()");
            String line;
            while ((line = streamReadLine(inputStream)) != null) {
                if (line.Equals("")) {
                    Console.WriteLine("got headers");
                    return;
                }
                
                int separator = line.IndexOf(':');
                if (separator == -1) {
                    throw new Exception("invalid http header line: " + line);
                }
                String name = line.Substring(0, separator);
                int pos = separator + 1;
                while ((pos < line.Length) && (line[pos] == ' ')) {
                    pos++; // strip any spaces
                }
                    
                string value = line.Substring(pos, line.Length - pos);
                Console.WriteLine("header: {0}:{1}",name,value);
                httpHeaders[name] = value;
            }
        }

        public void handleGETRequest() {
            srv.handleGETRequest(this);
        }

        private const int BUF_SIZE = 4096;
        public void handlePOSTRequest() {
            // this post data processing just reads everything into a memory stream.
            // this is fine for smallish things, but for large stuff we should really
            // hand an input stream to the request processor. However, the input stream 
            // we hand him needs to let him see the "end of the stream" at this content 
            // length, because otherwise he won't know when he's seen it all! 

            Console.WriteLine("get post data start");
            int content_len = 0;
            MemoryStream ms = new MemoryStream();
            if (this.httpHeaders.ContainsKey("Content-Length")) {
                 content_len = Convert.ToInt32(this.httpHeaders["Content-Length"]);
                 if (content_len > MAX_POST_SIZE) {
                     throw new Exception(
                         String.Format("POST Content-Length({0}) too big for this simple server",
                           content_len));
                 }
                 byte[] buf = new byte[BUF_SIZE];              
                 int to_read = content_len;
                 while (to_read > 0) {  
                     Console.WriteLine("starting Read, to_read={0}",to_read);

                     int numread = this.inputStream.Read(buf, 0, Math.Min(BUF_SIZE, to_read));
                     Console.WriteLine("read finished, numread={0}", numread);
                     if (numread == 0) {
                         if (to_read == 0) {
                             break;
                         } else {
                             throw new Exception("client disconnected during post");
                         }
                     }
                     to_read -= numread;
                     ms.Write(buf, 0, numread);
                 }
                 ms.Seek(0, SeekOrigin.Begin);
            }
            Console.WriteLine("get post data end");
            srv.handlePOSTRequest(this, new StreamReader(ms));

        }

        public void writeSuccess(string content_type="text/html") {
            outputStream.WriteLine("HTTP/1.0 200 OK");            
            outputStream.WriteLine("Content-Type: " + content_type);
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }

        public void writeFailure() {
            outputStream.WriteLine("HTTP/1.0 404 File not found");
            outputStream.WriteLine("Connection: close");
            outputStream.WriteLine("");
        }
    }

    public abstract class HttpServer {

        protected int port;
        TcpListener listener;
        bool is_active = true;
       
        public HttpServer(int port) {
            this.port = port;
        }

        public void listen() {
            //listener = new TcpListener(port);
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            while (is_active) {                
                TcpClient s = listener.AcceptTcpClient();
                HttpProcessor processor = new HttpProcessor(s, this);
                Thread thread = new Thread(new ThreadStart(processor.process));
                thread.Start();
                Thread.Sleep(1);
            }
        }

        public abstract void handleGETRequest(HttpProcessor p);
        public abstract void handlePOSTRequest(HttpProcessor p, StreamReader inputData);
    }

    public class MyHttpServer : HttpServer {
        public HTML_IssueTracker issueTracker;
        public MyHttpServer(int port)
            : base(port) {
                issueTracker = new HTML_IssueTracker();
        }
        public MyHttpServer(int port, string HTML_Filename) : base(port) {
                issueTracker = new HTML_IssueTracker(HTML_Filename);
        }
        public override void handleGETRequest (HttpProcessor p)
		{

			if (p.http_url.Equals ("/Test.png")) {
				Stream fs = File.Open("../../Test.png",FileMode.Open);

				p.writeSuccess("image/png");
				fs.CopyTo (p.outputStream.BaseStream);
				p.outputStream.BaseStream.Flush ();
			}

            Console.WriteLine("request: {0}", p.http_url);
            p.writeSuccess();
            p.outputStream.WriteLine(issueTracker.getPage());
            //var page = issueTracker.getPageList();
            //foreach (string line in page) p.outputStream.WriteLine(line);


            /* p.outputStream.WriteLine("<html><body><h1>test server</h1>");
            p.outputStream.WriteLine("Current Time: " + DateTime.Now.ToString());
            p.outputStream.WriteLine("url : {0}", p.http_url);

            p.outputStream.WriteLine("<form method=post action=/form>");
            p.outputStream.WriteLine("<input type=text name=foo value=foovalue>");
            p.outputStream.WriteLine("<input type=submit name=bar value=barvalue>");
            p.outputStream.WriteLine("</form>"); */
        }

        public override void handlePOSTRequest(HttpProcessor p, StreamReader inputData) {
            Console.WriteLine("POST request: {0}", p.http_url);
            string data = inputData.ReadToEnd();
            
            issueTracker.handlePOST(data);
            

            //p.writeSuccess();
            /* p.outputStream.WriteLine("<html><body><h1>test server</h1>");
            p.outputStream.WriteLine("<a href=/test>return</a><p>");
            p.outputStream.WriteLine("postbody: <pre>{0}</pre>", data); */
            p.outputStream.WriteLine("HTTP/1.1 303 See Other\nLocation: /");
            //p.outputStream.WriteLine(issueTracker.getPage());
            

        }
    }

    public class HTML_IssueTracker {
        List<string> pageTop = new List<string>();
        List<string> issuesSection = new List<string>();
        List<string> pageBottom = new List<string>();
        List<Tuple<string,Boolean>> issues = new List<Tuple<string,Boolean>>();
        public HTML_IssueTracker() {
            /* pageTop.Add("<!DOCTYPE html><html><head><meta http-equiv='content-type' content='text/html; charset=UTF-8'><title>HTTP Issue Tracker in C#</title>".Replace("'", "\""));
            pageTop.Add("<style>body { font-family: sans-serif } table { border: 3px solid #CCF; } td { padding: 10px; } td:nth-child(1) {width: 64px; text-align: center} td:nth-child(2) {width: 400px;} tr:nth-child(even) {background: #CFC} tr:nth-child(odd)  {background: #FFF}</style>");
            pageTop.Add("</head><body><h3>If you're seeing this, it's a miracle.</h3><table>"); */
            
            pageTop.Add("<!DOCTYPE html><html><head><meta http-equiv='content-type' content='text/html; charset=UTF-8'><title>C# HTTP</title>".Replace("'", "\""));
            pageTop.Add("<style>	body{ font-family: sans-serif; text-align: center; } table{ border: 3px solid #F2F2F2; width: 100%; text-align: left; } td{ padding: 10px; } td:nth-child(1){ width: 5%; text-align: center;} td:nth-child(2){ width: 85%;} tr:nth-child(even){ background: #F2F2F2} tr:nth-child(odd){ background: #FFF} textarea{ width: 100%; }</style>".Replace("'", "\""));
            pageTop.Add("</head><body><table><p>Issues:</p><tbody>");
            
            issues.Add(new Tuple<string, bool>("Sample issue",false));

            /* pageBottom.Add("</table>");
            pageBottom.Add("<form action='/' method='post'><label for='issue'>New Issue:</label><br><textarea name='issue' cols='50' rows='10'></textarea><br><input value='Submit' type='submit'></form>".Replace("'", "\""));
            pageBottom.Add("</body></html>"); */

            pageBottom.Add("</tbody></table><br>");
            pageBottom.Add("<form action='/' method='post'><label for='issue'>New Issue:</label><br><textarea name='issue' cols='50' rows='10'></textarea><br><input value='Submit' type='submit'></form>".Replace("'", "\""));
            pageBottom.Add("</body></html>");   
        }
        public HTML_IssueTracker(string filename) {
            //var pageArray = (String.Join("#$%&",File.ReadAllLines(filename))).Split("<ISSUES_SECTION>");
            //pageTop = pageArray[0].Split("#$%&").ToList();
            //pageBottom = pageArray[1].Split("#$%&").ToList();
            var pageArray = File.ReadAllLines(filename).ToList();
            var splitPos = pageArray.IndexOf("<ISSUES_SECTION>");

            pageTop = pageArray.Take(splitPos).ToList();
            pageBottom = pageArray.Skip(splitPos).ToList();

            issues.Add(new Tuple<string, bool>("Sample issue",false));
        }
        public string getPage() {
            var page = "";
            foreach (string i in pageTop) page += i;
            for (int i=0; i<issues.Count; i++) page += issueHTMLString(i,issues[i].Item1,issues[i].Item2);
            foreach (string i in pageBottom) page += i;
            return page;
        }
        /* public List<string> getPageList() {
            return pageTop.Concat(issues).Concat(pageBottom).ToList();
        } */
        public void handlePOST(string message) {
            string method = new string(message.Take(5).ToArray());
            //Console.WriteLine(method);

            //handle index=
            if (method.Equals("index")) {
                Console.WriteLine("POST request was an issue update.");
                int position = 0;
                bool convSuccess = Int32.TryParse(new string(message.Substring(6).ToArray()), out position);
                if (convSuccess && position>=0 && position<issues.Count) {
                    updateIssue(position, true);
                    Console.WriteLine("Issue {0} updated.", position);
                }
                else {
                    Console.WriteLine("Issue number could not be resolved: "+position);
                }
            }
            //handle issue=
            else if (method.Equals("issue")) {
                Console.WriteLine("POST request was in issue added.");
                //var issueString = String.Join(' ', (new string(message.Split('%')[0].Substring(6).ToArray())).Split('+') );
                var issueString = new string(HttpUtility.UrlDecode(message).Substring(6).ToArray());
                //Console.WriteLine(issueString);
                addIssue(issueString);
                Console.WriteLine("Added issue at position {0}, {2}: {1}", issues.IndexOf(issues.Last()), issues.Last().Item1, issues.Last().Item2);
            }
        }
        public void addIssue(string issue) {
            issues.Add(new Tuple<string, bool>(issue, false));
        }
        public void updateIssue(int position, Boolean value) {
            var currentIssueText = issues[position].Item1;
            issues[position] = new Tuple<string, bool>(currentIssueText,value);
        }
        private string issueHTMLString(int position, string issue, Boolean value) {
            if (!value) return "<tr><td>"+position+"</td><td>"+issue+"</td><td><form action='/' method='post'><input name='index' value='".Replace("'", "\"")+position+"' type='hidden'><input value='Mark Fixed' type='submit'></form></td></tr>".Replace("'", "\"");
            else return "<tr><td>"+position+"</td><td>"+issue+"</td><td>Fixed</td></tr>".Replace("'", "\"");
        }
        public void sendpage(HttpProcessor p) {
            var pageList = pageTop.Concat(issuesSection).Concat(pageBottom).ToList();
            foreach(string item in pageList) {
                p.outputStream.WriteLine(item);
            }
        }
    }
}