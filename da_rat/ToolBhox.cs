using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;

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
        public class Nav
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

            public Nav(string path = "")
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
        }
    }
}