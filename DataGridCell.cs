using System;
using System.Collections.Generic;
using System.Text;

namespace URLSchemeViewer
{
    public class DataGridCell
    {
        public string scheme;
        public string executeName;
        public string bundleID;

        public DataGridCell()
        {

        }

        public DataGridCell(string scheme, string executeName, string bundleID)
        {
            this.scheme = scheme;
            this.executeName = executeName;
            this.bundleID = bundleID;
        }
    }
}
