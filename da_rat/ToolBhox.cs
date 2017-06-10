using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

/** Class for specific, abstracted function.
 * -> handles local Path, Exec and File logic
 */

namespace da_box
{
    public class Box
    {
        public string _path;
        private List<string> dirs; // Directories @ PATH
        private List<string> fils; // Files @ PATH

        private void Initialize(string path = "")
        {
            if (String.IsNullOrWhiteSpace(path))
                this._path = Directory.GetCurrentDirectory();
            else
                this._path = path;

            this.dirs = new List<string>(Directory.EnumerateDirectories(_path));
            this.fils = new List<string>(Directory.EnumerateFiles(_path));
        }

        public Box(string path = "")
        {
            this._path = "";
            this.dirs = null;
            this.fils = null;

            Initialize(path);
        }

        /* Print out current dir. and it's conntents */
        public string Dir()
        {
            /* Refresh path. */
            Initialize(_path);

            string output = _path + "\n";
            
            foreach (var dir in dirs)   output += dir + "\n";
            foreach (var fil in fils)   output += fil + "\n";

            return output;
        }

        /* Navigate PATH */
        public bool Cd(string d)
        {
            if (d == "..")
            {
                Initialize(Directory.GetParent(_path).ToString());
                return true;
            }
            foreach (var dir in dirs)
                if (d == dir.Remove(0, _path.Length+1))
                {
                    Initialize(dir);
                    return true;
                }

            return false;
        }

        public bool DoesFileExist(string file)
        {
            foreach (var fil in fils)
                if (file == fil.Remove(0, _path.Length+1))
                    return true;

            return false;
        }

        /* Start an instance of powershell with the given command(s)
         * and return the output                                        */
        public string CMD(string command)
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "powershell.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();

            cmd.StandardInput.WriteLine( command );
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.WaitForExit();

            return cmd.StandardOutput.ReadToEnd();
        }

        /* Read a file to string and prepend `headerSize` */
        public string FileToString(string file)
        {
            /* headerSize ... */
            if (DoesFileExist(file))
                return (5 + file.Length + 3).ToString() + " " + File.ReadAllText(_path + "\\" + file);
            
            return "0 empty"; // File not found.
        }
    }
}